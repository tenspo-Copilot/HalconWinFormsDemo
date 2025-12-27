using System;
using System.Collections.Generic;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Vision
{
    public static class HalconCameraHelper
    {
        public sealed class InterfaceScanResult
        {
            public string InterfaceName { get; set; } = "";
            public bool Available { get; set; }
            public string Error { get; set; } = "";
        }

        public static string ToHalconInterfaceName(CameraInterfaceType t)
        {
            switch (t)
            {
                case CameraInterfaceType.GigEVision2: return "GigEVision2";
                case CameraInterfaceType.USB3Vision:  return "USB3Vision";
                case CameraInterfaceType.DirectShow:  return "DirectShow";
                default: return "GigEVision2";
            }
        }

        public static CameraInterfaceType FromHalconInterfaceName(string ifName)
        {
            if (string.IsNullOrWhiteSpace(ifName)) return CameraInterfaceType.GigEVision2;
            ifName = ifName.Trim();
            if (ifName.Equals("USB3Vision", StringComparison.OrdinalIgnoreCase)) return CameraInterfaceType.USB3Vision;
            if (ifName.Equals("DirectShow", StringComparison.OrdinalIgnoreCase)) return CameraInterfaceType.DirectShow;
            return CameraInterfaceType.GigEVision2;
        }

        /// <summary>
        /// Scan common interfaces and report availability.
        /// </summary>
        public static List<InterfaceScanResult> ScanInterfaces()
        {
            var names = new[] { "GigEVision2", "GigEVision", "USB3Vision", "GenTL", "DirectShow" };
            var list = new List<InterfaceScanResult>();
            foreach (var n in names)
            {
                var ok = TryCheckInterfaceAvailable(n, out var err);
                list.Add(new InterfaceScanResult { InterfaceName = n, Available = ok, Error = err ?? "" });
            }
            return list;
        }

        public static string AppendDiagnosticHints(string error, CameraInterfaceType it)
        {
            if (string.IsNullOrWhiteSpace(error)) return "";
            var baseErr = error.Trim();

            // Add a few practical hints without overwhelming users.
            var hints = new List<string>();
            if (baseErr.Contains("#8603"))
            {
                hints.Add("检测到接口库不可用（#8603）。请在 MVTec Software Manager 中安装对应 Image Acquisition Interface。\n");
                if (it == CameraInterfaceType.GigEVision2) hints.Add("GigE：确认安装 GigEVision2（或旧版 GigEVision）接口组件。\n");
                if (it == CameraInterfaceType.USB3Vision) hints.Add("USB3：确认安装 USB3Vision 接口组件；若使用 GenTL，请确认 Producer(CTI) 已安装。\n");
            }
            if (baseErr.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hints.Add("可能为超时：检查相机是否被占用、触发/曝光设置、网段/带宽（GigE）或 USB3 带宽。\n");
            }

            return hints.Count == 0 ? baseErr : (baseErr + "\n\n" + string.Join("", hints).Trim());
        }

        /// <summary>
        /// Fallback order by camera interface type.
        /// GigE: GigEVision2 -> GigEVision
        /// USB3: USB3Vision -> DirectShow
        /// </summary>
        public static string[] GetFallbackInterfaces(CameraInterfaceType t)
        {
            switch (t)
            {
                case CameraInterfaceType.GigEVision2:
                    return new[] { "GigEVision2", "GigEVision" };
                case CameraInterfaceType.USB3Vision:
                    return new[] { "USB3Vision", "DirectShow" };
                case CameraInterfaceType.DirectShow:
                    return new[] { "DirectShow" };
                default:
                    return new[] { "GigEVision2", "GigEVision" };
            }
        }

        public static bool TryCheckInterfaceAvailable(string interfaceName, out string error)
        {
            error = null;
            try
            {
                // A lightweight query: if interface dll is missing, this call throws #8603.
                HTuple info, values;
                HOperatorSet.InfoFramegrabber(interfaceName, "info_boards", out info, out values);
                return true;
            }
            catch (HOperatorException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryResolveAvailableInterface(CameraInterfaceType preferred, out string resolved, out string diagnostic)
        {
            diagnostic = "";
            foreach (var ifName in GetFallbackInterfaces(preferred))
            {
                if (TryCheckInterfaceAvailable(ifName, out var err))
                {
                    resolved = ifName;
                    if (ifName != ToHalconInterfaceName(preferred))
                        diagnostic = $"已自动回退到接口：{ifName}";
                    return true;
                }
            }

            resolved = ToHalconInterfaceName(preferred);
            diagnostic =
                $"HALCON 接口不可用：{resolved}。\n" +
                "请使用 MVTec Software Manager 安装对应的图像采集接口（GigEVision2/USB3Vision）。\n" +
                "若你已安装旧接口，可尝试 GigEVision（程序已自动回退尝试）。";
            return false;
        }

        public static List<string> TryListDevices(CameraInterfaceType type, out string error)
        {
            error = null;
            var list = new List<string>();

            if (!TryResolveAvailableInterface(type, out var ifName, out var diag))
            {
                error = diag;
                return list;
            }

            try
            {
                // Net48 HALCON 24.11 signature
                HTuple info, devs;
                HOperatorSet.InfoFramegrabber(ifName, "device", out info, out devs);

                for (int i = 0; i < devs.Length; i++)
                    list.Add(devs[i].S);

                return list;
            }
            catch (HOperatorException ex)
            {
                error = ex.Message;
                return list;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return list;
            }
        }

        public static bool TryTestOpen(CameraConfig cfg, out string error)
        {
            error = null;

            if (!TryResolveAvailableInterface(cfg.InterfaceType, out var ifName, out var diag))
            {
                error = diag;
                return false;
            }

            try
            {
                var port = GetPortTuple(ifName, cfg.Port);
                var candidates = GetDeviceCandidates(ifName, cfg.Device);

                HTuple handle = null;
                Exception last = null;

                foreach (var dev in candidates)
                {
                    try
                    {
                        HOperatorSet.OpenFramegrabber(
                            ifName,
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
                            out handle);

                        // If open succeeded, try one grab.
                        HOperatorSet.GrabImageAsync(out HObject img, handle, -1);
                        img.Dispose();
                        last = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        try { if (handle != null && handle.Length > 0) HOperatorSet.CloseFramegrabber(handle); } catch { }
                        handle = null;
                    }
                }

                if (last != null)
                    throw last;

                try { if (handle != null && handle.Length > 0) HOperatorSet.CloseFramegrabber(handle); } catch { }

                if (!string.IsNullOrWhiteSpace(diag))
                    error = diag; // non-fatal hint
                return true;
            }
            catch (HOperatorException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Returns the correct HALCON "port" tuple for OpenFramegrabber.
        /// Some interfaces (notably USB3Vision) reject numeric 0 and expect "default".
        /// </summary>
        public static HTuple GetPortTuple(string interfaceName, int port)
        {
            // HALCON's OpenFramegrabber expects different *types* for 'port' depending on the interface.
            // For GigE/USB3 Vision interfaces, 'port' is typically numeric (0 = default).
            // For some other interfaces, HALCON accepts the string "default".
            if (port > 0)
                return new HTuple(port);

            if (!string.IsNullOrWhiteSpace(interfaceName))
            {
                var n = interfaceName.Trim();
                if (n.Equals("USB3Vision", System.StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("GigEVision2", System.StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("GigEVision", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new HTuple(0);
                }
            }

            return new HTuple("default");
        }

        /// <summary>
        /// Normalizes the HALCON device identifier for OpenFramegrabber.
        /// Prefer a stable ASCII token (unique_name) when InfoFramegrabber returns localized/descriptive strings.
        /// </summary>
        public static string NormalizeDeviceId(string interfaceName, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "default";

            var s = raw.Trim();

            // If the caller already provides a simple identifier (no separators), keep it.
            // Otherwise, parse tokens separated by '|'.
            if (s.Contains("unique_name:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = s.IndexOf("unique_name:", StringComparison.OrdinalIgnoreCase);
                return s.Substring(idx).Trim();
            }

            if (s.Contains("|"))
            {
                var parts = s.Split('|');
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("unique_name:", StringComparison.OrdinalIgnoreCase))
                        return t;
                }
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("device:", StringComparison.OrdinalIgnoreCase))
                        return t;
                }
            }

            // As a last resort, keep the raw string; this matches many interfaces (e.g., "default", "0").
            return s;
        }

        /// <summary>
        /// Generates candidate device identifiers for OpenFramegrabber, trying stable tokens first.
        /// </summary>
        public static List<HTuple> GetDeviceCandidates(string interfaceName, string raw)
        {
            // For HALCON framegrabbers, the 'device' control parameter is typically expected to be a STRING.
            // Passing an integer can lead to HALCON error #1214 (wrong type).
            var list = new List<HTuple>();

            if (string.IsNullOrWhiteSpace(raw))
            {
                list.Add(new HTuple("default"));
                return list;
            }

            var rawTrim = raw.Trim();

            // Normalize into a compact identifier if possible (e.g. "unique_name:XXXX" or "device:0")
            var norm = NormalizeDeviceId(interfaceName, rawTrim);
            if (!string.IsNullOrWhiteSpace(norm))
            {
                list.Add(new HTuple(norm));

                // If normalized is "unique_name:XXXX", also try the bare value "XXXX" (some systems accept it).
                if (norm.StartsWith("unique_name:", StringComparison.OrdinalIgnoreCase))
                {
                    var v = norm.Substring("unique_name:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(v))
                        list.Add(new HTuple(v));
                }
            }

            // If the raw text contains "unique_name:", extract the token as additional candidates.
            if (rawTrim.IndexOf("unique_name:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    var idx = rawTrim.IndexOf("unique_name:", StringComparison.OrdinalIgnoreCase);
                    var after = rawTrim.Substring(idx + "unique_name:".Length).Trim();
                    var stop = after.IndexOfAny(new[] { ' ', '|', '\t', '\r', '\n' });
                    var token = stop > 0 ? after.Substring(0, stop) : after;
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        list.Add(new HTuple("unique_name:" + token));
                        list.Add(new HTuple(token));
                    }
                }
                catch { /* ignore */ }
            }

            // Parse device token if present (e.g. "device:0"). Keep it as string only.
            if (rawTrim.IndexOf("device:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    var idx = rawTrim.IndexOf("device:", StringComparison.OrdinalIgnoreCase);
                    var after = rawTrim.Substring(idx + "device:".Length).Trim();
                    var stop = after.IndexOfAny(new[] { ' ', '|', '\t', '\r', '\n' });
                    var token = stop > 0 ? after.Substring(0, stop) : after;
                    if (!string.IsNullOrWhiteSpace(token))
                        list.Add(new HTuple("device:" + token));
                }
                catch { /* ignore */ }
            }

            // Also try the original raw string (some interfaces accept it as-is).
            if (!string.Equals(rawTrim, norm, StringComparison.Ordinal))
                list.Add(new HTuple(rawTrim));

            // Common fallback
            list.Add(new HTuple("default"));

            // Deduplicate by string representation.
            var dedup = new List<HTuple>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in list)
            {
                var key = t.ToString();
                if (seen.Add(key))
                    dedup.Add(t);
            }

            return dedup;
        }
    }
}
