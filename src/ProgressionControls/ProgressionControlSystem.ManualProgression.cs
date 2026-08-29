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
        private readonly ManualProgressionBank m_ManualProgressionBank =
            new ManualProgressionBank();

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_MilestoneLevelQuery;
        private EntityQuery m_MilestoneQuery;
        private ManualProgressionDialogKind m_ManualProgressionDialog;
        private ManualProgressionDecision m_RequestedManualDecision;
        private int m_RequestedManualMilestone;
        private bool m_ManualClaimsActive;
        private bool m_ManualRecoveryDeferred;

        private void CreateManualProgression()
        {
            m_PrefabSystem =
                World.GetOrCreateSystemManaged<PrefabSystem>();
            m_MilestoneLevelQuery = GetEntityQuery(
                ComponentType.ReadOnly<MilestoneLevel>());
            m_MilestoneQuery = GetEntityQuery(
                ComponentType.ReadOnly<MilestoneData>());
        }

        private void InitializeManualProgression(
            ProgressionStateSnapshot persisted,
            int cityXp,
            KobbyistProgressionControlsSettings settings)
        {
            ResetManualProgression();

            if (persisted != null &&
                !m_ManualProgressionBank.TryRestore(
                    persisted.HeldMilestoneXp,
                    persisted.PendingMilestoneClaimIndex,
                    persisted.PendingMilestoneClaimXp,
                    persisted.PendingMilestoneClaimThreshold))
            {
                Mod.Log.Warn(
                    "Ignored invalid persisted manual milestone state");
                m_ManualProgressionBank.TryRestore(0, 0, 0, 0);
            }

            var achievedMilestone = GetAchievedMilestone();
            var recovery =
                m_ManualProgressionBank.RecoverPendingClaim(
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
            if (!requested && m_ManualProgressionBank.HeldXp > 0)
            {
                m_ManualProgressionDialog =
                    recovery == PendingClaimRecovery.WaitingForVanilla
                        ? ManualProgressionDialogKind.Disable
                        : ManualProgressionDialogKind.Restore;
            }
        }

        private void ResetManualProgression()
        {
            m_ManualProgressionBank.TryRestore(0, 0, 0, 0);
            m_ManualProgressionDialog =
                ManualProgressionDialogKind.None;
            m_RequestedManualDecision =
                ManualProgressionDecision.None;
            m_RequestedManualMilestone = 0;
            m_ManualClaimsActive = false;
            m_ManualRecoveryDeferred = false;
        }

        private bool UpdateManualProgression(
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
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.None;
            }
            else if (m_ManualClaimsActive &&
                m_ManualProgressionDialog ==
                    ManualProgressionDialogKind.None)
            {
                if (m_ManualProgressionBank.HeldXp > 0 ||
                    m_ManualProgressionBank.IsClaimPending)
                {
                    m_ManualProgressionDialog =
                        ManualProgressionDialogKind.Disable;
                }
                else
                {
                    m_ManualClaimsActive = false;
                }
            }
            else if (!m_ManualClaimsActive &&
                !m_ManualRecoveryDeferred &&
                m_ManualProgressionBank.HeldXp > 0 &&
                m_ManualProgressionDialog ==
                    ManualProgressionDialogKind.None)
            {
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.Restore;
            }

            if (m_ManualClaimsActive &&
                m_ManualProgressionBank.HeldXp > 0 &&
                !m_ManualProgressionBank.IsClaimPending &&
                !TryGetNextMilestone(
                    GetAchievedMilestone(),
                    out _))
            {
                if (!ReleaseHeldXpToCity())
                {
                    return true;
                }

                Mod.Log.Info(
                    "Released surplus held XP after the final milestone");
            }

            return m_ManualClaimsActive ||
                m_ManualProgressionBank.IsClaimPending;
        }

        internal void RequestManualMilestoneClaim(int milestoneIndex)
        {
            if (milestoneIndex > 0)
            {
                m_RequestedManualMilestone = milestoneIndex;
            }
        }

        internal void RequestManualProgressionDecision(string decision)
        {
            if (string.IsNullOrWhiteSpace(decision) ||
                !Enum.TryParse(
                    decision,
                    ignoreCase: true,
                    out ManualProgressionDecision parsed) ||
                parsed == ManualProgressionDecision.None)
            {
                return;
            }

            m_RequestedManualDecision = parsed;
        }

        private void ApplyRequestedManualDecision(
            KobbyistProgressionControlsSettings settings)
        {
            var decision = m_RequestedManualDecision;
            m_RequestedManualDecision =
                ManualProgressionDecision.None;
            if (decision == ManualProgressionDecision.None ||
                m_ManualProgressionDialog ==
                    ManualProgressionDialogKind.None)
            {
                return;
            }

            if (decision == ManualProgressionDecision.Cancel)
            {
                settings.EnableCustomProgression = true;
                settings.ManualMilestoneClaims = true;
                settings.ApplyAndSave();
                m_ManualClaimsActive = true;
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.None;
                return;
            }

            if (m_ManualProgressionDialog ==
                ManualProgressionDialogKind.Restore)
            {
                if (decision == ManualProgressionDecision.Restore)
                {
                    settings.EnableCustomProgression = true;
                    settings.ManualMilestoneClaims = true;
                    settings.ApplyAndSave();
                    m_ManualClaimsActive = true;
                    m_ManualProgressionDialog =
                        ManualProgressionDialogKind.None;
                }
                else if (decision ==
                    ManualProgressionDecision.Discard)
                {
                    m_ManualProgressionBank.DiscardHeldXp();
                    m_ManualProgressionDialog =
                        ManualProgressionDialogKind.None;
                    m_ManualClaimsActive = false;
                }
                else if (decision ==
                    ManualProgressionDecision.Later)
                {
                    m_ManualRecoveryDeferred = true;
                    m_ManualProgressionDialog =
                        ManualProgressionDialogKind.None;
                    m_ManualClaimsActive = false;
                }

                return;
            }

            if (m_ManualProgressionBank.IsClaimPending)
            {
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.Disable;
                return;
            }

            if (decision == ManualProgressionDecision.Release)
            {
                if (!ReleaseHeldXpToCity())
                {
                    m_ManualProgressionDialog =
                        ManualProgressionDialogKind.Disable;
                    return;
                }

                settings.ManualMilestoneClaims = false;
                settings.ApplyAndSave();
                m_ManualClaimsActive = false;
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.None;
            }
            else if (decision ==
                ManualProgressionDecision.Discard)
            {
                m_ManualProgressionBank.DiscardHeldXp();
                settings.ManualMilestoneClaims = false;
                settings.ApplyAndSave();
                m_ManualClaimsActive = false;
                m_ManualProgressionDialog =
                    ManualProgressionDialogKind.None;
            }
        }

        private void ConfirmPendingManualClaim()
        {
            if (!m_ManualProgressionBank.IsClaimPending)
            {
                return;
            }

            var pendingIndex =
                m_ManualProgressionBank.PendingClaimIndex;
            if (m_ManualProgressionBank.TryConfirmClaim(
                GetAchievedMilestone()))
            {
                Mod.Log.Info(
                    $"Confirmed manual milestone claim {pendingIndex}");
            }
        }

        private bool ReleaseHeldXpToCity()
        {
            if (m_ManualProgressionBank.IsClaimPending ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Game.City.XP>(city))
            {
                return false;
            }

            var heldXp = m_ManualProgressionBank.HeldXp;
            if (heldXp <= 0)
            {
                return true;
            }

            var cityXp =
                EntityManager.GetComponentData<Game.City.XP>(city);
            var available = Math.Max(
                0L,
                (long)int.MaxValue - cityXp.m_XP);
            var released = Math.Min(heldXp, available);
            cityXp.m_XP = (int)Math.Min(
                int.MaxValue,
                (long)cityXp.m_XP + released);
            EntityManager.SetComponentData(city, cityXp);
            m_ManualProgressionBank.ReleaseHeldXp();

            if (released < heldXp)
            {
                Mod.Log.Warn(
                    $"Vanilla XP saturated after releasing {released} of {heldXp} held XP");
            }
            else
            {
                Mod.Log.Info(
                    $"Released {released} held milestone XP to vanilla progression");
            }

            return true;
        }
        private int RouteManualPositiveXp(
            int amount,
            long projectedCityXp,
            int nextRequiredXp)
        {
            if (!m_ManualClaimsActive ||
                amount <= 0 ||
                nextRequiredXp <= 0)
            {
                return amount;
            }

            var projected = projectedCityXp <= 0
                ? 0
                : projectedCityXp >= int.MaxValue
                    ? int.MaxValue
                    : (int)projectedCityXp;
            if (!m_ManualProgressionBank.TryRoutePositiveXp(
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
                m_ManualProgressionBank.IsClaimPending)
            {
                return;
            }

            var achievedMilestone = GetAchievedMilestone();
            if (!TryGetNextMilestone(
                    achievedMilestone,
                    out var next) ||
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
            var queueEntries = ManualMilestoneQueue.Build(
                achievedMilestone,
                cityXp,
                m_ManualProgressionBank.HeldXp,
                claimPending: false,
                GetMilestoneDefinitions());
            var first = queueEntries.FirstOrDefault();
            if (first == null ||
                !first.CanClaim ||
                first.Index != requestedIndex ||
                !m_ManualProgressionBank.TryBeginClaim(
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

        internal ManualProgressionViewState
            GetManualProgressionViewState()
        {
            if (!m_HasActiveCity ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Game.City.XP>(city))
            {
                return ManualProgressionViewState.Empty;
            }

            var cityXp = Math.Max(
                0,
                EntityManager.GetComponentData<Game.City.XP>(city).m_XP);
            var achievedMilestone = GetAchievedMilestone();
            var definitions = GetMilestoneRuntimeDefinitions();
            var queue = ManualMilestoneQueue.Build(
                achievedMilestone,
                cityXp,
                m_ManualProgressionBank.HeldXp,
                m_ManualProgressionBank.IsClaimPending,
                definitions.Select(definition =>
                    new ManualMilestoneDefinition(
                        definition.Index,
                        definition.RequiredXp)));
            var images = definitions
                .GroupBy(definition => definition.Index)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Image);
            var milestones = queue.Select(entry =>
                new ManualProgressionMilestoneView
                {
                    Index = entry.Index,
                    RequiredXp = entry.RequiredXp,
                    CanClaim = entry.CanClaim,
                    Image = images.TryGetValue(
                        entry.Index,
                        out var image)
                        ? image
                        : string.Empty,
                }).ToArray();
            var effectiveXp =
                m_ManualProgressionBank.HeldXp >
                    long.MaxValue - cityXp
                    ? long.MaxValue
                    : m_ManualProgressionBank.HeldXp + cityXp;
            var nextMilestone = definitions
                .Where(definition =>
                    definition.Index > achievedMilestone)
                .OrderBy(definition => definition.Index)
                .ThenBy(definition => definition.RequiredXp)
                .FirstOrDefault();
            var nextRange = MilestoneRangeProgress.Calculate(
                effectiveXp,
                nextMilestone?.RequiredXp ?? 0);
            return new ManualProgressionViewState
            {
                Available = true,
                Active = m_ManualClaimsActive,
                HeldXp = m_ManualProgressionBank.HeldXp,
                CityXp = cityXp,
                EffectiveXp = effectiveXp,
                ClaimPending =
                    m_ManualProgressionBank.IsClaimPending,
                Dialog = m_ManualProgressionDialog
                    .ToString()
                    .ToLowerInvariant(),
                Milestones = milestones,
                NextMilestoneIndex =
                    nextMilestone?.Index ?? 0,
                NextRequiredXp =
                    nextRange.RequiredXp,
                NextImage =
                    nextMilestone?.Image ?? string.Empty,
                NextRangeXp = nextRange.CurrentXp,
                NextBackgroundColor =
                    nextMilestone?.BackgroundColor ?? default,
                NextTextColor =
                    nextMilestone?.TextColor ?? default,
            };
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
            out ManualMilestoneRuntimeDefinition milestone)
        {
            milestone = GetMilestoneRuntimeDefinitions()
                .Where(candidate =>
                    candidate.Index > achievedMilestone)
                .OrderBy(candidate => candidate.Index)
                .ThenBy(candidate => candidate.RequiredXp)
                .FirstOrDefault();
            return milestone != null;
        }

        private IReadOnlyList<ManualMilestoneDefinition>
            GetMilestoneDefinitions()
        {
            return GetMilestoneRuntimeDefinitions()
                .Select(definition =>
                    new ManualMilestoneDefinition(
                        definition.Index,
                        definition.RequiredXp))
                .ToArray();
        }

        private IReadOnlyList<ManualMilestoneRuntimeDefinition>
            GetMilestoneRuntimeDefinitions()
        {
            if (m_MilestoneQuery.IsEmptyIgnoreFilter)
            {
                return Array.Empty<
                    ManualMilestoneRuntimeDefinition>();
            }

            using (var entities =
                m_MilestoneQuery.ToEntityArray(Allocator.Temp))
            using (var data =
                m_MilestoneQuery.ToComponentDataArray<
                    MilestoneData>(Allocator.Temp))
            {
                var count = Math.Min(entities.Length, data.Length);
                var result =
                    new List<ManualMilestoneRuntimeDefinition>(count);
                for (var index = 0; index < count; index++)
                {
                    var milestoneData = data[index];
                    if (milestoneData.m_Index <= 0 ||
                        milestoneData.m_XpRequried <= 0)
                    {
                        continue;
                    }

                    var image = string.Empty;
                    var backgroundColor = default(
                        ManualProgressionColorView);
                    var textColor = default(
                        ManualProgressionColorView);
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
                            image,
                            backgroundColor,
                            textColor));
                }

                return result
                    .OrderBy(milestone => milestone.Index)
                    .ThenBy(milestone => milestone.RequiredXp)
                    .ToArray();
            }
        }

        private static ManualProgressionColorView ToColorView(
            Color color)
        {
            return new ManualProgressionColorView(
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
                string image,
                ManualProgressionColorView backgroundColor,
                ManualProgressionColorView textColor)
            {
                Index = index;
                RequiredXp = requiredXp;
                Image = image;
                BackgroundColor = backgroundColor;
                TextColor = textColor;
            }

            public int Index { get; }

            public int RequiredXp { get; }

            public string Image { get; }

            public ManualProgressionColorView BackgroundColor { get; }

            public ManualProgressionColorView TextColor { get; }
        }
    }
}
