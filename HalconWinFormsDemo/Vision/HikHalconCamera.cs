using HalconDotNet;
using System;
using System.Threading;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public class HikHalconCamera : ICamera
    {
        public string Name { get; }
        private readonly string device;

        public CameraState State { get; private set; } = CameraState.Disconnected;
        public string LastError { get; private set; } = string.Empty;
        public int RetryDelayMs { get; private set; } = 0;
        public DateTime NextRetryAt { get; private set; } = DateTime.MinValue;

        private HTuple acqHandle = new HTuple();
        private Thread grabThread;
        private readonly AutoResetEvent triggerEvent = new(false);
        private volatile bool running;

        public event Action<string, HObject> ImageArrived;
        public event Action<string, string> CameraError;
        public event Action<string, CameraState, string> StatusChanged;

        private void SetState(CameraState state, string error = "")
        {
            State = state;
            LastError = error ?? string.Empty;
            StatusChanged?.Invoke(Name, State, LastError);
        }

        public HikHalconCamera(string name, string device)
        {
            Name = name;
            this.device = device;
        }

        public void Open()
        {
            SetState(CameraState.Connecting);
            try
            {
                var port = HalconCameraHelper.GetPortTuple("GigEVision2", 0);
                HOperatorSet.OpenFramegrabber(
                    "GigEVision2",
                    0, 0, 0, 0, 0, 0,
                    "default",
                    -1,
                    "default",
                    -1,
                    "false",
                    "default",
                    device,
                    port,
                    -1,
                    out acqHandle);

                // Best-effort trigger params (may vary by interface/camera)
                TrySetParam("TriggerMode", "On");
                TrySetParam("TriggerSource", "Software");

                SetState(CameraState.Online);
            }
            catch (Exception ex)
            {
                SetState(CameraState.Disconnected, ex.Message);
                throw;
            }
        }

        private void TrySetParam(string name, string value)
        {
            try { HOperatorSet.SetFramegrabberParam(acqHandle, name, value); }
            catch { }
        }

        public void Start()
        {
            running = true;
            grabThread = new Thread(GrabLoop) { IsBackground = true, Name = $"Grab_{Name}" };
            grabThread.Start();
        }

        private void GrabLoop()
        {
            while (running)
            {
                triggerEvent.WaitOne();
                if (!running) break;

                try
                {
                    HOperatorSet.GrabImageAsync(out HObject img, acqHandle, -1);
                    ImageArrived?.Invoke(Name, img);
                }
                catch (Exception ex)
                {
                    CameraError?.Invoke(Name, ex.Message);
                    SetState(CameraState.Disconnected, ex.Message);
                }
            }
        }

        public void SoftwareTrigger() => triggerEvent.Set();

        public void Stop()
        {
            running = false;
            triggerEvent.Set();
            try { grabThread?.Join(500); } catch { }

            try
            {
                if (acqHandle != null && acqHandle.Length > 0)
                    HOperatorSet.CloseFramegrabber(acqHandle);
            }
            catch { }
            finally
            {
                acqHandle = new HTuple();
            }

            SetState(CameraState.Disconnected);
        }

        public void Dispose()
        {
            Stop();
            triggerEvent.Dispose();
        }
    }
}
