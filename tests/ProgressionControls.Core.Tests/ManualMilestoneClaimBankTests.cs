using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ManualMilestoneClaimBankTests
{
    [TestMethod]
    public void PositiveXpStopsOnePointBeforeMilestone()
    {
        var bank = new ManualMilestoneClaimBank();

        Assert.IsTrue(bank.TryRoutePositiveXp(
            amount: 25,
            projectedCityXp: 90,
            nextRequiredXp: 100,
            out var forwarded,
            out var banked));

        Assert.AreEqual(9, forwarded);
        Assert.AreEqual(16, banked);
        Assert.AreEqual(16L, bank.HeldXp);
    }

    [TestMethod]
    public void RepeatedGainsUseProjectedCityXp()
    {
        var bank = new ManualMilestoneClaimBank();

        Assert.IsTrue(bank.TryRoutePositiveXp(
            5,
            projectedCityXp: 90,
            nextRequiredXp: 100,
            out var firstForwarded,
            out _));
        Assert.IsTrue(bank.TryRoutePositiveXp(
            10,
            projectedCityXp: 90 + firstForwarded,
            nextRequiredXp: 100,
            out var secondForwarded,
            out var secondBanked));

        Assert.AreEqual(5, firstForwarded);
        Assert.AreEqual(4, secondForwarded);
        Assert.AreEqual(6, secondBanked);
        Assert.AreEqual(6L, bank.HeldXp);
    }

    [TestMethod]
    public void UnknownMilestoneThresholdHoldsTheCompleteGain()
    {
        var bank = new ManualMilestoneClaimBank();

        Assert.IsTrue(bank.TryHoldPositiveXp(25));

        Assert.AreEqual(25L, bank.HeldXp);
    }

    [TestMethod]
    public void ClaimReleasesOnlyThresholdDelta()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(bank.TryRestore(
            heldXp: 50,
            pendingClaim: PendingMilestoneClaim.None));

        Assert.IsTrue(bank.TryBeginClaim(
            milestoneIndex: 4,
            requiredXp: 100,
            cityXp: 99,
            out var released));

        Assert.AreEqual(1, released);
        Assert.AreEqual(49L, bank.HeldXp);
        Assert.AreEqual(4, bank.PendingClaim.Index);
        Assert.AreEqual(1, bank.PendingClaim.ReleasedXp);
        Assert.AreEqual(100, bank.PendingClaim.Threshold);
    }

    [TestMethod]
    public void PendingClaimBlocksAnotherClaim()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(bank.TryRestore(50, PendingMilestoneClaim.None));
        Assert.IsTrue(bank.TryBeginClaim(4, 100, 99, out _));

        Assert.IsFalse(bank.TryBeginClaim(5, 150, 100, out _));
        Assert.AreEqual(49L, bank.HeldXp);
    }

    [TestMethod]
    public void ConfirmedClaimClearsPendingState()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(bank.TryRestore(50, PendingMilestoneClaim.None));
        Assert.IsTrue(bank.TryBeginClaim(4, 100, 99, out _));

        Assert.IsFalse(bank.TryConfirmClaim(3));
        Assert.IsTrue(bank.TryConfirmClaim(4));
        Assert.IsFalse(bank.IsClaimPending);
        Assert.AreEqual(49L, bank.HeldXp);
    }

    [TestMethod]
    public void RecoveryRollsBackClaimMissingFromCitySave()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(PendingMilestoneClaim.TryCreate(
            4,
            1,
            100,
            out var pendingClaim));
        Assert.IsTrue(bank.TryRestore(
            heldXp: 49,
            pendingClaim: pendingClaim));

        var recovery = bank.RecoverPendingClaim(
            achievedMilestone: 3,
            cityXp: 99);

        Assert.AreEqual(PendingClaimRecovery.RolledBack, recovery);
        Assert.AreEqual(50L, bank.HeldXp);
        Assert.IsFalse(bank.IsClaimPending);
    }

    [TestMethod]
    public void RecoveryWaitsWhenClaimXpReachedCitySave()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(PendingMilestoneClaim.TryCreate(
            4,
            1,
            100,
            out var pendingClaim));
        Assert.IsTrue(bank.TryRestore(49, pendingClaim));

        var recovery = bank.RecoverPendingClaim(
            achievedMilestone: 3,
            cityXp: 100);

        Assert.AreEqual(
            PendingClaimRecovery.WaitingForVanilla,
            recovery);
        Assert.IsTrue(bank.IsClaimPending);
        Assert.AreEqual(49L, bank.HeldXp);
    }

    [TestMethod]
    public void PendingClaimRejectsInconsistentState()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(bank.TryRestore(12, PendingMilestoneClaim.None));

        Assert.IsFalse(PendingMilestoneClaim.TryCreate(
            4,
            0,
            100,
            out _));
        Assert.AreEqual(12L, bank.HeldXp);
        Assert.IsFalse(bank.IsClaimPending);
    }

    [TestMethod]
    public void RestoreRejectsNegativeHeldXpAtomically()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(bank.TryRestore(12, PendingMilestoneClaim.None));

        Assert.IsFalse(bank.TryRestore(-1, PendingMilestoneClaim.None));
        Assert.AreEqual(12L, bank.HeldXp);
        Assert.IsFalse(bank.IsClaimPending);
    }

    [TestMethod]
    public void DiscardClearsHeldXpButPreservesPendingClaim()
    {
        var bank = new ManualMilestoneClaimBank();
        Assert.IsTrue(PendingMilestoneClaim.TryCreate(
            4,
            1,
            100,
            out var pendingClaim));
        Assert.IsTrue(bank.TryRestore(49, pendingClaim));

        Assert.AreEqual(0L, bank.ReleaseHeldXp());
        bank.DiscardHeldXp();
        Assert.AreEqual(0L, bank.HeldXp);
        Assert.IsTrue(bank.IsClaimPending);
        Assert.AreEqual(4, bank.PendingClaim.Index);
    }
}
