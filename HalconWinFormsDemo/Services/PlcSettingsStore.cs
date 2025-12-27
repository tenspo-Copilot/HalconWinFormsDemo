using System;
using HalconWinFormsDemo.Infrastructure;
using System.IO;
using System.Text.Json;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class PlcSettingsStore
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HalconWinFormsDemo");

        private static readonly string FilePath = Path.Combine(Dir, "plc_settings.json");

        public static PlcSettings Load()
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                if (!File.Exists(FilePath))
                {
                    var def = new PlcSettings();
                    Save(def);
                    return def;
                }

                var json = File.ReadAllText(FilePath);
                var obj = JsonSerializer.Deserialize<PlcSettings>(json);
                return obj ?? new PlcSettings();
            }
            catch
            {
                return new PlcSettings();
            }
        }

        public static void Save(PlcSettings settings)
        {
            if (AppRuntimeState.ProductionLocked)
                throw new InvalidOperationException("生产模式锁定：禁止保存参数。请切换到 MOCK 后再保存。");

            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            AtomicFile.WriteAllTextAtomic(FilePath, json);
        }
    }
}
