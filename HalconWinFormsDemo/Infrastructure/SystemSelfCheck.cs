using System;
using System.Text;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using HalconWinFormsDemo.Vision;

namespace HalconWinFormsDemo.Infrastructure
{
    public static class SystemSelfCheck
    {
        public static bool TryCheckHalconInterfaces(out string message)
        {
            var sb = new StringBuilder();

            var camSettings = CameraSettingsStore.Load();

            var cams = new[]
            {
                camSettings.Cam1, camSettings.Cam2, camSettings.Cam3,
                camSettings.Cam4, camSettings.Cam5, camSettings.Cam6
            };

            bool ok = true;
            foreach (var cfg in cams)
            {
                if (!HalconCameraHelper.TryResolveAvailableInterface(cfg.InterfaceType, out var resolved, out var diag))
                {
                    ok = false;
                    sb.AppendLine($"[{cfg.Name}] 接口不可用：{HalconCameraHelper.ToHalconInterfaceName(cfg.InterfaceType)}");
                    sb.AppendLine(diag);
                    sb.AppendLine();
                }
            }

            if (ok)
                sb.AppendLine("HALCON 图像采集接口检查通过。");
            else
                sb.AppendLine("提示：当前可继续使用 MOCK 模式运行；要启用 REAL 模式，请先安装对应 HALCON 接口组件。");

            message = sb.ToString();
            return ok;
        }
    }
}
