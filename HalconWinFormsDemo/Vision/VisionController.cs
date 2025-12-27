using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public class VisionController : IDisposable
    {
        private readonly CameraManager cameraManager = new();

        private readonly Dictionary<string, int> frameCount = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Stopwatch> fpsTimer = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> fpsValues = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> cameraStatus = new(StringComparer.OrdinalIgnoreCase);

        public event Action<string, HObject> ImageReady;
        public event Action<string, string> CameraError;
        public event Action<string, bool> CameraOnlineChanged;

        public VisionController()
        {
            cameraManager.ImageArrived += ProcessImage;
            cameraManager.CameraError += (n, msg) =>
            {
                cameraStatus[n] = false;
                CameraError?.Invoke(n, msg);
            };
            cameraManager.CameraOnlineChanged += (n, on) =>
            {
                cameraStatus[n] = on;
                CameraOnlineChanged?.Invoke(n, on);
            };
        }

        public void AddCamera(ICamera cam, TriggerGroup group)
        {
            cameraManager.AddCamera(cam, group);
            frameCount[cam.Name] = 0;
            fpsTimer[cam.Name] = Stopwatch.StartNew();
            fpsValues[cam.Name] = 0;
            cameraStatus[cam.Name] = false;
        }

        public IReadOnlyCollection<string> CameraNames => cameraManager.CameraNames;

        public bool TryGetCamera(string name, out ICamera camera)
        {
            return cameraManager.TryGetCamera(name, out camera);
        }

        public void Start() => cameraManager.StartAll();
        public void Stop() => cameraManager.StopAll();
        public void Clear() => cameraManager.Clear();

        public void TriggerOnceAll() => cameraManager.TriggerAll();
        public void TriggerGroup(TriggerGroup group) => cameraManager.TriggerGroupCapture(group);

        private void ProcessImage(string name, HObject image)
        {
            // Ownership: ImageReady consumer must Dispose() the HObject after display.
            frameCount[name]++;
            if (!fpsTimer.TryGetValue(name, out var sw))
            {
                sw = Stopwatch.StartNew();
                fpsTimer[name] = sw;
            }

            if (sw.ElapsedMilliseconds >= 1000)
            {
                fpsValues[name] = frameCount[name] * 1000.0 / sw.ElapsedMilliseconds;
                frameCount[name] = 0;
                sw.Restart();
            }

            ImageReady?.Invoke(name, image);
        }

        public void Dispose()
        {
            try { Stop(); } catch { }
            try { Clear(); } catch { }
        }
    }
}
