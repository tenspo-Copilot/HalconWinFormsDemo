using System;
using System.Collections.Generic;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public class CameraManager
    {
        private readonly Dictionary<string, ICamera> cameras = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<TriggerGroup, List<string>> groups = new()
        {
            { TriggerGroup.Group1, new List<string>() },
            { TriggerGroup.Group2, new List<string>() },
        };

        private readonly Dictionary<string, bool> cameraOnline = new(StringComparer.OrdinalIgnoreCase);

        public event Action<string, HObject> ImageArrived;
        public event Action<string, string> CameraError;
        public event Action<string, bool> CameraOnlineChanged;

        public IReadOnlyCollection<string> CameraNames => cameras.Keys.ToList().AsReadOnly();

        public bool TryGetCamera(string name, out ICamera camera)
        {
            return cameras.TryGetValue(name, out camera);
        }

        public void AddCamera(ICamera cam, TriggerGroup group)
        {
            if (cam == null) throw new ArgumentNullException(nameof(cam));
            if (cameras.ContainsKey(cam.Name))
                throw new InvalidOperationException($"Camera name already exists: {cam.Name}");

            cameras[cam.Name] = cam;
            groups[group].Add(cam.Name);
            MarkOnline(cam.Name, false);

            cam.ImageArrived += OnCameraImageArrived;
            cam.CameraError += OnCameraError;
        }

        private void OnCameraImageArrived(string name, HObject img)
        {
            MarkOnline(name, true);
            ImageArrived?.Invoke(name, img);
        }

        private void OnCameraError(string name, string message)
        {
            MarkOnline(name, false);
            CameraError?.Invoke(name, message);
        }

        private void MarkOnline(string camName, bool online)
        {
            if (cameraOnline.TryGetValue(camName, out var old) && old == online)
                return;

            cameraOnline[camName] = online;
            CameraOnlineChanged?.Invoke(camName, online);
        }

        public void StartAll()
        {
            foreach (var cam in cameras.Values)
            {
                try
                {
                    cam.Open();
                    cam.Start();
                    MarkOnline(cam.Name, true);
                }
                catch (Exception ex)
                {
                    MarkOnline(cam.Name, false);
                    CameraError?.Invoke(cam.Name, $"[{cam.Name}] Start failed: {ex.Message}");
                    // Continue starting other cameras to avoid whole-app crash.
                }
            }
        }

        public void StopAll()
        {
            foreach (var cam in cameras.Values)
            {
                try { cam.Stop(); } catch { }
            }
        }

        public void TriggerAll()
        {
            foreach (var cam in cameras.Values)
                cam.SoftwareTrigger();
        }

        public void TriggerGroupCapture(TriggerGroup group)
        {
            if (!groups.TryGetValue(group, out var list)) return;
            foreach (var camName in list)
            {
                if (cameras.TryGetValue(camName, out var cam))
                    cam.SoftwareTrigger();
            }
        }

        public void Clear()
        {
            foreach (var cam in cameras.Values)
            {
                try { cam.Stop(); } catch { }
                try { cam.Dispose(); } catch { }
            }

            cameras.Clear();
            groups[TriggerGroup.Group1].Clear();
            groups[TriggerGroup.Group2].Clear();
            cameraOnline.Clear();
        }
    }
}
