using System;
using HalconDotNet;

namespace HalconWinFormsDemo.Infrastructure
{
    public static class HalconStartupCheck
    {
        public static bool TryCheck(out string message)
        {
            try
            {
                HOperatorSet.GenEmptyObj(out HObject obj);
                obj.Dispose();

                HOperatorSet.GetSystem("version", out HTuple ver);
                message = $"HALCON OK (Version: {ver})";
                return true;
            }
            catch (DllNotFoundException ex)
            {
                message =
                    "未找到 HALCON 原生运行库（DLL）。\r\n" +
                    "请确认已安装 HALCON，并且 HALCON/bin 已加入 PATH，或 HALCONROOT 配置正确。\r\n\r\n" +
                    ex.Message;
                return false;
            }
            catch (BadImageFormatException ex)
            {
                message =
                    "HALCON 位数不匹配（请使用 x64）。\r\n" +
                    "请确认项目平台为 x64，且 HALCON 为 x64 版本。\r\n\r\n" +
                    ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                message =
                    "HALCON 初始化失败。\r\n" +
                    "请检查 HALCON 安装与环境变量。\r\n\r\n" +
                    ex.ToString();
                return false;
            }
        }
    }
}
