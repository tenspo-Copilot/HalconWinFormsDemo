using System;
using System.IO;
using System.Text.Json;
using HalconWinFormsDemo.Infrastructure;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    /// <summary>
    /// Persists view-to-camera mapping (view_mapping.json) next to the executable.
    /// </summary>
    public static class ViewMappingStore
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "view_mapping.json");

        public static void Save(ViewMappingSettings settings)
        {
            if (AppRuntimeState.ProductionLocked)
                return;

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            AtomicFile.WriteAllTextAtomic(FilePath, json);
        }

        public static ViewMappingSettings Load()
        {
            if (!File.Exists(FilePath))
                return new ViewMappingSettings();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ViewMappingSettings>(json) ?? new ViewMappingSettings();
        }
    }
}
