using System;
using System.Net;
using System.Windows.Forms;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo.Forms
{
    public partial class PlcSettingsForm : Form
    {
        private PlcSettings settings = null!;

        public PlcSettings CurrentSettings => settings;

        public PlcSettingsForm()
        {
            InitializeComponent();
            settings = PlcSettingsStore.Load();
            LoadToUI();
        }

        private void LoadToUI()
        {
            txtPlcAIp.Text = settings.PlcAIp;
            txtPlcBIp.Text = settings.PlcBIp;
            txtPlcAAlarmReg.Text = settings.PlcAAlarmRegister.ToString();
            txtPlcBAlarmReg.Text = settings.PlcBAlarmRegister.ToString();
            txtSlaveId.Text = settings.SlaveId.ToString();
        }

        private bool ValidateInputs(out string error)
        {
            error = "";

            if (!IsValidIp(txtPlcAIp.Text.Trim())) { error = "PLC A IP 格式不正确"; return false; }
            if (!IsValidIp(txtPlcBIp.Text.Trim())) { error = "PLC B IP 格式不正确"; return false; }

            if (!ushort.TryParse(txtPlcAAlarmReg.Text.Trim(), out _)) { error = "PLC A 报警寄存器必须为数字"; return false; }
            if (!ushort.TryParse(txtPlcBAlarmReg.Text.Trim(), out _)) { error = "PLC B 报警寄存器必须为数字"; return false; }
            if (!byte.TryParse(txtSlaveId.Text.Trim(), out _)) { error = "SlaveId 必须为数字（0~255）"; return false; }

            return true;
        }

        private static bool IsValidIp(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.PlcAIp = txtPlcAIp.Text.Trim();
            settings.PlcBIp = txtPlcBIp.Text.Trim();
            settings.PlcAAlarmRegister = ushort.Parse(txtPlcAAlarmReg.Text.Trim());
            settings.PlcBAlarmRegister = ushort.Parse(txtPlcBAlarmReg.Text.Trim());
            settings.SlaveId = byte.Parse(txtSlaveId.Text.Trim());

            PlcSettingsStore.Save(settings);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnTestPlcA_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var svc = new ModbusPlcService("PLC_A");
            svc.Configure(txtPlcAIp.Text.Trim(), byte.Parse(txtSlaveId.Text.Trim()));
            bool ok = svc.TestConnection();
            svc.Dispose();
            MessageBox.Show(ok ? "PLC A 连接正常" : "PLC A 连接失败", "测试", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private void btnTestPlcB_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var svc = new ModbusPlcService("PLC_B");
            svc.Configure(txtPlcBIp.Text.Trim(), byte.Parse(txtSlaveId.Text.Trim()));
            bool ok = svc.TestConnection();
            svc.Dispose();
            MessageBox.Show(ok ? "PLC B 连接正常" : "PLC B 连接失败", "测试", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private void btnPlcAOn_Click(object sender, EventArgs e)
        {
            WriteAlarmTest(true, true);
        }

        private void btnPlcAOff_Click(object sender, EventArgs e)
        {
            WriteAlarmTest(true, false);
        }

        private void btnPlcBOn_Click(object sender, EventArgs e)
        {
            WriteAlarmTest(false, true);
        }

        private void btnPlcBOff_Click(object sender, EventArgs e)
        {
            WriteAlarmTest(false, false);
        }

        private void WriteAlarmTest(bool isPlcA, bool on)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ip = isPlcA ? txtPlcAIp.Text.Trim() : txtPlcBIp.Text.Trim();
            ushort reg = isPlcA ? ushort.Parse(txtPlcAAlarmReg.Text.Trim()) : ushort.Parse(txtPlcBAlarmReg.Text.Trim());
            byte sid = byte.Parse(txtSlaveId.Text.Trim());

            var svc = new ModbusPlcService(isPlcA ? "PLC_A" : "PLC_B");
            svc.Configure(ip, sid);
            svc.EnsureConnected();
            svc.SetAlarmRegister(reg, on);
            svc.Dispose();

            MessageBox.Show($"{(isPlcA ? "PLC A" : "PLC B")} 报警寄存器写入 {(on ? 1 : 0)} 已下发（若 PLC 侧有延迟，请观察数秒）", "测试",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
