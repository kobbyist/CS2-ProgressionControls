using System;
using System.Collections.Generic;
using System.Linq;
using Game.City;
using Game.Prefabs;
using Kobbyist.ProgressionControls.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Kobbyist.ProgressionControls
{
    public partial class ProgressionControlSystem
    {
        private const uint MilestoneCatalogRetryInterval = 64;

        private readonly ManualMilestoneClaimBank m_ManualMilestoneClaimBank =
            new ManualMilestoneClaimBank();

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_MilestoneLevelQuery;
        private EntityQuery m_MilestoneQuery;
        private EntityQuery m_LockedMilestoneQuery;
        private ManualMilestoneClaimsDialogKind m_ManualMilestoneClaimsDialog;
        private ManualMilestoneClaimsDecision m_RequestedManualMilestoneClaimsDecision;
        private ManualMilestoneCatalog m_MilestoneCatalog;
        private ManualMilestoneRuntimeDefinition[] m_MilestoneRuntimeDefinitions =
            Array.Empty<ManualMilestoneRuntimeDefinition>();
        private uint m_NextMilestoneCatalogAttemptFrame;
        private int m_MilestoneCatalogRevision;
        private int m_RequestedManualMilestone;
        private bool m_ManualClaimsActive;
        private bool m_ManualRecoveryDeferred;
        private bool m_HeldXpReleaseSaturationLogged;
        private bool m_MilestoneCatalogAttempted;

        private void CreateManualMilestoneClaims()
        {
            m_PrefabSystem =
                World.GetOrCreateSystemManaged<PrefabSystem>();
            m_MilestoneLevelQuery = GetEntityQuery(
                ComponentType.ReadOnly<MilestoneLevel>());
            m_MilestoneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<MilestoneData>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            m_LockedMilestoneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<MilestoneData>(),
                    ComponentType.ReadOnly<Locked>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Common.Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            ResetMilestoneCatalog();
        }

        private void InitializeManualMilestoneClaims(
            ProgressionStateSnapshot persisted,
            int cityXp,
            KobbyistProgressionControlsSettings settings)
        {
            ResetManualMilestoneClaims();

            if (persisted != null &&
                !m_ManualMilestoneClaimBank.TryRestore(
                    persisted.HeldMilestoneXp,
                    persisted.PendingMilestoneClaim))
            {
                Mod.Log.Warn(
                    "Ignored invalid persisted manual milestone state");
                m_ManualMilestoneClaimBank.TryRestore(
                    0,
                    PendingMilestoneClaim.None);
            }

            var achievedMilestone = GetAchievedMilestone();
            var recovery =
                m_ManualMilestoneClaimBank.RecoverPendingClaim(
                    achievedMilestone,
                    cityXp);
            if (recovery == PendingClaimRecovery.RolledBack)
            {
                Mod.Log.Warn(
                    "Returned an interrupted milestone claim to held XP");
            }
            else if (recovery == PendingClaimRecovery.Confirmed)
            {
                Mod.Log.Info(
                    "Confirmed a milestone claim recovered from its checkpoint");
            }

            var requested = settings.EnableCustomProgression &&
                settings.ManualMilestoneClaims;
            m_ManualClaimsActive = requested ||
                recovery == PendingClaimRecovery.WaitingForVanilla;
            if (!requested && m_ManualMilestoneClaimBank.HeldXp > 0)
            {
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.Restore;
            }
        }

        private void ResetManualMilestoneClaims()
        {
            m_ManualMilestoneClaimBank.TryRestore(
                0,
                PendingMilestoneClaim.None);
            m_ManualMilestoneClaimsDialog =
                ManualMilestoneClaimsDialogKind.None;
            m_RequestedManualMilestoneClaimsDecision =
                ManualMilestoneClaimsDecision.None;
            m_RequestedManualMilestone = 0;
            m_ManualClaimsActive = false;
            m_ManualRecoveryDeferred = false;
            m_HeldXpReleaseSaturationLogged = false;
        }

        private bool UpdateManualMilestoneClaims(
            KobbyistProgressionControlsSettings settings)
        {
            ConfirmPendingManualClaim();
            ApplyRequestedManualDecision(settings);

            var requested = settings.EnableCustomProgression &&
                settings.ManualMilestoneClaims;
            if (requested)
            {
                m_ManualRecoveryDeferred = false;
                m_ManualClaimsActive = true;
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.None;
            }
            else if (m_ManualClaimsActive &&
                m_ManualMilestoneClaimsDialog ==
                    ManualMilestoneClaimsDialogKind.None)
            {
                if (m_ManualMilestoneClaimBank.HeldXp > 0 ||
                    m_ManualMilestoneClaimBank.IsClaimPending)
                {
                    m_ManualMilestoneClaimsDialog =
                        ManualMilestoneClaimsDialogKind.Disable;
                }
                else
                {
                    m_ManualClaimsActive = false;
                }
            }
            else if (!m_ManualClaimsActive &&
                !m_ManualRecoveryDeferred &&
                m_ManualMilestoneClaimBank.HeldXp > 0 &&
                m_ManualMilestoneClaimsDialog ==
                    ManualMilestoneClaimsDialogKind.None)
            {
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.Restore;
            }

            if (m_ManualClaimsActive &&
                m_ManualMilestoneClaimBank.HeldXp > 0 &&
                !m_ManualMilestoneClaimBank.IsClaimPending &&
                !TryGetNextMilestone(
                    GetAchievedMilestone(),
                    out _,
                    out var finalMilestoneReached) &&
                finalMilestoneReached)
            {
                if (!ReleaseHeldXpToCity())
                {
                    return true;
                }

                Mod.Log.Info(
                    "Released surplus held XP after the final milestone");
            }

            return m_ManualClaimsActive ||
                m_ManualMilestoneClaimBank.IsClaimPending;
        }

        internal void RequestManualMilestoneClaim(int milestoneIndex)
        {
            if (milestoneIndex > 0)
            {
                m_RequestedManualMilestone = milestoneIndex;
            }
        }

        internal void RequestManualMilestoneClaimsDecision(string decision)
        {
            if (string.IsNullOrWhiteSpace(decision) ||
                !Enum.TryParse(
                    decision,
                    ignoreCase: true,
                    out ManualMilestoneClaimsDecision parsed) ||
                parsed == ManualMilestoneClaimsDecision.None)
            {
                return;
            }

            m_RequestedManualMilestoneClaimsDecision = parsed;
        }

        private void ApplyRequestedManualDecision(
            KobbyistProgressionControlsSettings settings)
        {
            var decision = m_RequestedManualMilestoneClaimsDecision;
            m_RequestedManualMilestoneClaimsDecision =
                ManualMilestoneClaimsDecision.None;
            if (decision == ManualMilestoneClaimsDecision.None ||
                m_ManualMilestoneClaimsDialog ==
                    ManualMilestoneClaimsDialogKind.None)
            {
                return;
            }

            if (decision == ManualMilestoneClaimsDecision.Cancel)
            {
                settings.EnableCustomProgression = true;
                settings.ManualMilestoneClaims = true;
                settings.ApplyAndSave();
                m_ManualClaimsActive = true;
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.None;
                return;
            }

            if (m_ManualMilestoneClaimsDialog ==
                ManualMilestoneClaimsDialogKind.Restore)
            {
                if (decision == ManualMilestoneClaimsDecision.Restore)
                {
                    settings.EnableCustomProgression = true;
                    settings.ManualMilestoneClaims = true;
                    settings.ApplyAndSave();
                    m_ManualClaimsActive = true;
                    m_ManualMilestoneClaimsDialog =
                        ManualMilestoneClaimsDialogKind.None;
                }
                else if (decision ==
                    ManualMilestoneClaimsDecision.Discard)
                {
                    m_ManualMilestoneClaimBank.DiscardHeldXp();
                    m_ManualMilestoneClaimsDialog =
                        ManualMilestoneClaimsDialogKind.None;
                    m_ManualClaimsActive = false;
                }
                else if (decision ==
                    ManualMilestoneClaimsDecision.Later)
                {
                    m_ManualRecoveryDeferred = true;
                    m_ManualMilestoneClaimsDialog =
                        ManualMilestoneClaimsDialogKind.None;
                    m_ManualClaimsActive = false;
                }

                return;
            }

            if (m_ManualMilestoneClaimBank.IsClaimPending)
            {
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.Disable;
                return;
            }

            if (decision == ManualMilestoneClaimsDecision.Release)
            {
                if (!ReleaseHeldXpToCity())
                {
                    m_ManualMilestoneClaimsDialog =
                        ManualMilestoneClaimsDialogKind.Disable;
                    return;
                }

                settings.ManualMilestoneClaims = false;
                settings.ApplyAndSave();
                m_ManualClaimsActive = false;
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.None;
            }
            else if (decision ==
                ManualMilestoneClaimsDecision.Discard)
            {
                m_ManualMilestoneClaimBank.DiscardHeldXp();
                settings.ManualMilestoneClaims = false;
                settings.ApplyAndSave();
                m_ManualClaimsActive = false;
                m_ManualMilestoneClaimsDialog =
                    ManualMilestoneClaimsDialogKind.None;
            }
        }

        private void ConfirmPendingManualClaim()
        {
            if (!m_ManualMilestoneClaimBank.IsClaimPending)
            {
                return;
            }

            var pendingIndex =
                m_ManualMilestoneClaimBank.PendingClaim.Index;
            if (m_ManualMilestoneClaimBank.TryConfirmClaim(
                GetAchievedMilestone()))
            {
                Mod.Log.Info(
                    $"Confirmed manual milestone claim {pendingIndex}");
            }
        }

        private bool ReleaseHeldXpToCity()
        {
            if (m_ManualMilestoneClaimBank.IsClaimPending ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Game.City.XP>(city))
            {
                return false;
            }

            var heldXp = m_ManualMilestoneClaimBank.HeldXp;
            if (heldXp <= 0)
            {
                return true;
            }

            var cityXp =
                EntityManager.GetComponentData<Game.City.XP>(city);
            var available = Math.Max(
                0L,
                (long)int.MaxValue - cityXp.m_XP);
            if (!VanillaXpCapacity.TryAdd(
                cityXp.m_XP,
                heldXp,
                out var updatedXp))
            {
                if (!m_HeldXpReleaseSaturationLogged)
                {
                    Mod.Log.Warn(
                        $"Vanilla XP can hold only {available} of {heldXp} held XP; retained the complete bank");
                    m_HeldXpReleaseSaturationLogged = true;
                }

                return false;
            }

            cityXp.m_XP = updatedXp;
            EntityManager.SetComponentData(city, cityXp);
            m_ManualMilestoneClaimBank.ReleaseHeldXp();
            m_HeldXpReleaseSaturationLogged = false;
            Mod.Log.Info(
                $"Released {heldXp} held milestone XP to vanilla progression");

            return true;
        }
        private int RouteManualPositiveXp(
            int amount,
            long projectedCityXp,
            int nextRequiredXp,
            bool finalMilestoneReached)
        {
            if (!m_ManualClaimsActive ||
                amount <= 0 ||
                finalMilestoneReached)
            {
                return amount;
            }

            if (nextRequiredXp <= 0)
            {
                if (m_ManualMilestoneClaimBank.TryHoldPositiveXp(amount))
                {
                    return 0;
                }

                Mod.Log.Warn(
                    "Could not hold milestone XP while milestone definitions were unavailable; forwarded the gain unchanged");
                return amount;
            }

            var projected = projectedCityXp <= 0
                ? 0
                : projectedCityXp >= int.MaxValue
                    ? int.MaxValue
                    : (int)projectedCityXp;
            if (!m_ManualMilestoneClaimBank.TryRoutePositiveXp(
                amount,
                projected,
                nextRequiredXp,
                out var forwarded,
                out _))
            {
                Mod.Log.Warn(
                    "Could not bank milestone XP; forwarded the gain unchanged");
                return amount;
            }

            return forwarded;
        }

        private void TryEnqueueRequestedManualClaim(
            NativeQueue<Game.Simulation.XPGain> queue,
            Entity city,
            ref long projectedCityXp)
        {
            var requestedIndex = m_RequestedManualMilestone;
            m_RequestedManualMilestone = 0;
            if (!m_ManualClaimsActive ||
                requestedIndex <= 0 ||
                m_ManualMilestoneClaimBank.IsClaimPending)
            {
                return;
            }

            var achievedMilestone = GetAchievedMilestone();
            if (!TryGetNextMilestone(
                    achievedMilestone,
                    out var next,
                    out _) ||
                next.Index != requestedIndex)
            {
                Mod.Log.Warn(
                    $"Ignored stale manual milestone claim {requestedIndex}");
                return;
            }

            var cityXp = projectedCityXp <= 0
                ? 0
                : projectedCityXp >= int.MaxValue
                    ? int.MaxValue
                    : (int)projectedCityXp;
            var queueEntries = m_MilestoneCatalog.Build(
                achievedMilestone,
                cityXp,
                m_ManualMilestoneClaimBank.HeldXp,
                claimPending: false,
                claimsActive: m_ManualClaimsActive);
            var first = queueEntries.FirstOrDefault();
            if (first == null ||
                !first.CanClaim ||
                first.Index != requestedIndex ||
                !m_ManualMilestoneClaimBank.TryBeginClaim(
                    next.Index,
                    next.RequiredXp,
                    cityXp,
                    out var releasedXp))
            {
                Mod.Log.Warn(
                    $"Rejected unavailable manual milestone claim {requestedIndex}");
                return;
            }

            queue.Enqueue(new Game.Simulation.XPGain
            {
                entity = city,
                amount = releasedXp,
                reason = Game.Simulation.XPReason.Unknown,
            });
            projectedCityXp += releasedXp;
            Mod.Log.Info(
                $"Released {releasedXp} held XP for milestone {requestedIndex}");
        }

        internal ManualMilestoneClaimsViewState
            GetManualMilestoneClaimsViewState()
        {
            if (!m_HasActiveCity ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Game.City.XP>(city))
            {
                return ManualMilestoneClaimsViewState.Empty;
            }

            var cityXp = Math.Max(
                0,
                EntityManager.GetComponentData<Game.City.XP>(city).m_XP);
            var achievedMilestone = GetAchievedMilestone();
            var catalogAvailable = TryEnsureMilestoneCatalog();
            var queue = catalogAvailable
                ? m_MilestoneCatalog.Build(
                    achievedMilestone,
                    cityXp,
                    m_ManualMilestoneClaimBank.HeldXp,
                    m_ManualMilestoneClaimBank.IsClaimPending,
                    m_ManualClaimsActive)
                : Array.Empty<ManualMilestoneQueueEntry>();
            var milestones = new ManualMilestoneClaimView[queue.Count];
            for (var index = 0; index < queue.Count; index++)
            {
                var entry = queue[index];
                var runtimeIndex = entry.Index - 1;
                var image = runtimeIndex >= 0 &&
                    runtimeIndex < m_MilestoneRuntimeDefinitions.Length
                    ? m_MilestoneRuntimeDefinitions[runtimeIndex].Image
                    : string.Empty;
                milestones[index] = new ManualMilestoneClaimView
                {
                    Index = entry.Index,
                    RequiredXp = entry.RequiredXp,
                    CanClaim = entry.CanClaim,
                    Image = image,
                };
            }
            var effectiveXp =
                m_ManualMilestoneClaimBank.HeldXp >
                    long.MaxValue - cityXp
                    ? long.MaxValue
                    : m_ManualMilestoneClaimBank.HeldXp + cityXp;
            ManualMilestoneRuntimeDefinition nextMilestone = null;
            var finalMilestoneReached = false;
            if (catalogAvailable)
            {
                TryGetNextMilestone(
                    achievedMilestone,
                    out nextMilestone,
                    out finalMilestoneReached);
            }
            var nextRequiredXp = nextMilestone?.RequiredXp ?? 0;
            var nextRangeXp = nextRequiredXp > 0
                ? Math.Min(Math.Max(0L, effectiveXp), nextRequiredXp)
                : 0;
            return new ManualMilestoneClaimsViewState
            {
                HeldXp = m_ManualMilestoneClaimBank.HeldXp,
                ClaimPending =
                    m_ManualMilestoneClaimBank.IsClaimPending,
                Dialog = m_ManualMilestoneClaimsDialog
                    .ToString()
                    .ToLowerInvariant(),
                Milestones = milestones,
                NextMilestoneIndex =
                    nextMilestone?.Index ?? 0,
                NextRequiredXp = nextRequiredXp,
                NextImage =
                    nextMilestone?.Image ?? string.Empty,
                NextRangeXp = nextRangeXp,
                NextBackgroundColor =
                    nextMilestone?.BackgroundColor ?? default,
                NextTextColor =
                    nextMilestone?.TextColor ?? default,
                CatalogAvailable = catalogAvailable,
                FinalMilestoneReached = finalMilestoneReached,
            };
        }

        internal ManualMilestoneClaimsViewKey
            GetManualMilestoneClaimsViewKey(bool includePanelState)
        {
            if (!m_HasActiveCity ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Game.City.XP>(city))
            {
                return default;
            }

            var cityXp = 0;
            var achievedMilestone = 0;
            if (includePanelState)
            {
                cityXp = Math.Max(
                    0,
                    EntityManager.GetComponentData<Game.City.XP>(city).m_XP);
                achievedMilestone = GetAchievedMilestone();
                TryEnsureMilestoneCatalog();
            }
            return new ManualMilestoneClaimsViewKey(
                available: true,
                cityXp,
                achievedMilestone,
                m_ManualMilestoneClaimBank.HeldXp,
                m_ManualMilestoneClaimBank.IsClaimPending,
                m_ManualClaimsActive,
                m_ManualMilestoneClaimsDialog,
                m_MilestoneCatalogRevision);
        }

        private int GetAchievedMilestone()
        {
            if (m_MilestoneLevelQuery.CalculateEntityCount() != 1)
            {
                return 0;
            }

            return Math.Max(
                0,
                m_MilestoneLevelQuery
                    .GetSingleton<MilestoneLevel>()
                    .m_AchievedMilestone);
        }

        private bool TryGetNextMilestone(
            int achievedMilestone,
            out ManualMilestoneRuntimeDefinition milestone,
            out bool finalMilestoneReached)
        {
            finalMilestoneReached = false;
            if (!TryEnsureMilestoneCatalog())
            {
                milestone = null;
                return false;
            }

            if (!m_MilestoneCatalog.TryGetNext(
                    achievedMilestone,
                    out var next,
                    out var catalogFinalMilestoneReached))
            {
                milestone = null;
                finalMilestoneReached =
                    catalogFinalMilestoneReached &&
                    m_LockedMilestoneQuery.IsEmpty;
                return false;
            }

            milestone = m_MilestoneRuntimeDefinitions[next.Index - 1];
            return true;
        }

        private void ResetMilestoneCatalog()
        {
            m_MilestoneCatalog = null;
            m_MilestoneRuntimeDefinitions =
                Array.Empty<ManualMilestoneRuntimeDefinition>();
            m_MilestoneCatalogAttempted = false;
            m_NextMilestoneCatalogAttemptFrame = 0;
            m_MilestoneCatalogRevision = 0;
        }

        private bool TryEnsureMilestoneCatalog()
        {
            if (m_MilestoneCatalog != null)
            {
                return true;
            }

            var currentFrame = m_SimulationSystem == null
                ? 0
                : m_SimulationSystem.frameIndex;
            if (m_MilestoneCatalogAttempted &&
                unchecked((int)(currentFrame -
                    m_NextMilestoneCatalogAttemptFrame)) < 0)
            {
                return false;
            }

            m_MilestoneCatalogAttempted = true;
            m_NextMilestoneCatalogAttemptFrame =
                currentFrame + MilestoneCatalogRetryInterval;
            if (m_MilestoneQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            using (var entities =
                m_MilestoneQuery.ToEntityArray(Allocator.Temp))
            using (var data =
                m_MilestoneQuery.ToComponentDataArray<
                    MilestoneData>(Allocator.Temp))
            {
                if (entities.Length != data.Length)
                {
                    return false;
                }

                var count = data.Length;
                var result =
                    new List<ManualMilestoneRuntimeDefinition>(count);
                for (var index = 0; index < count; index++)
                {
                    var milestoneData = data[index];
                    if (milestoneData.m_Index <= 0 ||
                        milestoneData.m_XpRequried < 0)
                    {
                        return false;
                    }

                    var image = string.Empty;
                    var backgroundColor = default(
                        MilestoneCardColorView);
                    var textColor = default(
                        MilestoneCardColorView);
                    try
                    {
                        var prefab =
                            m_PrefabSystem.GetPrefab<MilestonePrefab>(
                                entities[index]);
                        if (prefab != null)
                        {
                            image = prefab.m_Image ?? string.Empty;
                            backgroundColor = ToColorView(
                                prefab.m_BackgroundColor);
                            textColor = ToColorView(
                                prefab.m_TextColor);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    result.Add(
                        new ManualMilestoneRuntimeDefinition(
                            milestoneData.m_Index,
                            milestoneData.m_XpRequried,
                            milestoneData.m_IsVictory,
                            image,
                            backgroundColor,
                            textColor));
                }

                var runtimeDefinitions = result
                    .OrderBy(milestone => milestone.Index)
                    .ThenBy(milestone => milestone.RequiredXp)
                    .ToArray();
                var definitions = runtimeDefinitions
                    .Select(definition =>
                        new ManualMilestoneDefinition(
                            definition.Index,
                            definition.RequiredXp,
                            definition.IsFinal))
                    .ToArray();
                if (!ManualMilestoneCatalog.TryCreate(
                    definitions,
                    out var catalog))
                {
                    return false;
                }

                m_MilestoneRuntimeDefinitions = runtimeDefinitions;
                m_MilestoneCatalog = catalog;
                m_MilestoneCatalogRevision++;
                return true;
            }
        }

        private static MilestoneCardColorView ToColorView(
            Color color)
        {
            return new MilestoneCardColorView(
                color.r,
                color.g,
                color.b,
                color.a);
        }

        private sealed class ManualMilestoneRuntimeDefinition
        {
            public ManualMilestoneRuntimeDefinition(
                int index,
                int requiredXp,
                bool isFinal,
                string image,
                MilestoneCardColorView backgroundColor,
                MilestoneCardColorView textColor)
            {
                Index = index;
                RequiredXp = requiredXp;
                IsFinal = isFinal;
                Image = image;
                BackgroundColor = backgroundColor;
                TextColor = textColor;
            }

            public int Index { get; }

            public int RequiredXp { get; }

            public bool IsFinal { get; }

            public string Image { get; }

            public MilestoneCardColorView BackgroundColor { get; }

            public MilestoneCardColorView TextColor { get; }
        }
    }
}
