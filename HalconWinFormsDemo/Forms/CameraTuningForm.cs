using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using HalconWinFormsDemo.Vision;

namespace HalconWinFormsDemo.Forms
{
    /// <summary>
    /// Non-modal tuning window. Intended for use while preview is running.
    /// No explicit Save button: parameters are applied live and persisted when the window closes.
    /// </summary>
    public sealed class CameraTuningForm : Form
    {
        // Primary HALCON/GenICam easyparam keys
        private const string KEY_EXPOSURE_AUTO = "Consumer|exposure_auto";
        private const string KEY_EXPOSURE = "Consumer|exposure";
        private const string KEY_GAIN_AUTO = "Consumer|gain_auto";
        private const string KEY_GAIN = "Consumer|gain";

        // Additional common GenICam nodes (often available in MVS)
        private const string KEY_GAMMA_ENABLE = "GammaEnable";
        private const string KEY_GAMMA = "Gamma";

        private readonly string camName;
        private readonly CameraConfig camConfig;
        private readonly ICamera camera;

        private readonly CheckBox chkExpAuto = new CheckBox { Text = "自动曝光", AutoSize = true };
        private readonly NumericUpDown nudExposure = new NumericUpDown { Minimum = 1, Maximum = 200000, Increment = 100, DecimalPlaces = 0, Width = 120 };
        private readonly TrackBar tbExposure = new TrackBar { Minimum = 1, Maximum = 200000, TickFrequency = 10000, SmallChange = 100, LargeChange = 1000, Dock = DockStyle.Fill };

        private readonly CheckBox chkGainAuto = new CheckBox { Text = "自动增益", AutoSize = true };
        private readonly NumericUpDown nudGain = new NumericUpDown { Minimum = 0, Maximum = 30, Increment = 0.1M, DecimalPlaces = 1, Width = 120 };
        private readonly TrackBar tbGain = new TrackBar { Minimum = 0, Maximum = 300, TickFrequency = 50, SmallChange = 1, LargeChange = 10, Dock = DockStyle.Fill };

        private readonly CheckBox chkGammaEnable = new CheckBox { Text = "启用伽马", AutoSize = true };
        private readonly NumericUpDown nudGamma = new NumericUpDown { Minimum = 0.10M, Maximum = 4.00M, Increment = 0.01M, DecimalPlaces = 2, Width = 120 };
        private readonly TrackBar tbGamma = new TrackBar { Minimum = 10, Maximum = 400, TickFrequency = 50, SmallChange = 1, LargeChange = 10, Dock = DockStyle.Fill };

        private readonly Label lblHint = new Label { AutoSize = true, ForeColor = Color.DimGray };

        // UI input -> throttled apply (like MVS)
        private readonly Timer applyTimer = new Timer();
        private volatile bool pendingApply;

        private readonly object applyChainLock = new object();
        private Task applyChain = Task.CompletedTask;

        private DesiredParams latestDesired;

        private struct DesiredParams
        {
            public bool ExpAuto;
            public int Exposure;
            public bool GainAuto;
            public double Gain;
            public bool GammaEnable;
            public double Gamma;
        }

