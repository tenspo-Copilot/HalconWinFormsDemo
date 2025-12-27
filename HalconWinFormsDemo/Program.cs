using System;
using System.Windows.Forms;
using HalconWinFormsDemo.Infrastructure;

namespace HalconWinFormsDemo
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!HalconStartupCheck.TryCheck(out var msg))
            {
                MessageBox.Show(msg, "HALCON 环境错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Interface availability check (non-fatal): if missing, user can still run in MOCK mode.
            if (!SystemSelfCheck.TryCheckHalconInterfaces(out var ifMsg))
            {
                MessageBox.Show(ifMsg, "HALCON 接口提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Application.Run(new MainForm());
        }
    }
}
