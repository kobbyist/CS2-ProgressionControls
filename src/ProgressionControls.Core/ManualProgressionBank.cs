using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal enum PendingClaimRecovery
    {
        None,
        WaitingForVanilla,
        RolledBack,
        Confirmed,
    }

    internal sealed class ManualProgressionBank
    {
        public long HeldXp { get; private set; }

        public int PendingClaimIndex { get; private set; }

        public int PendingClaimXp { get; private set; }

        public int PendingClaimThreshold { get; private set; }

        public bool IsClaimPending => PendingClaimIndex > 0;

        public bool TryRestore(
            long heldXp,
            int pendingClaimIndex,
            int pendingClaimXp,
            int pendingClaimThreshold)
        {
            if (heldXp < 0 ||
                pendingClaimIndex < 0 ||
                pendingClaimXp < 0 ||
                pendingClaimThreshold < 0 ||
                !PendingFieldsAreConsistent(
                    pendingClaimIndex,
                    pendingClaimXp,
                    pendingClaimThreshold))
            {
                return false;
            }

            HeldXp = heldXp;
            PendingClaimIndex = pendingClaimIndex;
            PendingClaimXp = pendingClaimXp;
            PendingClaimThreshold = pendingClaimThreshold;
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
            PendingClaimIndex = milestoneIndex;
            PendingClaimXp = releasedXp;
            PendingClaimThreshold = requiredXp;
            return true;
        }

        public bool TryConfirmClaim(int achievedMilestone)
        {
            if (!IsClaimPending ||
                achievedMilestone < PendingClaimIndex)
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

            if (achievedMilestone >= PendingClaimIndex)
            {
                ClearPendingClaim();
                return PendingClaimRecovery.Confirmed;
            }

            if (cityXp >= PendingClaimThreshold)
            {
                return PendingClaimRecovery.WaitingForVanilla;
            }

            if (HeldXp > long.MaxValue - PendingClaimXp)
            {
                return PendingClaimRecovery.WaitingForVanilla;
            }

            HeldXp += PendingClaimXp;
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

        public bool DiscardHeldXp()
        {
            if (IsClaimPending)
            {
                return false;
            }

            HeldXp = 0;
            return true;
        }

        private static bool PendingFieldsAreConsistent(
            int pendingClaimIndex,
            int pendingClaimXp,
            int pendingClaimThreshold)
        {
            if (pendingClaimIndex == 0)
            {
                return pendingClaimXp == 0 &&
                    pendingClaimThreshold == 0;
            }

            return pendingClaimXp > 0 &&
                pendingClaimThreshold > 0;
        }

        private void ClearPendingClaim()
        {
            PendingClaimIndex = 0;
            PendingClaimXp = 0;
            PendingClaimThreshold = 0;
        }
    }
}