        public CameraTuningForm(string camName, CameraConfig camConfig, ICamera camera)
        {
            this.camName = camName;
            this.camConfig = camConfig;
            this.camera = camera;

            Text = $"{camName} 参数";
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            TopMost = false;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(560, 270);

            BuildLayout();
            WireEvents();
            LoadFromConfigAndDevice();

            // Throttle: merge rapid slider events into one apply (80-120ms feels responsive)
            applyTimer.Interval = 120;
            applyTimer.Tick += (_, __) =>
            {
                if (!pendingApply) return;
                pendingApply = false;
                applyTimer.Stop(); // one-shot style
                EnqueueApply(latestDesired);
            };
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(10),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // exposure
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // gain
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // gamma
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // hint

            // Exposure row
            var pnlExpFlags = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pnlExpFlags.Controls.Add(chkExpAuto);
            table.Controls.Add(new Label { Text = "曝光", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(tbExposure, 1, 0);
            var pnlExpRight = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pnlExpRight.Controls.Add(nudExposure);
            pnlExpRight.Controls.Add(pnlExpFlags);
            table.Controls.Add(pnlExpRight, 2, 0);

            // Gain row
            var pnlGainFlags = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pnlGainFlags.Controls.Add(chkGainAuto);
            table.Controls.Add(new Label { Text = "增益", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            table.Controls.Add(tbGain, 1, 1);
            var pnlGainRight = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pnlGainRight.Controls.Add(nudGain);
            pnlGainRight.Controls.Add(pnlGainFlags);
            table.Controls.Add(pnlGainRight, 2, 1);

            // Gamma row
            table.Controls.Add(new Label { Text = "伽马", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            table.Controls.Add(tbGamma, 1, 2);
            var pnlGammaRight = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            pnlGammaRight.Controls.Add(nudGamma);
            pnlGammaRight.Controls.Add(chkGammaEnable);
            table.Controls.Add(pnlGammaRight, 2, 2);

            lblHint.Text = "提示：拖动滑条时将自动“节流合并下发”，减少卡顿；松开鼠标会立即应用。关闭窗口自动保存。";
            table.Controls.Add(lblHint, 0, 3);
            table.SetColumnSpan(lblHint, 3);

            Controls.Add(table);
        }

        private void WireEvents()
        {
            chkExpAuto.CheckedChanged += (_, __) =>
            {
                UpdateUiEnabled();
                MarkPendingApply();
            };

            chkGainAuto.CheckedChanged += (_, __) =>
            {
                UpdateUiEnabled();
                MarkPendingApply();
            };

            tbExposure.Scroll += (_, __) =>
            {
                if (chkExpAuto.Checked) chkExpAuto.Checked = false;
                if (nudExposure.Value != tbExposure.Value)
                    nudExposure.Value = tbExposure.Value;
                MarkPendingApply();
            };
            nudExposure.ValueChanged += (_, __) =>
            {
                if (chkExpAuto.Checked) chkExpAuto.Checked = false;
                var v = (int)Math.Max(tbExposure.Minimum, Math.Min(tbExposure.Maximum, (int)nudExposure.Value));
                if (tbExposure.Value != v) tbExposure.Value = v;
                MarkPendingApply();
            };

            tbGain.Scroll += (_, __) =>
            {
                if (chkGainAuto.Checked) chkGainAuto.Checked = false;
                var g = tbGain.Value / 10.0M;
                if (nudGain.Value != g) nudGain.Value = g;
                MarkPendingApply();
            };
            nudGain.ValueChanged += (_, __) =>
            {
                if (chkGainAuto.Checked) chkGainAuto.Checked = false;
                var v = (int)Math.Max(tbGain.Minimum, Math.Min(tbGain.Maximum, (int)(nudGain.Value * 10)));
                if (tbGain.Value != v) tbGain.Value = v;
                MarkPendingApply();
            };

            chkGammaEnable.CheckedChanged += (_, __) =>
            {
                UpdateUiEnabled();
                MarkPendingApply();
            };

            tbGamma.Scroll += (_, __) =>
            {
                if (!chkGammaEnable.Checked) chkGammaEnable.Checked = true;
                var g = tbGamma.Value / 100.0M;
                if (nudGamma.Value != g) nudGamma.Value = g;
                MarkPendingApply();
            };
            nudGamma.ValueChanged += (_, __) =>
            {
                if (!chkGammaEnable.Checked) chkGammaEnable.Checked = true;
                var v = (int)Math.Max(tbGamma.Minimum, Math.Min(tbGamma.Maximum, (int)(nudGamma.Value * 100)));
                if (tbGamma.Value != v) tbGamma.Value = v;
                MarkPendingApply();
            };

            // "MouseUp = apply immediately" to mimic MVS feel.
            tbExposure.MouseUp += (_, __) => ApplyImmediately();
            tbGain.MouseUp += (_, __) => ApplyImmediately();
            tbGamma.MouseUp += (_, __) => ApplyImmediately();

            FormClosed += (_, __) =>
            {
                applyTimer.Stop();
                applyTimer.Dispose();
                PersistToConfig();
            };
        }

        private void UpdateUiEnabled()
        {
            tbExposure.Enabled = nudExposure.Enabled = !chkExpAuto.Checked;
            tbGain.Enabled = nudGain.Enabled = !chkGainAuto.Checked;
            tbGamma.Enabled = nudGamma.Enabled = chkGammaEnable.Checked;
        }

        private void MarkPendingApply()
        {
            latestDesired = SnapshotDesired();
            pendingApply = true;
            applyTimer.Stop();
            applyTimer.Start();
        }

        private void ApplyImmediately()
        {
            latestDesired = SnapshotDesired();
            pendingApply = false;
            applyTimer.Stop();
            EnqueueApply(latestDesired);
        }

        private DesiredParams SnapshotDesired()
        {
            return new DesiredParams
            {
                ExpAuto = chkExpAuto.Checked,
                Exposure = (int)nudExposure.Value,
                GainAuto = chkGainAuto.Checked,
                Gain = (double)nudGain.Value,
                GammaEnable = chkGammaEnable.Checked,
                Gamma = (double)nudGamma.Value
            };
        }

        private void LoadFromConfigAndDevice()
        {
            var dict = camConfig.FramegrabberParams ?? new Dictionary<string, string>();

            chkExpAuto.Checked = ReadBool(dict, KEY_EXPOSURE_AUTO, false);
            nudExposure.Value = ReadInt(dict, KEY_EXPOSURE, 8000);

            chkGainAuto.Checked = ReadBool(dict, KEY_GAIN_AUTO, false);
            nudGain.Value = (decimal)ReadDouble(dict, KEY_GAIN, 1.0);

            chkGammaEnable.Checked = ReadBool(dict, KEY_GAMMA_ENABLE, true);
            nudGamma.Value = ClampDecimal(ReadDouble(dict, KEY_GAMMA, 1.0), nudGamma.Minimum, nudGamma.Maximum);

            // Best-effort sync from device (non-fatal)
            var access = camera as IFramegrabberParamAccess;
            if (access != null)
            {
                string err;
                HalconDotNet.HTuple v;
                if (access.TryGetParam(KEY_EXPOSURE_AUTO, out v, out err))
                    chkExpAuto.Checked = TryParseBoolFromTuple(v, chkExpAuto.Checked);
                if (access.TryGetParam(KEY_EXPOSURE, out v, out err))
                    nudExposure.Value = ClampDecimal(TryGetDoubleFromTuple(v, (double)nudExposure.Value), nudExposure.Minimum, nudExposure.Maximum);
                if (access.TryGetParam(KEY_GAIN_AUTO, out v, out err))
                    chkGainAuto.Checked = TryParseBoolFromTuple(v, chkGainAuto.Checked);
                if (access.TryGetParam(KEY_GAIN, out v, out err))
                    nudGain.Value = ClampDecimal(TryGetDoubleFromTuple(v, (double)nudGain.Value), nudGain.Minimum, nudGain.Maximum);

                if (access.TryGetParam(KEY_GAMMA_ENABLE, out v, out err))
                    chkGammaEnable.Checked = TryParseBoolFromTuple(v, chkGammaEnable.Checked);
                if (access.TryGetParam(KEY_GAMMA, out v, out err))
                    nudGamma.Value = ClampDecimal(TryGetDoubleFromTuple(v, (double)nudGamma.Value), nudGamma.Minimum, nudGamma.Maximum);
            }

            tbExposure.Value = (int)nudExposure.Value;
            tbGain.Value = (int)(nudGain.Value * 10);
            tbGamma.Value = (int)(nudGamma.Value * 100);
            UpdateUiEnabled();

            // Apply once to ensure device matches UI (background, throttled)
            MarkPendingApply();
        }

        private void EnqueueApply(DesiredParams desired)
        {
            var access = camera as IFramegrabberParamAccess;
            if (access == null) return;

            lock (applyChainLock)
            {
                applyChain = applyChain.ContinueWith(_ =>
                {
                    ApplyToDeviceBestEffort(access, desired);
                }, TaskScheduler.Default);
            }
        }

        private void ApplyToDeviceBestEffort(IFramegrabberParamAccess access, DesiredParams desired)
        {
            string err;
            var errors = new List<string>();

            // Auto flags
            if (!TrySetAuto(access, KEY_EXPOSURE_AUTO, desired.ExpAuto, out err))
            {
                // err already set
            }
            if (!string.IsNullOrWhiteSpace(err)) errors.Add($"曝光自动({KEY_EXPOSURE_AUTO}): " + err);

            if (!desired.ExpAuto)
            {
                var expInt = desired.Exposure;
                if (!access.TrySetParam(KEY_EXPOSURE, expInt, out err))
                {
                    access.TrySetParam(KEY_EXPOSURE, (double)expInt, out err);
                }
                if (!string.IsNullOrWhiteSpace(err))
                    errors.Add($"曝光({KEY_EXPOSURE}): " + err);
            }

            if (!TrySetAuto(access, KEY_GAIN_AUTO, desired.GainAuto, out err))
            {
                // err already set
            }
            if (!string.IsNullOrWhiteSpace(err)) errors.Add($"增益自动({KEY_GAIN_AUTO}): " + err);

            if (!desired.GainAuto)
            {
                var gainDouble = desired.Gain;
                if (!access.TrySetParam(KEY_GAIN, gainDouble, out err))
                {
                    access.TrySetParam(KEY_GAIN, (int)Math.Round(gainDouble), out err);
                }
                if (!string.IsNullOrWhiteSpace(err))
                    errors.Add($"增益({KEY_GAIN}): " + err);
            }

            // Gamma
            if (!access.TrySetParam(KEY_GAMMA_ENABLE, desired.GammaEnable, out err))
            {
                access.TrySetParam(KEY_GAMMA_ENABLE, desired.GammaEnable ? 1 : 0, out err);
            }
            if (!string.IsNullOrWhiteSpace(err))
                errors.Add($"伽马使能({KEY_GAMMA_ENABLE}): " + err);

            if (desired.GammaEnable)
            {
                var g = desired.Gamma;
                if (!access.TrySetParam(KEY_GAMMA, g, out err))
                {
                    access.TrySetParam(KEY_GAMMA, g.ToString(CultureInfo.InvariantCulture), out err);
                }
                if (!string.IsNullOrWhiteSpace(err))
                    errors.Add($"伽马({KEY_GAMMA}): " + err);
            }

            // UI feedback (marshal to UI thread)
            try
            {
                if (IsDisposed) return;
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    if (errors.Count > 0)
                    {
                        lblHint.ForeColor = Color.Firebrick;
                        lblHint.Text = "应用失败: " + string.Join("; ", errors);
                    }
                    else
                    {
                        lblHint.ForeColor = Color.DimGray;
                        lblHint.Text = "提示：拖动滑条时将自动“节流合并下发”，减少卡顿；松开鼠标会立即应用。关闭窗口自动保存。";
                    }
                }));
            }
            catch
            {
                // ignore
            }

            // Optional: force one fresh frame for quick observation.
            try { camera.SoftwareTrigger(); } catch { }
        }

        private void PersistToConfig()
        {
            if (Infrastructure.AppRuntimeState.ProductionLocked)
            {
                MessageBox.Show("生产模式锁定：参数已实时应用，但未写入配置文件。\n\n请切换到 MOCK 后再保存参数。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (camConfig.FramegrabberParams == null)
                camConfig.FramegrabberParams = new Dictionary<string, string>();

            var dict = camConfig.FramegrabberParams;
            dict[KEY_EXPOSURE_AUTO] = chkExpAuto.Checked ? "On" : "Off";
            dict[KEY_EXPOSURE] = ((int)nudExposure.Value).ToString(CultureInfo.InvariantCulture);
            dict[KEY_GAIN_AUTO] = chkGainAuto.Checked ? "On" : "Off";
            dict[KEY_GAIN] = ((double)nudGain.Value).ToString(CultureInfo.InvariantCulture);

            dict[KEY_GAMMA_ENABLE] = chkGammaEnable.Checked ? "On" : "Off";
            dict[KEY_GAMMA] = ((double)nudGamma.Value).ToString(CultureInfo.InvariantCulture);

            var settings = CameraSettingsStore.Load();
            var target = GetConfigByName(settings, camName);
            if (target != null)
            {
                target.FramegrabberParams = dict;
                CameraSettingsStore.Save(settings);
            }
        }

        private static bool TrySetAuto(IFramegrabberParamAccess access, string key, bool enabled, out string error)
        {
            var candidates = enabled
                ? new object[] { "On", "on", "True", true, 1, "1", "Enable", "enable", "Continuous" }
                : new object[] { "Off", "off", "False", false, 0, "0", "Disable", "disable" };

            error = string.Empty;
            foreach (var v in candidates)
            {
                if (access.TrySetParam(key, v, out error))
                {
                    error = string.Empty;
                    return true;
                }
            }
            return false;
        }

        private static CameraConfig GetConfigByName(CameraSettings settings, string name)
        {
            if (settings == null) return null;
            if (string.Equals(name, "Cam1", StringComparison.OrdinalIgnoreCase)) return settings.Cam1;
            if (string.Equals(name, "Cam2", StringComparison.OrdinalIgnoreCase)) return settings.Cam2;
            if (string.Equals(name, "Cam3", StringComparison.OrdinalIgnoreCase)) return settings.Cam3;
            if (string.Equals(name, "Cam4", StringComparison.OrdinalIgnoreCase)) return settings.Cam4;
            if (string.Equals(name, "Cam5", StringComparison.OrdinalIgnoreCase)) return settings.Cam5;
            if (string.Equals(name, "Cam6", StringComparison.OrdinalIgnoreCase)) return settings.Cam6;
            return null;
        }

        private static bool ReadBool(Dictionary<string, string> dict, string key, bool defaultValue)
        {
            if (dict != null && dict.TryGetValue(key, out var s))
                return string.Equals(s, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "True", StringComparison.OrdinalIgnoreCase) || s == "1";
            return defaultValue;
        }

        private static int ReadInt(Dictionary<string, string> dict, string key, int defaultValue)
        {
            if (dict != null && dict.TryGetValue(key, out var s))
            {
                int v;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    return v;
            }
            return defaultValue;
        }

        private static double ReadDouble(Dictionary<string, string> dict, string key, double defaultValue)
        {
            if (dict != null && dict.TryGetValue(key, out var s))
            {
                double v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return v;
            }
            return defaultValue;
        }

        private static bool TryParseBoolFromTuple(HalconDotNet.HTuple t, bool fallback)
        {
            try
            {
                if (t == null || t.Length <= 0) return fallback;
            }
            catch
            {
                return fallback;
            }

            try { return t.I != 0; } catch { }

            try
            {
                var s = t.S;
                if (string.IsNullOrWhiteSpace(s)) return fallback;
                return string.Equals(s, "On", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "True", StringComparison.OrdinalIgnoreCase)
                    || s == "1"
                    || string.Equals(s, "Enable", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "Continuous", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return fallback;
            }
        }

        private static double TryGetDoubleFromTuple(HalconDotNet.HTuple t, double fallback)
        {
            try
            {
                if (t == null || t.Length <= 0) return fallback;
            }
            catch
            {
                return fallback;
            }

            try { return t.D; } catch { }
            try { return t.I; } catch { }

            try
            {
                var s = t.S;
                if (string.IsNullOrWhiteSpace(s)) return fallback;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    return v;
            }
            catch { }

            return fallback;
        }

        private static decimal ClampDecimal(double v, decimal min, decimal max)
        {
            var d = (decimal)v;
            if (d < min) return min;
            if (d > max) return max;
            return d;
        }
    }
}
