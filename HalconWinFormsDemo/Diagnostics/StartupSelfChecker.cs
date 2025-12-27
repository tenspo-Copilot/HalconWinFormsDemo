using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using HalconWinFormsDemo.Vision;

namespace HalconWinFormsDemo.Diagnostics
{
    public sealed class StartupSelfChecker
    {
        public List<SelfCheckItem> RunAll()
        {
            var items = new List<SelfCheckItem>();

            // 1) Process bitness
            items.Add(new SelfCheckItem
            {
                Name = "进程位数（x64）",
                Passed = Environment.Is64BitProcess,
                Detail = Environment.Is64BitProcess ? "当前为 64 位进程" : "当前为 32 位进程",
                Suggestion = "请将项目 Platform target 设置为 x64，并在 64 位系统上运行。"
            });

            // 2) HALCON kernel availability + version
            items.Add(CheckHalconVersion());

            // 2.1) .NET Framework 4.8
            items.Add(CheckDotNet48());

            // 2.2) Common conflicting processes (HDevelop / MVS) that can occupy devices
            items.Add(CheckConflictingProcesses());

            // 2.3) Network adapter snapshot (for GigE troubleshooting)
            items.Add(CheckNetworkAdapters());

            // 3) Acquisition interfaces availability
            items.Add(CheckInterface("GigEVision2"));
            items.Add(CheckInterface("USB3Vision"));
            items.Add(CheckInterface("GenTL"));
            items.Add(CheckInterface("DirectShow"));

            // 3.1) PLC reachability (ping + TCP/502)
            items.Add(CheckPlcReachability());

            // 4) Camera mapping validity (best-effort)
            items.Add(CheckCameraMappingAgainstEnumeration());

            // 4.1) Quick open test for configured cameras (best-effort, limited)
            items.Add(CheckConfiguredCameraOpenTest());

            return items;
        }

