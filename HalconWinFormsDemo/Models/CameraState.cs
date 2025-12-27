namespace HalconWinFormsDemo.Models
{
    public enum CameraState
    {
        // Keep Offline for UI semantics; map it to the same numeric value as Disconnected
        // to remain backward/forward compatible across versions.
        Offline = 0,
        Disconnected = 0,
        Connecting = 1,
        Online = 2
    }
}
