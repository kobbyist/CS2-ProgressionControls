using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ManualProgressionBankTests
{
    [TestMethod]
    public void PositiveXpStopsOnePointBeforeMilestone()
    {
        var bank = new ManualProgressionBank();

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
        var bank = new ManualProgressionBank();

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
    public void ClaimReleasesOnlyThresholdDelta()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(
            heldXp: 50,
            pendingClaimIndex: 0,
            pendingClaimXp: 0,
            pendingClaimThreshold: 0));

        Assert.IsTrue(bank.TryBeginClaim(
            milestoneIndex: 4,
            requiredXp: 100,
            cityXp: 99,
            out var released));

        Assert.AreEqual(1, released);
        Assert.AreEqual(49L, bank.HeldXp);
        Assert.AreEqual(4, bank.PendingClaimIndex);
        Assert.AreEqual(1, bank.PendingClaimXp);
        Assert.AreEqual(100, bank.PendingClaimThreshold);
    }

    [TestMethod]
    public void PendingClaimBlocksAnotherClaim()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(50, 0, 0, 0));
        Assert.IsTrue(bank.TryBeginClaim(4, 100, 99, out _));

        Assert.IsFalse(bank.TryBeginClaim(5, 150, 100, out _));
        Assert.AreEqual(49L, bank.HeldXp);
    }

    [TestMethod]
    public void ConfirmedClaimClearsPendingState()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(50, 0, 0, 0));
        Assert.IsTrue(bank.TryBeginClaim(4, 100, 99, out _));

        Assert.IsFalse(bank.TryConfirmClaim(3));
        Assert.IsTrue(bank.TryConfirmClaim(4));
        Assert.IsFalse(bank.IsClaimPending);
        Assert.AreEqual(49L, bank.HeldXp);
    }

    [TestMethod]
    public void RecoveryRollsBackClaimMissingFromCitySave()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(
            heldXp: 49,
            pendingClaimIndex: 4,
            pendingClaimXp: 1,
            pendingClaimThreshold: 100));

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
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(49, 4, 1, 100));

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
    public void RestoreRejectsInconsistentPendingStateAtomically()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(12, 0, 0, 0));

        Assert.IsFalse(bank.TryRestore(20, 4, 0, 100));
        Assert.AreEqual(12L, bank.HeldXp);
        Assert.IsFalse(bank.IsClaimPending);
    }

    [TestMethod]
    public void ReleaseAndDiscardWaitForPendingClaim()
    {
        var bank = new ManualProgressionBank();
        Assert.IsTrue(bank.TryRestore(49, 4, 1, 100));

        Assert.AreEqual(0L, bank.ReleaseHeldXp());
        Assert.IsFalse(bank.DiscardHeldXp());
        Assert.AreEqual(49L, bank.HeldXp);
    }
}
