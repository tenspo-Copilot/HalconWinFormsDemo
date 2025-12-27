using HalconDotNet;
using System;
using System.Threading.Tasks;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    /// <summary>
    /// MOCK 相机：不依赖真实硬件，但仍使用 HALCON HObject 流程。
    /// 支持自动故障注入（断线/异常）与自动恢复，用于联调报警、重连、统计等。
    /// </summary>
    public class MockCamera : ICamera, IFramegrabberParamAccess
    {
        public string Name { get; }

        public event Action<string, HObject> ImageArrived;
        public event Action<string, string> CameraError;
        public event Action<string, CameraState, string> StatusChanged;

        public CameraState State { get; private set; } = CameraState.Disconnected;
        public string LastError { get; private set; } = string.Empty;
        public int RetryDelayMs { get; private set; } = 0;
        public DateTime NextRetryAt { get; private set; } = DateTime.MinValue;

        private volatile bool running;
        private volatile bool isFaulted;
        private readonly Random rnd = new Random(Guid.NewGuid().GetHashCode());

        // Mock-tunable parameters (mirrors common Consumer|* nodes)
        private bool exposureAuto = false;
        private int exposure = 8000;
        private bool gainAuto = false;
        private double gain = 1.0;
        private int frameId = 0;

        /// <summary>
        /// 每次触发发生故障的概率（0~1）
        /// </summary>
        public double FaultRate { get; set; } = 0.05;

        /// <summary>
        /// 故障后自动恢复时间（ms）
        /// </summary>
        public int RecoveryMs { get; set; } = 1500;

        public MockCamera(string name)
        {
            Name = name;
        }

        public void Open()
        {
            SetState(CameraState.Online, "");
        }

        public void Start()
        {
            running = true;
            SetState(CameraState.Online, "");
        }

        public void Stop()
        {
            running = false;
            SetState(CameraState.Offline, "");
        }

        private void SetState(CameraState st, string err)
        {
            State = st;
            LastError = err ?? string.Empty;
            StatusChanged?.Invoke(Name, st, LastError);
        }

        public void SoftwareTrigger()
        {
            if (!running) return;

            if (isFaulted)
            {
                CameraError?.Invoke(Name, "MOCK: camera is faulted");
                return;
            }

            if (rnd.NextDouble() < FaultRate)
            {
                isFaulted = true;
                CameraError?.Invoke(Name, "MOCK: simulated disconnect");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(RecoveryMs).ConfigureAwait(false);
                    isFaulted = false;
                });
                return;
            }

            // Generate a visible mock image. Brightness is affected by exposure/gain.
            // This enables validating the tuning UI without real hardware.
            frameId++;

            HObject baseImg;
            HOperatorSet.GenImageConst(out baseImg, "byte", 640, 480);

            // Derive a brightness value in [30..230]
            double expNorm = Math.Max(0.0, Math.Min(1.0, exposure / 20000.0));
            double gainNorm = Math.Max(0.0, Math.Min(1.0, (gain - 1.0) / 10.0));
            int gray = 30 + (int)(200 * Math.Max(expNorm, 0.2) * (1.0 + gainNorm));
            if (gray > 230) gray = 230;

            // Moving rectangle to show frame refresh
            int x = (frameId * 12) % 520;
            int y = (frameId * 7) % 360;
            HObject rect;
            HOperatorSet.GenRectangle1(out rect, y, x, y + 120, x + 120);

            HObject outImg;
            HOperatorSet.PaintRegion(rect, baseImg, out outImg, gray, "fill");

            try { rect.Dispose(); } catch { }
            try { baseImg.Dispose(); } catch { }

            ImageArrived?.Invoke(Name, outImg);
        }

        public bool TrySetParam(string name, object value, out string error)
        {
            error = string.Empty;
            try
            {
                if (string.Equals(name, "Consumer|exposure_auto", StringComparison.OrdinalIgnoreCase))
                {
                    exposureAuto = ToBool(value);
                    return true;
                }
                if (string.Equals(name, "Consumer|exposure", StringComparison.OrdinalIgnoreCase))
                {
                    exposure = ToInt(value, exposure);
                    return true;
                }
                if (string.Equals(name, "Consumer|gain_auto", StringComparison.OrdinalIgnoreCase))
                {
                    gainAuto = ToBool(value);
                    return true;
                }
                if (string.Equals(name, "Consumer|gain", StringComparison.OrdinalIgnoreCase))
                {
                    gain = ToDouble(value, gain);
                    return true;
                }

                error = "Unsupported mock parameter";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryGetParam(string name, out HTuple value, out string error)
        {
            value = new HTuple();
            error = string.Empty;
            try
            {
                if (string.Equals(name, "Consumer|exposure_auto", StringComparison.OrdinalIgnoreCase))
                {
                    value = exposureAuto ? new HTuple("On") : new HTuple("Off");
                    return true;
                }
                if (string.Equals(name, "Consumer|exposure", StringComparison.OrdinalIgnoreCase))
                {
                    value = new HTuple(exposure);
                    return true;
                }
                if (string.Equals(name, "Consumer|gain_auto", StringComparison.OrdinalIgnoreCase))
                {
                    value = gainAuto ? new HTuple("On") : new HTuple("Off");
                    return true;
                }
                if (string.Equals(name, "Consumer|gain", StringComparison.OrdinalIgnoreCase))
                {
                    value = new HTuple(gain);
                    return true;
                }

                if (string.Equals(name, "available_easyparam_names", StringComparison.OrdinalIgnoreCase))
                {
                    value = new HTuple(
                        "Consumer|exposure_auto",
                        "Consumer|exposure",
                        "Consumer|gain_auto",
                        "Consumer|gain");
                    return true;
                }

                error = "Unsupported mock parameter";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryGetAvailableEasyParams(out string[] names, out string error)
        {
            error = string.Empty;
            names = new[]
            {
                "Consumer|exposure_auto",
                "Consumer|exposure",
                "Consumer|gain_auto",
                "Consumer|gain",
                "Consumer|info_general",
                "Consumer|trigger",
                "Consumer|trigger_activation",
                "Consumer|trigger_delay",
                "Consumer|trigger_software"
            };
            return true;
        }

        private static bool ToBool(object v)
        {
            if (v == null) return false;
            var s = v.ToString() ?? "";
            if (string.Equals(s, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "True", StringComparison.OrdinalIgnoreCase) || s == "1")
                return true;
            if (string.Equals(s, "Off", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "False", StringComparison.OrdinalIgnoreCase) || s == "0")
                return false;
            bool b;
            if (bool.TryParse(s, out b)) return b;
            int i;
            if (int.TryParse(s, out i)) return i != 0;
            return false;
        }

        private static int ToInt(object v, int fallback)
        {
            if (v == null) return fallback;
            if (v is int) return (int)v;
            int i;
            if (int.TryParse(v.ToString(), out i)) return i;
            double d;
            if (double.TryParse(v.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d))
                return (int)d;
            return fallback;
        }

        private static double ToDouble(object v, double fallback)
        {
            if (v == null) return fallback;
            if (v is double) return (double)v;
            double d;
            if (double.TryParse(v.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
            int i;
            if (int.TryParse(v.ToString(), out i)) return i;
            return fallback;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
