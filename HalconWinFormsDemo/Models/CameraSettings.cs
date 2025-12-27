namespace HalconWinFormsDemo.Models
{
    public enum CameraInterfaceType
    {
        GigEVision2 = 0,
        USB3Vision = 1,
        DirectShow = 2
    }

    public class CameraConfig
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public CameraInterfaceType InterfaceType { get; set; } = CameraInterfaceType.GigEVision2;

        /// <summary>
        /// HALCON device string. For GigE/USB3Vision, this is typically a GenICam device identifier.
        /// </summary>
        public string Device { get; set; } = "default";

        /// <summary>
        /// HALCON port parameter (HALCON 24.11 OpenFramegrabber requires this).
        /// Usually 0 or "default".
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Persisted framegrabber/camera parameters (GenICam EasyParams etc.).
        /// Key examples: "Consumer|exposure", "Consumer|gain".
        /// Values are stored as invariant strings for portability.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> FramegrabberParams { get; set; }
            = new System.Collections.Generic.Dictionary<string, string>();
    }

    public class CameraSettings
    {
        public CameraConfig Cam1 { get; set; } = new() { Name = "Cam1" };
        public CameraConfig Cam2 { get; set; } = new() { Name = "Cam2" };
        public CameraConfig Cam3 { get; set; } = new() { Name = "Cam3" };
        public CameraConfig Cam4 { get; set; } = new() { Name = "Cam4" };
        public CameraConfig Cam5 { get; set; } = new() { Name = "Cam5" };
        public CameraConfig Cam6 { get; set; } = new() { Name = "Cam6" };
    }
}
