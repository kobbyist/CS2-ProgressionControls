namespace Kobbyist.ProgressionControls.Core
{
    internal static class ProgressionInitializationPolicy
    {
        public static bool IsSaveIdentityReady(
            bool loadedSaveDescriptorAvailable,
            bool loadedSaveIdentityResolved)
        {
            return !loadedSaveDescriptorAvailable ||
                loadedSaveIdentityResolved;
        }
    }
}
