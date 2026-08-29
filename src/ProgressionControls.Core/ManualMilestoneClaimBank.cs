using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal readonly struct PendingMilestoneClaim
    {
        private PendingMilestoneClaim(
            int index,
            int releasedXp,
            int threshold)
        {
            Index = index;
            ReleasedXp = releasedXp;
            Threshold = threshold;
        }

        public static PendingMilestoneClaim None => default;

        public int Index { get; }

        public int ReleasedXp { get; }

        public int Threshold { get; }

        public bool IsPending => Index > 0;

        public bool IsValid =>
            Index == 0
                ? ReleasedXp == 0 && Threshold == 0
                : Index > 0 && ReleasedXp > 0 && Threshold > 0;

        public static bool TryCreate(
            int index,
            int releasedXp,
            int threshold,
            out PendingMilestoneClaim claim)
        {
            var candidate = new PendingMilestoneClaim(
                index,
                releasedXp,
                threshold);
            if (!candidate.IsValid)
            {
                claim = None;
                return false;
            }

            claim = candidate;
            return true;
        }
    }

    internal enum PendingClaimRecovery
    {
        None,
        WaitingForVanilla,
        RolledBack,
        Confirmed,
    }

    internal sealed class ManualMilestoneClaimBank
    {
        public long HeldXp { get; private set; }

        public PendingMilestoneClaim PendingClaim { get; private set; }

        public bool IsClaimPending => PendingClaim.IsPending;

        public bool TryRestore(
            long heldXp,
            PendingMilestoneClaim pendingClaim)
        {
            if (heldXp < 0 || !pendingClaim.IsValid)
            {
                return false;
            }

            HeldXp = heldXp;
            PendingClaim = pendingClaim;
            return true;
        }

        public bool TryRoutePositiveXp(
            int amount,
            int projectedCityXp,
            int nextRequiredXp,
            out int forwardedXp,
            out int bankedXp)
        {
            forwardedXp = 0;
            bankedXp = 0;
            if (amount < 0 || nextRequiredXp <= 0)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            var cap = (long)nextRequiredXp - 1L;
            var availableRoom = Math.Max(
                0L,
                cap - projectedCityXp);
            forwardedXp = (int)Math.Min(amount, availableRoom);
            bankedXp = amount - forwardedXp;
            if (HeldXp > long.MaxValue - bankedXp)
            {
                forwardedXp = amount;
                bankedXp = 0;
                return false;
            }

            HeldXp += bankedXp;
            return true;
        }

        public bool TryBeginClaim(
            int milestoneIndex,
            int requiredXp,
            int cityXp,
            out int releasedXp)
        {
            releasedXp = 0;
            if (IsClaimPending ||
                milestoneIndex <= 0 ||
                requiredXp <= 0 ||
                cityXp >= requiredXp)
            {
                return false;
            }

            var requiredRelease = (long)requiredXp - cityXp;
            if (requiredRelease > int.MaxValue ||
                HeldXp < requiredRelease)
            {
                return false;
            }

            releasedXp = (int)requiredRelease;
            HeldXp -= releasedXp;
            if (!PendingMilestoneClaim.TryCreate(
                milestoneIndex,
                releasedXp,
                requiredXp,
                out var pendingClaim))
            {
                HeldXp += releasedXp;
                releasedXp = 0;
                return false;
            }

            PendingClaim = pendingClaim;
            return true;
        }

        public bool TryConfirmClaim(int achievedMilestone)
        {
            if (!IsClaimPending ||
                achievedMilestone < PendingClaim.Index)
            {
                return false;
            }

            ClearPendingClaim();
            return true;
        }

        public PendingClaimRecovery RecoverPendingClaim(
            int achievedMilestone,
            int cityXp)
        {
            if (!IsClaimPending)
            {
                return PendingClaimRecovery.None;
            }

            if (achievedMilestone >= PendingClaim.Index)
            {
                ClearPendingClaim();
                return PendingClaimRecovery.Confirmed;
            }

            if (cityXp >= PendingClaim.Threshold)
            {
                return PendingClaimRecovery.WaitingForVanilla;
            }

            if (HeldXp > long.MaxValue - PendingClaim.ReleasedXp)
            {
                return PendingClaimRecovery.WaitingForVanilla;
            }

            HeldXp += PendingClaim.ReleasedXp;
            ClearPendingClaim();
            return PendingClaimRecovery.RolledBack;
        }

        public long ReleaseHeldXp()
        {
            if (IsClaimPending)
            {
                return 0;
            }

            var released = HeldXp;
            HeldXp = 0;
            return released;
        }

        public void DiscardHeldXp()
        {
            HeldXp = 0;
        }

        private void ClearPendingClaim()
        {
            PendingClaim = PendingMilestoneClaim.None;
        }
    }
}
