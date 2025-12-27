namespace HalconWinFormsDemo.Models
{
    /// <summary>
    /// View (display slot) to camera mapping. Enforces 1 camera -> 1 view policy (方案B) by validation.
    /// </summary>
    public class ViewMappingSettings
    {
        public string View1 { get; set; } = "Cam1";
        public string View2 { get; set; } = "Cam2";
        public string View3 { get; set; } = "Cam3";
        public string View4 { get; set; } = "Cam4";
        public string View5 { get; set; } = "Cam5";
        public string View6 { get; set; } = "Cam6";
    }
}