        private SelfCheckItem CheckDotNet48()
        {
            try
            {
                // .NET Framework Release key: 528040+ is .NET Framework 4.8 on Windows 10/11
                const int MinRelease = 528040;
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                           .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    var releaseObj = ndpKey?.GetValue("Release");
                    int release = releaseObj is int i ? i : 0;
                    bool ok = release >= MinRelease;
                    return new SelfCheckItem
                    {
                        Name = ".NET Framework 4.8",
                        Passed = ok,
                        Detail = release > 0 ? $"Release={release}" : "未检测到 Release 键",
                        Suggestion = ok ? "" : "请安装 .NET Framework 4.8（Windows 功能或离线安装包）。"
                    };
                }
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = ".NET Framework 4.8",
                    Passed = false,
                    Detail = ex.Message,
                    Suggestion = "请安装 .NET Framework 4.8（Windows 功能或离线安装包）。"
                };
            }
        }

        private SelfCheckItem CheckConflictingProcesses()
        {
            try
            {
                // Common tools that may occupy GigE/USB3 devices.
                // Note: process names are best-effort; different versions may differ.
                var names = new[]
                {
                    "HDevelop",
                    "hdevelop",
                    "MVS",
                    "MVSApp",
                    "MVViewer",
                    "MvCamera",
                    "MvCameraControl",
                    "CameraControl",
                    "HalconStudio"
                };

                var hits = new List<string>();
                foreach (var n in names)
                {
                    try
                    {
                        var ps = Process.GetProcessesByName(n);
                        if (ps != null && ps.Length > 0) hits.Add(n);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                bool ok = hits.Count == 0;
                return new SelfCheckItem
                {
                    Name = "相机占用风险（进程检测）",
                    Passed = ok,
                    Detail = ok ? "未发现常见占用进程" : $"检测到可能占用相机的进程：{string.Join(", ", hits.Distinct())}",
                    Suggestion = ok ? "" : "建议关闭 HDevelop / 海康 MVS 等工具后再进入 REAL 模式连接相机。"
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "相机占用风险（进程检测）",
                    Passed = true,
                    Detail = $"检测失败（忽略）：{ex.Message}",
                    Suggestion = ""
                };
            }
        }

        private SelfCheckItem CheckNetworkAdapters()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var nic in nics)
                {
                    var ipProps = nic.GetIPProperties();
                    var ips = ipProps.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString())
                        .ToList();
                    if (ips.Count == 0) continue;
                    sb.AppendLine($"{nic.Name}: {string.Join(", ", ips)}");
                }

                var detail = sb.Length == 0 ? "未发现活动 IPv4 网卡" : sb.ToString().TrimEnd();
                return new SelfCheckItem
                {
                    Name = "网卡/IPv4（GigE 排障参考）",
                    Passed = true,
                    Detail = detail,
                    Suggestion = "GigE 相机需与网卡同网段；必要时关闭 VPN/虚拟网卡或为专用网卡配置固定 IP。"
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "网卡/IPv4（GigE 排障参考）",
                    Passed = true,
                    Detail = $"读取失败（忽略）：{ex.Message}",
                    Suggestion = ""
                };
            }
        }

        private SelfCheckItem CheckPlcReachability()
        {
            try
            {
                var s = PlcSettingsStore.Load();
                var ips = new[] { ("PLC1", s.PlcAIp), ("PLC2", s.PlcBIp) }
                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
                    .ToList();

                if (ips.Count == 0)
                {
                    return new SelfCheckItem
                    {
                        Name = "PLC 连通性（Ping/Modbus 502）",
                        Passed = true,
                        Detail = "未配置 PLC IP",
                        Suggestion = "可在 PLC 设置中配置两台 PLC 的 IP（MOCK 模式下）。"
                    };
                }

                var results = new List<string>();
                int okCount = 0;
                foreach (var (tag, ipStr) in ips)
                {
                    string one = $"{tag} {ipStr}: ";

                    bool pingOk = false;
                    try
                    {
                        using (var ping = new Ping())
                        {
                            var reply = ping.Send(ipStr, 800);
                            pingOk = reply != null && reply.Status == IPStatus.Success;
                        }
                    }
                    catch
                    {
                        pingOk = false;
                    }

                    bool tcpOk = false;
                    try
                    {
                        using (var client = new TcpClient())
                        {
                            var ar = client.BeginConnect(ipStr, 502, null, null);
                            tcpOk = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(800));
                            if (tcpOk) client.EndConnect(ar);
                        }
                    }
                    catch
                    {
                        tcpOk = false;
                    }

                    one += $"ping={(pingOk ? "OK" : "FAIL")}, tcp502={(tcpOk ? "OK" : "FAIL")}";
                    results.Add(one);
                    if (tcpOk) okCount++;
                }

                bool pass = okCount == ips.Count;
                var suggestion = pass
                    ? ""
                    : "请检查 PLC 是否上电、网线/交换机、IP/网段、防火墙；Modbus TCP 默认端口为 502。";

                return new SelfCheckItem
                {
                    Name = "PLC 连通性（Ping/Modbus 502）",
                    Passed = pass,
                    Detail = string.Join(" | ", results),
                    Suggestion = suggestion
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "PLC 连通性（Ping/Modbus 502）",
                    Passed = false,
                    Detail = ex.Message,
                    Suggestion = "请检查 plc_settings.json 是否损坏；必要时在 PLC 设置中重新保存。"
                };
            }
        }

        private SelfCheckItem CheckHalconVersion()
        {
            try
            {
                HTuple v;
                HOperatorSet.GetSystem("version", out v);
                var ver = v.Length > 0 ? v[0].S : "(unknown)";
                return new SelfCheckItem
                {
                    Name = "HALCON Runtime",
                    Passed = true,
                    Detail = $"HALCON version: {ver}",
                    Suggestion = ""
                };
            }
            catch (DllNotFoundException ex)
            {
                return new SelfCheckItem
                {
                    Name = "HALCON Runtime",
                    Passed = false,
                    Detail = $"HALCON DLL 缺失: {ex.Message}",
                    Suggestion = "请安装 HALCON 24.11 Runtime（并确保 HALCONROOT/注册表正确）。"
                };
            }
            catch (BadImageFormatException ex)
            {
                return new SelfCheckItem
                {
                    Name = "HALCON Runtime",
                    Passed = false,
                    Detail = $"位数不匹配: {ex.Message}",
                    Suggestion = "请确保 HALCON 与程序均为 x64。"
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "HALCON Runtime",
                    Passed = false,
                    Detail = ex.Message,
                    Suggestion = "请确认 HALCON 已正确安装并授权。"
                };
            }
        }

        private SelfCheckItem CheckInterface(string ifName)
        {
            bool ok = HalconCameraHelper.TryCheckInterfaceAvailable(ifName, out var err);
            string suggestion = ok
                ? ""
                : "请在 MVTec Software Manager 中安装对应 Image Acquisition Interface；并避免被 MVS/HDevelop 占用。";

            return new SelfCheckItem
            {
                Name = $"采集接口：{ifName}",
                Passed = ok,
                Detail = ok ? "可用" : err,
                Suggestion = suggestion
            };
        }

        private SelfCheckItem CheckCameraMappingAgainstEnumeration()
        {
            try
            {
                var settings = CameraSettingsStore.Load();
                // Aggregate enumerate: collect [接口::device]
                var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                CollectDevices(CameraInterfaceType.GigEVision2, available);
                CollectDevices(CameraInterfaceType.USB3Vision, available);
                CollectDevices(CameraInterfaceType.DirectShow, available);

                var cams = new[] { settings.Cam1, settings.Cam2, settings.Cam3, settings.Cam4, settings.Cam5, settings.Cam6 };
                int configured = 0;
                int matched = 0;
                foreach (var cfg in cams)
                {
                    if (cfg == null) continue;
                    var dev = (cfg.Device ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(dev) || dev.Equals("default", StringComparison.OrdinalIgnoreCase))
                        continue;
                    configured++;
                    var ifName = HalconCameraHelper.ToHalconInterfaceName(cfg.InterfaceType);
                    var key = $"{ifName}::{dev}";
                    if (available.Contains(key)) matched++;
                }

                bool pass = configured == 0 || matched > 0;
                var detail = configured == 0
                    ? "未配置画面映射（可在相机设置中绑定画面 1~6）"
                    : $"已配置 {configured} 项映射，当前可匹配 {matched} 项";

                var suggestion = configured == 0
                    ? "建议先在 MOCK 模式打开相机设置，完成画面 1~6 的相机绑定并保存。"
                    : (matched == 0
                        ? "当前映射的 [接口+device] 未在枚举结果中出现：请检查相机是否连接、驱动、网段，或重新绑定后保存。"
                        : "");

                return new SelfCheckItem
                {
                    Name = "相机映射（画面 1~6）",
                    Passed = pass,
                    Detail = detail,
                    Suggestion = suggestion
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "相机映射（画面 1~6）",
                    Passed = false,
                    Detail = ex.Message,
                    Suggestion = "请检查 camera_settings.json 是否损坏；必要时删除后重新配置。"
                };
            }
        }

        private SelfCheckItem CheckConfiguredCameraOpenTest()
        {
            try
            {
                var settings = CameraSettingsStore.Load();
                var cams = new[]
                {
                    ("Cam1", settings.Cam1), ("Cam2", settings.Cam2), ("Cam3", settings.Cam3),
                    ("Cam4", settings.Cam4), ("Cam5", settings.Cam5), ("Cam6", settings.Cam6)
                };

                // Only test those with explicit device binding.
                var toTest = cams
                    .Where(c => c.Item2 != null)
                    .Where(c => !string.IsNullOrWhiteSpace(c.Item2.Device) && !c.Item2.Device.Trim().Equals("default", StringComparison.OrdinalIgnoreCase))
                    .Take(6)
                    .ToList();

                if (toTest.Count == 0)
                {
                    return new SelfCheckItem
                    {
                        Name = "相机可打开性（快速测试）",
                        Passed = true,
                        Detail = "未配置 device，跳过快速打开测试",
                        Suggestion = "如需进一步自检，请先在相机设置中为画面 1~6 绑定 [接口::device]。"
                    };
                }

                int okCount = 0;
                var details = new List<string>();
                foreach (var (name, cfg) in toTest)
                {
                    var ok = HalconCameraHelper.TryTestOpen(cfg, out var err);
                    if (ok) okCount++;
                    var shortErr = string.IsNullOrWhiteSpace(err) ? "OK" : err.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? err;
                    details.Add($"{name}: {(ok ? "OK" : "FAIL")} ({shortErr})");
                }

                bool pass = okCount == toTest.Count;
                return new SelfCheckItem
                {
                    Name = "相机可打开性（快速测试）",
                    Passed = pass,
                    Detail = string.Join(" | ", details),
                    Suggestion = pass ? "" : "若 FAIL：请检查相机是否被 MVS/HDevelop 占用、网段/USB3 带宽、以及对应 HALCON 采集接口是否安装齐全。"
                };
            }
            catch (Exception ex)
            {
                return new SelfCheckItem
                {
                    Name = "相机可打开性（快速测试）",
                    Passed = true,
                    Detail = $"测试异常（忽略）：{ex.Message}",
                    Suggestion = ""
                };
            }
        }

        private void CollectDevices(CameraInterfaceType type, HashSet<string> sink)
        {
            try
            {
                var devs = HalconCameraHelper.TryListDevices(type, out _);
                var ifName = HalconCameraHelper.ToHalconInterfaceName(type);
                foreach (var d in devs)
                {
                    if (string.IsNullOrWhiteSpace(d)) continue;
                    sink.Add($"{ifName}::{d}");
                }
            }
            catch
            {
                // Ignore enumeration errors in self-check.
            }
        }
    }
}
