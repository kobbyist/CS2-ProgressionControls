using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionInitializationPolicyTests
{
    [TestMethod]
    public void LoadedCityWaitsForItsSaveIdentity()
    {
        Assert.IsFalse(
            ProgressionInitializationPolicy.IsSaveIdentityReady(
                loadedSaveDescriptorAvailable: true,
                loadedSaveIdentityResolved: false));
    }

    [TestMethod]
    public void LoadedCityStartsAfterItsSaveIdentityResolves()
    {
        Assert.IsTrue(
            ProgressionInitializationPolicy.IsSaveIdentityReady(
                loadedSaveDescriptorAvailable: true,
                loadedSaveIdentityResolved: true));
    }

    [TestMethod]
    public void NewCityDoesNotRequireAStoredSaveIdentity()
    {
        Assert.IsTrue(
            ProgressionInitializationPolicy.IsSaveIdentityReady(
                loadedSaveDescriptorAvailable: false,
                loadedSaveIdentityResolved: false));
    }
}
