using System;
using HalconWinFormsDemo.Infrastructure;
using System.IO;
using System.Text.Json;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class CameraSettingsStore
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "camera_settings.json");

        

        private static string BuildKey(CameraConfig cfg)
        {
            if (cfg == null) return "";
            var dev = (cfg.Device ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dev) || dev.Equals("default", StringComparison.OrdinalIgnoreCase))
                return "";
            var ifName = cfg.InterfaceType switch
            {
                CameraInterfaceType.GigEVision2 => "GigEVision2",
                CameraInterfaceType.USB3Vision => "USB3Vision",
                CameraInterfaceType.DirectShow => "DirectShow",
                _ => "GigEVision2"
            };
            return $"{ifName}::{dev}";
        }

        /// <summary>
        /// 方案B：禁止同一物理相机（接口::device）被重复分配到多个 Cam 槽位。
        /// 若发现重复，仅保留最先出现的槽位，其余槽位清空 Device（避免启动崩溃与死循环）。
        /// </summary>
        private static bool EnsureUniqueDevices(CameraSettings s)
        {
            if (s == null) return false;

            var list = new[]
            {
                s.Cam1, s.Cam2, s.Cam3, s.Cam4, s.Cam5, s.Cam6
            };

            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changed = false;

            foreach (var cfg in list)
            {
                var key = BuildKey(cfg);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (seen.Contains(key))
                {
                    cfg.Device = "";
                    changed = true;
                }
                else
                {
                    seen.Add(key);
                }
            }

            return changed;
        }

public static void Save(CameraSettings settings)
        {
            if (AppRuntimeState.ProductionLocked)
                throw new InvalidOperationException("生产模式锁定：禁止保存参数。请切换到 MOCK 后再保存。");

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            AtomicFile.WriteAllTextAtomic(FilePath, json);
        }

        public static CameraSettings Load()
        {
            if (!File.Exists(FilePath))
                return new CameraSettings();

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<CameraSettings>(json) ?? new CameraSettings();

            // 方案B：启动自愈，避免“重复映射->OpenFramegrabber失败->下次仍失败”的死循环
            var changed = EnsureUniqueDevices(settings);
            if (changed)
            {
                try
                {
                    if (!AppRuntimeState.ProductionLocked)
                        Save(settings);
                }
                catch
                {
                    // Ignore persistence errors; in-memory fix still prevents startup crash.
                }
            }

            return settings;
        }
    }
}
