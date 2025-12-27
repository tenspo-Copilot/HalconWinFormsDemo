namespace HalconWinFormsDemo.Infrastructure
{
    /// <summary>
    /// Global runtime flags. Used to enforce production lock beyond UI.
    /// </summary>
    public static class AppRuntimeState
    {
        /// <summary>
        /// True when app is in REAL (production) mode and settings must be locked.
        /// </summary>
        public static bool ProductionLocked { get; set; } = false;
    }
}
