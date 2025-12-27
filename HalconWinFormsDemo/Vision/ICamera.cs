using HalconDotNet;
using System;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public interface ICamera : IDisposable
    {
        string Name { get; }
        event Action<string, HObject> ImageArrived;
        event Action<string, string> CameraError;

        // 统一状态事件：用于主界面显示 ONLINE/OFFLINE/重试倒计时等
        event Action<string, CameraState, string> StatusChanged;

        CameraState State { get; }
        string LastError { get; }
        int RetryDelayMs { get; }
        DateTime NextRetryAt { get; }

        void Open();
        void Start();
        void Stop();
        void SoftwareTrigger();
    }
}
