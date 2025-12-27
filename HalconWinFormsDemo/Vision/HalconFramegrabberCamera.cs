using System;
using System.Threading;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public class HalconFramegrabberCamera : ICamera, IFramegrabberParamAccess
    {
        public string Name { get; }

        private readonly CameraConfig config;

        private volatile bool running;
        private HTuple acqHandle = new HTuple();
        private readonly object handleLock = new();

        
        public CameraState State { get; private set; } = CameraState.Disconnected;
        public string LastError { get; private set; } = string.Empty;

        private int retryDelayMs = 1000;
        private const int MaxRetryDelayMs = 30000;
        private DateTime nextRetryAt = DateTime.MinValue;

        public int RetryDelayMs => retryDelayMs;
        public DateTime NextRetryAt => nextRetryAt;

        public event Action<string, CameraState, string> StatusChanged;
        public event Action<string, HObject> ImageArrived;
        public event Action<string, string> CameraError;

        public HalconFramegrabberCamera(CameraConfig cfg)
        {
            config = cfg;
            Name = cfg.Name;
        }

        
        private void SetState(CameraState state, string err = "")
        {
            State = state;
            LastError = err ?? "";
            StatusChanged?.Invoke(Name, state, LastError);
        }

        private void ScheduleRetry()
        {
            retryDelayMs = Math.Min(retryDelayMs * 2, MaxRetryDelayMs);
            nextRetryAt = DateTime.Now.AddMilliseconds(retryDelayMs);
        }

        public void ForceReconnect()
        {
            lock (handleLock)
            {
                CloseInternal();
                retryDelayMs = 1000;
                nextRetryAt = DateTime.MinValue;
            }
            SetState(CameraState.Disconnected, "manual reconnect");
        }

public void Open()
        {
            SetState(CameraState.Connecting);

            try
            {
                lock (handleLock)
                {
                    CloseInternal();

                    if (!HalconCameraHelper.TryResolveAvailableInterface(config.InterfaceType, out var interfaceName, out var diagnostic))
                    {
                        SetState(CameraState.Disconnected, "接口不可用或缺失");
                        ScheduleRetry();
                        throw new InvalidOperationException($"[{Name}] HALCON 接口不可用: {HalconCameraHelper.ToHalconInterfaceName(config.InterfaceType)}. 详情: {diagnostic}");
                    }

                    var port = HalconCameraHelper.GetPortTuple(interfaceName, config.Port);
                    var candidates = HalconCameraHelper.GetDeviceCandidates(interfaceName, config.Device);

                    Exception last = null;
                    foreach (var dev in candidates)
                    {
                        try
                        {
                        HOperatorSet.OpenFramegrabber(
                                interfaceName,
                                0, 0, 0, 0, 0, 0,
                                "default",
                -1,
                "default",
                -1,
                "false",
                "default",
                                dev,
                                port,
                                -1,
                                out acqHandle);
                            last = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            last = ex;
                            try { if (acqHandle != null && acqHandle.Length > 0) HOperatorSet.CloseFramegrabber(acqHandle); } catch { }
                            acqHandle = new HTuple();
                        }
                    }

                    if (last != null)
                        throw last;

                    retryDelayMs = 1000;
                    nextRetryAt = DateTime.MinValue;
                    SetState(CameraState.Online);

                    ApplyPersistedFramegrabberParamsBestEffort();

                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[{Name}] OpenFramegrabber failed: {ex.Message}", ex);
            }
        }

        public void Start()
        {
            running = true;
        }

        public void Stop()
        {
            running = false;
        }

        public void Dispose()
        {
            running = false;
            lock (handleLock)
            {
                CloseInternal();
            }
        }

        public void SoftwareTrigger()
        {
            if (!running) return;

            // Auto reconnect with backoff
            if (DateTime.Now < nextRetryAt)
                return;

            try
            {
                lock (handleLock)
                {
                    if (acqHandle == null || acqHandle.Length == 0)
                    {
                        try
                        {
                            Open();
                        }
                        catch (Exception exOpen)
                        {
                            SetState(CameraState.Disconnected, exOpen.Message);
                            ScheduleRetry();
                            CameraError?.Invoke(Name, exOpen.Message);
                            return;
                        }
                    }

                    // Trigger mode depends on camera configuration; for many GenICam devices, soft-trigger requires parameters.
                    // Here we just grab an image; users should set camera to 'FreeRun' or software-trigger capable in the camera config tool.
                    HOperatorSet.GrabImageAsync(out HObject img, acqHandle, -1);
                    ImageArrived?.Invoke(Name, img);
                }
            }
            catch (Exception ex)
            {
                SetState(CameraState.Disconnected, ex.Message);
                ScheduleRetry();
                CameraError?.Invoke(Name, ex.Message);
            }
        }

        private void CloseInternal()
        {
            try
            {
                if (acqHandle != null && acqHandle.Length > 0)
                {
                    HOperatorSet.CloseFramegrabber(acqHandle);
                }
            }
            catch { }
            finally
            {
                acqHandle = new HTuple();
            }
        }

        /// <summary>
        /// Some GenICam stacks refuse writing parameters while streaming.
        /// As a last resort for tuning, we reopen the framegrabber handle to
        /// enter a clean state, then parameter writes typically succeed.
        /// This method never throws.
        /// </summary>
        private bool ReopenHandle_NoThrow(out string error)
        {
            error = string.Empty;
            try
            {
                CloseInternal();

                if (!HalconCameraHelper.TryResolveAvailableInterface(config.InterfaceType, out var interfaceName, out var diagnostic))
                {
                    error = $"接口不可用或缺失: {diagnostic}";
                    return false;
                }

                var port = HalconCameraHelper.GetPortTuple(interfaceName, config.Port);
                var candidates = HalconCameraHelper.GetDeviceCandidates(interfaceName, config.Device);

                Exception last = null;
                foreach (var dev in candidates)
                {
                    try
                    {
                        HOperatorSet.OpenFramegrabber(
                            interfaceName,
                            0, 0, 0, 0, 0, 0,
                            "default",
                            -1,
                            "default",
                            -1,
                            "false",
                            "default",
                            dev,
                            port,
                            -1,
                            out acqHandle);
                        last = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        try { if (acqHandle != null && acqHandle.Length > 0) HOperatorSet.CloseFramegrabber(acqHandle); } catch { }
                        acqHandle = new HTuple();
                    }
                }

                if (last != null)
                {
                    error = last.Message;
                    return false;
                }

                // Keep camera state coherent.
                SetState(CameraState.Online);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void ApplyPersistedFramegrabberParamsBestEffort()
        {
            if (config == null) return;
            var dict = config.FramegrabberParams;
            if (dict == null || dict.Count == 0) return;

            foreach (var kv in dict)
            {
                try
                {
                    // Black level is not enabled in this project variant; ignore persisted values to avoid driver errors.
                    var kNorm = NormalizeParamName(kv.Key);
                    if (kNorm != null && kNorm.IndexOf("black", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    object valueObj = kv.Value;
                    // Try parse numeric
                    double d;
                    int i;
                    if (int.TryParse(kv.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out i))
                        valueObj = i;
                    else if (double.TryParse(kv.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d))
                        valueObj = d;

                    string err;
                    TrySetParam(kv.Key, valueObj, out err);
                }
                catch { }
            }
        }

        public bool TrySetParam(string name, object value, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(name)) { error = "param name is empty"; return false; }

            // Defensive normalization: some UI layers may pass display labels
            // (e.g. Chinese "曝光") while HALCON expects the easyparam key.
            name = NormalizeParamName(name);

            // GenICam producers are not consistent about which parameter keys they expose.
            // Even if HALCON lists Consumer|* easyparams, some stacks only allow writing
            // the underlying GenICam node names (e.g. ExposureTime/Gain/ExposureAuto).
            // We therefore try a small set of candidate names for critical parameters.
            var nameCandidates = GetCandidateParamNames(name);

            // HALCON framegrabber parameters vary by interface/driver. Some devices are strict about
            // value types (int/double/string) and allowed string tokens (On/Off vs True/False vs 1/0).
            // Implement a robust, ordered fallback to avoid operator error #5329 where possible.
            try
            {
                lock (handleLock)
                {
                    if (acqHandle == null || acqHandle.Length == 0)
                    {
                        error = "acq_handle is not open";
                        return false;
                    }

                    // Try all candidate parameter keys.
                    foreach (var candidate in nameCandidates)
                    {
                        if (TrySetParamSingleName_NoThrow(candidate, value, out error))
                            return true;
                    }

                    // Some camera stacks forbid changing key parameters while a grab is pending.
                    // In that case HALCON commonly reports error #5329. We try to abort any pending
                    // grab request, then re-apply the parameter once.
                    if (!string.IsNullOrEmpty(error) && (error.Contains("#5329") || error.Contains("5329")))
                    {
                        try
                        {
                            // Best-effort. This parameter is present in the framegrabber layer.
                            HOperatorSet.SetFramegrabberParam(acqHandle, "do_abort_grab", 1);
                        }
                        catch { }

                        // Retry once after abort for all candidates.
                        string err2 = string.Empty;
                        foreach (var candidate in nameCandidates)
                        {
                            if (TrySetParamSingleName_NoThrow(candidate, value, out err2))
                            {
                                error = string.Empty;
                                return true;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(err2)) error = err2;

                        // Some USB3Vision / GenICam stacks still refuse parameter writes while
                        // the stream is active (even after abort). As a last resort, temporarily
                        // stop the preview (running=false), reopen the framegrabber handle, then
                        // apply the parameter once. This is heavier but makes tuning reliable.
                        if (!string.IsNullOrEmpty(error) && (error.Contains("#5329") || error.Contains("5329")))
                        {
                            var prevRunning = running;
                            try
                            {
                                running = false; // stop SoftwareTrigger grabs
                                if (ReopenHandle_NoThrow(out var reopenErr))
                                {
                                    string err3 = string.Empty;
                                    foreach (var candidate in nameCandidates)
                                    {
                                        if (TrySetParamSingleName_NoThrow(candidate, value, out err3))
                                        {
                                            error = string.Empty;
                                            return true;
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(err3)) error = err3;
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(reopenErr)) error = reopenErr;
                                }
                            }
                            finally
                            {
                                running = prevRunning;
                            }
                        }
                    }

                    // 1) Bool-like fallbacks
                    if (value is bool b)
                    {
                        foreach (var candidate in nameCandidates)
                        {
                            if (TrySetParamSingleName_NoThrow(candidate, b ? "On" : "Off", out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, b ? "True" : "False", out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, b ? 1 : 0, out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, b ? "1" : "0", out error)) return true;
                        }
                    }

                    // 2) Numeric fallbacks (int/double/string)
                    if (value is int i)
                    {
                        foreach (var candidate in nameCandidates)
                        {
                            if (TrySetParamSingleName_NoThrow(candidate, (double)i, out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, i.ToString(System.Globalization.CultureInfo.InvariantCulture), out error)) return true;
                        }
                    }
                    else if (value is double d)
                    {
                        foreach (var candidate in nameCandidates)
                        {
                            if (TrySetParamSingleName_NoThrow(candidate, (int)Math.Round(d), out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, d.ToString(System.Globalization.CultureInfo.InvariantCulture), out error)) return true;
                        }
                    }
                    else if (value is decimal m)
                    {
                        var dd = (double)m;
                        foreach (var candidate in nameCandidates)
                        {
                            if (TrySetParamSingleName_NoThrow(candidate, dd, out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, (int)Math.Round(dd), out error)) return true;
                            if (TrySetParamSingleName_NoThrow(candidate, dd.ToString(System.Globalization.CultureInfo.InvariantCulture), out error)) return true;
                        }
                    }

                    // 3) Exposure/Gain vendor quirks: some stacks accept alternate tokens
                    if (name.EndsWith("_auto", StringComparison.OrdinalIgnoreCase) && value is string s)
                    {
                        // Common variants: Off/On, Continuous, Once
                        if (string.Equals(s, "Off", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "On", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var candidate in nameCandidates)
                                if (TrySetParamSingleName_NoThrow(candidate, s, out error)) return true;
                        }
                        foreach (var candidate in nameCandidates)
                            if (TrySetParamSingleName_NoThrow(candidate, "Continuous", out error)) return true;
                    }

                    // No fallback succeeded.
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private bool TrySetParamSingleName_NoThrow(string name, object value, out string error)
        {
            return TrySetParamCore_NoThrow(name, value, out error);
        }

        private bool TrySetParamCore_NoThrow(string name, object value, out string error)
        {
            error = string.Empty;
            try
            {
                HTuple paramValue = value is HTuple ht ? ht : new HTuple(value);
                HOperatorSet.SetFramegrabberParam(acqHandle, name, paramValue);
                return true;
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
            if (string.IsNullOrWhiteSpace(name)) { error = "param name is empty"; return false; }

            name = NormalizeParamName(name);
            var nameCandidates = GetCandidateParamNames(name);

            try
            {
                lock (handleLock)
                {
                    if (acqHandle == null || acqHandle.Length == 0)
                    {
                        error = "acq_handle is not open";
                        return false;
                    }
                    foreach (var candidate in nameCandidates)
                    {
                        try
                        {
                            HOperatorSet.GetFramegrabberParam(acqHandle, candidate, out value);
                            error = string.Empty;
                            return true;
                        }
                        catch (Exception exGet)
                        {
                            // keep last error and continue
                            error = exGet.Message;
                            value = new HTuple();
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string[] GetCandidateParamNames(string normalizedName)
        {
            // Always try the requested name first.
            var list = new System.Collections.Generic.List<string>(8) { normalizedName };

            // Standard GenICam node names commonly exposed by different producers.
            switch (normalizedName)
            {
                case "Consumer|exposure":
                    list.Add("ExposureTime");
                    list.Add("ExposureTimeAbs");
                    list.Add("ExposureTimeRaw");
                    break;
                case "Consumer|gain":
                    list.Add("Gain");
                    list.Add("GainRaw");
                    list.Add("GainAbs");
                    break;
                case "Consumer|exposure_auto":
                    list.Add("ExposureAuto");
                    list.Add("ExposureAutoMode");
                    break;
                case "Consumer|gain_auto":
                    list.Add("GainAuto");
                    list.Add("GainAutoMode");
                    break;
                case "GammaEnable":
                    // Some producers expose GammaEnable / GammaEnabled
                    list.Add("GammaEnabled");
                    list.Add("Consumer|gamma_enable");
                    break;
                case "Gamma":
                    // Standard GenICam nodes
                    list.Add("Consumer|gamma");
                    list.Add("GammaAbs");
                    list.Add("GammaRaw");
                    break;
                case "BlackLevel":
                    list.Add("Consumer|black_level");
                    list.Add("BlackLevelAbs");
                    list.Add("BlackLevelRaw");
                    break;
            }

            // De-duplicate while preserving order.
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var outList = new System.Collections.Generic.List<string>(list.Count);
            foreach (var s in list)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (seen.Add(s)) outList.Add(s);
            }
            return outList.ToArray();
        }

        private static string NormalizeParamName(string name)
        {
            // Keep fast path for already-normalized names.
            if (name.IndexOf('|') >= 0) return name;

            // Known display-label fallbacks.
            switch (name.Trim())
            {
                case "曝光自动":
                case "自动曝光":
                    return "Consumer|exposure_auto";
                case "曝光":
                    return "Consumer|exposure";
                case "增益自动":
                case "自动增益":
                    return "Consumer|gain_auto";
                case "增益":
                    return "Consumer|gain";
                case "伽马":
                    return "Gamma";
                case "伽马使能":
                case "启用伽马":
                    return "GammaEnable";
                case "黑电平":
                    return "BlackLevel";
                default:
                    return name;
            }
        }

        public bool TryGetAvailableEasyParams(out string[] names, out string error)
        {
            names = new string[0];
            error = string.Empty;
            try
            {
                HTuple t;
                if (!TryGetParam("available_easyparam_names", out t, out error))
                    return false;

                var list = new System.Collections.Generic.List<string>();
                for (int idx = 0; idx < t.Length; idx++)
                {
                    try
                    {
                        var s = t[idx].S;
                        if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
                    }
                    catch { }
                }
                names = list.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
