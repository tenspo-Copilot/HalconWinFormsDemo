using HalconDotNet;

namespace HalconWinFormsDemo.Vision
{
    /// <summary>
    /// Provides safe accessors to the underlying HALCON framegrabber parameter API.
    /// This is used by the non-modal tuning window to adjust camera parameters while previewing.
    /// </summary>
    public interface IFramegrabberParamAccess
    {
        bool TrySetParam(string name, object value, out string error);
        bool TryGetParam(string name, out HTuple value, out string error);
        bool TryGetAvailableEasyParams(out string[] names, out string error);
    }
}
