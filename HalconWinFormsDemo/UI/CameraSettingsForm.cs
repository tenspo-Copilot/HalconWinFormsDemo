using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using HalconWinFormsDemo.Vision;

namespace HalconWinFormsDemo.UI
{
    public partial class CameraSettingsForm : Form
    {
        private CameraSettings settings;
        private CameraSettings original;
        private bool dirty;
        
        private bool _mappingUiUpdating;
private readonly Dictionary<string, CameraConfig> map = new();

        private TextBox txtDisplayName;
        private Label lblDisplayName;

        public CameraSettings ResultSettings => settings;

        public CameraSettingsForm()
        {
            InitializeComponent();
            InitUi();
        }

        private void InitUi()
        {
            // Make sure action buttons are always visible even under high DPI / font scaling.
            EnsureBottomActionBar();
            EnsureChineseUiFonts();
            EnsureDisplayNameControls();

            // Load persisted settings first (so reopening dialog shows last saved values)
            settings = CameraSettingsStore.Load();
            original = DeepClone(settings);
            dirty = false;

            // Button texts / UX
            btnOk.Text = "保存";
            btnCancel.Text = "取消";
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.FormClosing += CameraSettingsForm_FormClosing;

            comboInterface.Items.Clear();
            comboInterface.Items.AddRange(new object[]
            {
                CameraInterfaceType.GigEVision2,
                CameraInterfaceType.USB3Vision,
                CameraInterfaceType.DirectShow
            });


            map["Cam1"] = settings.Cam1;
            map["Cam2"] = settings.Cam2;
            map["Cam3"] = settings.Cam3;
            map["Cam4"] = settings.Cam4;
            map["Cam5"] = settings.Cam5;
            map["Cam6"] = settings.Cam6;

            comboCamera.Items.Clear();
            comboCamera.Items.AddRange(map.Keys.Cast<object>().ToArray());
            comboCamera.SelectedIndex = 0;

            LoadSelectedToUi();
            PopulateMappingCombos(new List<string>());

            btnEnumDevices.Click += (_, __) => EnumerateDevices();
            btnScanInterfaces.Click += (_, __) => ScanInterfaces();
            btnAggregateEnum.Click += (_, __) => AggregateEnumerate();
            btnTestOpen.Click += (_, __) => TestOpen();
            btnOk.Click += (_, __) => OnOk();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            comboCamera.SelectedIndexChanged += (_, __) => LoadSelectedToUi();

            // Dirty tracking
            comboInterface.SelectedIndexChanged += (_, __) => MarkDirty();
            txtDevice.TextChanged += (_, __) => MarkDirty();
            if (txtDisplayName != null) txtDisplayName.TextChanged += (_, __) => MarkDirty();
            cmbMap1.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap1);
            cmbMap2.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap2);
            cmbMap3.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap3);
            cmbMap4.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap4);
            cmbMap5.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap5);
            cmbMap6.SelectedIndexChanged += (_, __) => OnMappingComboChanged(cmbMap6);
            PopulateMappingCombos(new List<string>());
        }

        /// <summary>
        /// WinForms 在高 DPI 或字体缩放时，底部按钮可能被推到可视区域外。
        /// 这里将【保存/取消】放入 Dock=Bottom 的固定操作栏，并开启滚动，保证任何分辨率下可见。
        /// </summary>
        private void EnsureBottomActionBar()
        {
            // Avoid duplicated injection
            if (Controls.OfType<Panel>().Any(p => string.Equals(p.Name, "pnlBottomActions", StringComparison.Ordinal)))
                return;

            // Enable scrolling for the rest of the content
            AutoScroll = true;

            var pnl = new Panel
            {
                Name = "pnlBottomActions",
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(10),
            };

            // Move existing buttons into the bottom panel
            Controls.Remove(btnOk);
            Controls.Remove(btnCancel);

            btnOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnCancel.Top = 10;
            btnOk.Top = 10;

            // Set initial positions; Anchor will keep them on the right
            void Reflow()
            {
                btnCancel.Left = pnl.ClientSize.Width - btnCancel.Width - 10;
                btnOk.Left = btnCancel.Left - btnOk.Width - 10;
            }

            pnl.Resize += (_, __) => Reflow();

            // Initial layout pass
            Reflow();

            pnl.Controls.Add(btnOk);
            pnl.Controls.Add(btnCancel);

            Controls.Add(pnl);
            pnl.BringToFront();

            // One more layout pass after adding to the form
            Reflow();

            // Ensure form has a reasonable minimum size so controls don't overlap excessively
            if (MinimumSize.Width < 900 || MinimumSize.Height < 700)
            {
                MinimumSize = new System.Drawing.Size(900, 700);
            }
        }

        private CameraConfig CurrentConfig()
        {
            var key = comboCamera.SelectedItem?.ToString() ?? "Cam1";
            return map[key];
        }

        private void LoadSelectedToUi()
        {
            var cfg = CurrentConfig();

            comboInterface.SelectedItem = cfg.InterfaceType;
            txtDevice.Text = cfg.Device;
            if (txtDisplayName != null) txtDisplayName.Text = cfg.DisplayName;
            txtPort.Text = cfg.Port.ToString();
            listDevices.Items.Clear();
        }

        private bool SaveUiToSelected(out string error)
        {
            error = "";
            var cfg = CurrentConfig();

            if (comboInterface.SelectedItem is not CameraInterfaceType it)
            {
                error = "请选择相机接口类型。";
                return false;
            }

            cfg.InterfaceType = it;
            cfg.Device = txtDevice.Text.Trim();
            cfg.DisplayName = (txtDisplayName != null ? txtDisplayName.Text : "").Trim();

            if (!int.TryParse(txtPort.Text.Trim(), out var port) || port < 0)
            {
                error = "Port 必须为非负整数。";
                return false;
            }

            cfg.Port = port;
            return true;
        }

        private void ScanInterfaces()
        {
            txtInterfaceScan.Clear();

            var results = HalconCameraHelper.ScanInterfaces();
            foreach (var r in results)
            {
                txtInterfaceScan.AppendText($"{r.InterfaceName}: {(r.Available ? "OK" : "缺失")}{Environment.NewLine}");
            }
        }

        private List<string> lastEnumeratedDevices = new List<string>();

        private void PopulateMappingCombos(List<string> devices)
        {
            lastEnumeratedDevices = devices ?? new List<string>();

            // IMPORTANT:
            // Mapping dropdowns must be able to display the last saved mappings even when
            // the user has not enumerated devices in the current session (or enumeration fails).
            // Otherwise reopening the dialog will show blanks and users will assume saving failed.
            var savedKeys = new List<string>
            {
                BuildKey(settings.Cam1),
                BuildKey(settings.Cam2),
                BuildKey(settings.Cam3),
                BuildKey(settings.Cam4),
                BuildKey(settings.Cam5),
                BuildKey(settings.Cam6),
            }
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            var items = new List<string>();
            items.Add("");
            // Put saved mappings first for visibility.
            items.AddRange(savedKeys);
            // Then add enumerated devices not already included.
            foreach (var d in lastEnumeratedDevices)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                if (items.Contains(d, StringComparer.OrdinalIgnoreCase)) continue;
                items.Add(d);
            }

            void Bind(System.Windows.Forms.ComboBox cb)
            {
                cb.BeginUpdate();
                cb.Items.Clear();
                foreach (var it in items)
                    cb.Items.Add(it);
                cb.EndUpdate();
            }

            Bind(cmbMap1);
            Bind(cmbMap2);
            Bind(cmbMap3);
            Bind(cmbMap4);
            Bind(cmbMap5);
            Bind(cmbMap6);

            // load saved mapping into dropdowns
            cmbMap1.SelectedItem = BuildKey(settings.Cam1);
            cmbMap2.SelectedItem = BuildKey(settings.Cam2);
            cmbMap3.SelectedItem = BuildKey(settings.Cam3);
            cmbMap4.SelectedItem = BuildKey(settings.Cam4);
            cmbMap5.SelectedItem = BuildKey(settings.Cam5);
            cmbMap6.SelectedItem = BuildKey(settings.Cam6);
        }

        private static string BuildKey(CameraConfig cfg)
        {
            var ifName = HalconCameraHelper.ToHalconInterfaceName(cfg.InterfaceType);
            var dev = (cfg.Device ?? "").Trim();
            return string.IsNullOrWhiteSpace(dev) || dev.Equals("default", StringComparison.OrdinalIgnoreCase)
                ? ""
                : $"{ifName}::{dev}";
        }

        private static bool TryParseKey(string key, out string ifName, out string device)
        {
            ifName = "";
            device = "";
            if (string.IsNullOrWhiteSpace(key)) return false;
            var s = key.Trim();
            var idx = s.IndexOf("::", StringComparison.Ordinal);
            if (idx <= 0 || idx >= s.Length - 2) return false;
            ifName = s.Substring(0, idx);
            device = s.Substring(idx + 2);
            return !string.IsNullOrWhiteSpace(ifName) && !string.IsNullOrWhiteSpace(device);
        }

        private void btnAutoAssign_Click(object sender, EventArgs e)
        {
            if (lastEnumeratedDevices == null || lastEnumeratedDevices.Count == 0)
            {
                MessageBox.Show("请先点击【枚举设备】获取设备编码列表。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 按枚举顺序自动分配到画面1~6（不足则保留空）
            cmbMap1.SelectedItem = lastEnumeratedDevices.Count > 0 ? lastEnumeratedDevices[0] : "";
            cmbMap2.SelectedItem = lastEnumeratedDevices.Count > 1 ? lastEnumeratedDevices[1] : "";
            cmbMap3.SelectedItem = lastEnumeratedDevices.Count > 2 ? lastEnumeratedDevices[2] : "";
            cmbMap4.SelectedItem = lastEnumeratedDevices.Count > 3 ? lastEnumeratedDevices[3] : "";
            cmbMap5.SelectedItem = lastEnumeratedDevices.Count > 4 ? lastEnumeratedDevices[4] : "";
            cmbMap6.SelectedItem = lastEnumeratedDevices.Count > 5 ? lastEnumeratedDevices[5] : "";
        }

        private void EnumerateDevices()
        {
            if (!SaveUiToSelected(out var err))
            {
                MessageBox.Show(err, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cfg = CurrentConfig();
            var devicesRaw = HalconCameraHelper.TryListDevices(cfg.InterfaceType, out var error);

            listDevices.Items.Clear();
            if (!string.IsNullOrWhiteSpace(error))
            {
                error = HalconCameraHelper.AppendDiagnosticHints(error, cfg.InterfaceType);
                MessageBox.Show($"无法枚举设备：{error}\n\n提示：请确认已安装对应 HALCON 接口驱动（GigEVision2/USB3Vision）。",
                    "枚举失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (devicesRaw.Count == 0)
            {
                MessageBox.Show("未枚举到设备。\n\n提示：请确认相机已连接、网口同网段、相机未被其它软件占用。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var parsed = devicesRaw.Select(ParseDeviceDescriptor).ToList();

            foreach (var e in parsed)
                listDevices.Items.Add(new DeviceListItem(e.Display, e.Token));

            // For mapping combos we store [接口::deviceToken] to satisfy "保存接口+device" requirement.
            var ifName = HalconCameraHelper.ToHalconInterfaceName(cfg.InterfaceType);
            var keys = parsed.Select(e => $"{ifName}::{e.Token}").ToList();
            PopulateMappingCombos(keys);

            // allow double-click to fill device token
            listDevices.DoubleClick += (_, __) =>
            {
                if (listDevices.SelectedItem is DeviceListItem item)
                {
                    txtDevice.Text = item.Token;
                }
            };
        }

        private void TestOpen()
        {
            if (!SaveUiToSelected(out var err))
            {
                MessageBox.Show(err, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cfg = CurrentConfig();
            var ok = HalconCameraHelper.TryTestOpen(cfg, out var error);
            error = HalconCameraHelper.AppendDiagnosticHints(error, cfg.InterfaceType);

            MessageBox.Show(
                ok ? (string.IsNullOrWhiteSpace(error) ? "打开并抓图测试成功。" : $"打开并抓图测试成功。\n\n{error}") : $"测试失败：{error}",
                "相机测试",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        private void btnScanInterfaces_Click(object sender, EventArgs e)
        {
            ScanInterfaces();
        }
        private void OnMappingComboChanged(ComboBox source)
        {
            if (_mappingUiUpdating) return;

            try
            {
                _mappingUiUpdating = true;

                var key = source?.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(key))
                {
                    MarkDirty();
                    return;
                }

                // 方案B：禁止同一物理相机（接口::device）被映射到多个画面（Cam1..Cam6）
                var combos = new[] { cmbMap1, cmbMap2, cmbMap3, cmbMap4, cmbMap5, cmbMap6 };
                foreach (var cb in combos)
                {
                    if (cb == null || cb == source) continue;
                    var other = cb.SelectedItem?.ToString() ?? "";
                    if (string.Equals(other, key, StringComparison.OrdinalIgnoreCase))
                    {
                        cb.SelectedItem = "";
                    }
                }

                MarkDirty();
            }
            finally
            {
                _mappingUiUpdating = false;
            }
        }



        private void ApplyMappingFromUi()
        {
            // 映射区保存 [接口+device]：画面1~6 对应 Cam1~Cam6
            ApplyOne(cmbMap1, settings.Cam1);
            ApplyOne(cmbMap2, settings.Cam2);
            ApplyOne(cmbMap3, settings.Cam3);
            ApplyOne(cmbMap4, settings.Cam4);
            ApplyOne(cmbMap5, settings.Cam5);
            ApplyOne(cmbMap6, settings.Cam6);
        }

        private void ApplyOne(ComboBox cb, CameraConfig cfg)
        {
            var key = cb.SelectedItem?.ToString() ?? "";
            // IMPORTANT (stability + scheme B correctness):
            // When a mapping combo is cleared (""), we MUST clear the corresponding camera config.
            // Otherwise the previous device token remains in camera_settings.json, resulting in:
            // 1) User-perceived "mapping didn't change" (old mapping still effective)
            // 2) Duplicate device opens (same physical camera gets opened by multiple CamX), causing HALCON #5312
            if (string.IsNullOrWhiteSpace(key))
            {
                cfg.Device = "default";
                return;
            }

            if (TryParseKey(key, out var ifName, out var dev))
            {
                cfg.Device = dev;
                cfg.InterfaceType = HalconCameraHelper.FromHalconInterfaceName(ifName);
                return;
            }

            // Backward compatibility: if user picked a plain device string
            cfg.Device = key.Trim();
        }

        private void AggregateEnumerate()
        {
            // Cross-interface aggregate enumerate: GigE + USB3 + DirectShow
            var devices = new List<string>();

            void Add(CameraInterfaceType it)
            {
                var raw = HalconCameraHelper.TryListDevices(it, out var err);
                if (!string.IsNullOrWhiteSpace(err)) return;
                var ifName = HalconCameraHelper.ToHalconInterfaceName(it);
                foreach (var d in raw)
                {
                    var e = ParseDeviceDescriptor(d);
                    devices.Add($"{ifName}::{e.Token}");
                }
            }

            Add(CameraInterfaceType.GigEVision2);
            Add(CameraInterfaceType.USB3Vision);
            Add(CameraInterfaceType.DirectShow);

            devices = devices.Distinct().ToList();
            devices.Sort(StringComparer.OrdinalIgnoreCase);

            if (devices.Count == 0)
            {
                MessageBox.Show("未枚举到任何设备。\n\n提示：请确认相机已连接、网口同网段、驱动已安装、且相机未被其它软件占用。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PopulateMappingCombos(devices);
            MessageBox.Show($"跨接口聚合枚举完成：{devices.Count} 台设备。\n\n说明：映射保存为 [接口::device]。",
                "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAggregateEnum_Click(object sender, EventArgs e)
        {
            AggregateEnumerate();
        }

        private void OnOk()
        {
            if (!SaveUiToSelected(out var err))
            {
                MessageBox.Show(err, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplyMappingFromUi();
            CameraSettingsStore.Save(settings);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static CameraSettings DeepClone(CameraSettings s)
        {
            // Safe deep clone for edit/cancel scenarios
            var json = System.Text.Json.JsonSerializer.Serialize(s);
            return System.Text.Json.JsonSerializer.Deserialize<CameraSettings>(json) ?? new CameraSettings();
        }

        private bool HasUnsavedChanges()
        {
            // Compare current in-memory settings with the original snapshot
            var a = System.Text.Json.JsonSerializer.Serialize(settings);
            var b = System.Text.Json.JsonSerializer.Serialize(original);
            return !string.Equals(a, b, StringComparison.Ordinal);
        }

        private void MarkDirty() => dirty = true;

        private void CameraSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If user clicks X and there are unsaved changes, prompt
            if (this.DialogResult == DialogResult.OK) return; // already saved
            if (this.DialogResult == DialogResult.Cancel) { settings = DeepClone(original); return; }
            if (!dirty && !HasUnsavedChanges()) return;

            var r = MessageBox.Show(
                "检测到相机设置已修改，是否保存？\n\n选择“是”：保存并关闭；\n选择“否”：放弃修改并关闭；\n选择“取消”：返回继续编辑。",
                "相机设置",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (r == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (r == DialogResult.No)
            {
                // Revert in-memory to original so caller won't pick up dirty state
                settings = DeepClone(original);
                return;
            }

            // Yes -> save
            if (!SaveUiToSelected(out var err))
            {
                MessageBox.Show(err, "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            ApplyMappingFromUi();
            CameraSettingsStore.Save(settings);
            this.DialogResult = DialogResult.OK;
        }

        private void EnsureChineseUiFonts()
        {
            try
            {
                var f = new System.Drawing.Font("Microsoft YaHei UI", this.Font.Size);
                this.Font = f;
                // Best-effort on key controls
                comboCamera.Font = f;
                comboInterface.Font = f;
                txtDevice.Font = f;
                txtPort.Font = f;
                listDevices.Font = f;
                cmbMap1.Font = f; cmbMap2.Font = f; cmbMap3.Font = f; cmbMap4.Font = f; cmbMap5.Font = f; cmbMap6.Font = f;
            }
            catch
            {
                // ignore font issues
            }
        }

        private void EnsureDisplayNameControls()
        {
            if (txtDisplayName != null) return;

            // Insert a "Display Name" row right below the Device row, and push all controls below downwards
            // to avoid any overlap under different DPI/scaling settings.
            int insertTop = txtDevice.Bottom + 8;
            // Align the new label with the existing "Device" label if we can find it.
            // Do NOT rely on designer field names (they may differ across environments/versions).
            int labelLeft = 12;
            try
            {
                var deviceLabel = Controls
                    .OfType<Label>()
                    .FirstOrDefault(l => l != null &&
                                         l.Visible &&
                                         l.Top >= (txtDevice.Top - 6) &&
                                         l.Top <= (txtDevice.Top + 6) &&
                                         l.Right <= txtDevice.Left &&
                                         (string.Equals(l.Text?.Trim(), "Device:", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(l.Text?.Trim(), "Device：", StringComparison.OrdinalIgnoreCase) ||
                                          (l.Text?.IndexOf("Device", StringComparison.OrdinalIgnoreCase) >= 0)));
                if (deviceLabel != null) labelLeft = deviceLabel.Left;
            }
            catch
            {
                // Keep default labelLeft
            }
            int textLeft = txtDevice.Left;
            int textWidth = txtDevice.Width;

            lblDisplayName = new Label
            {
                AutoSize = true,
                Text = "显示名：",
                Left = labelLeft,
                Top = insertTop + 4
            };

            txtDisplayName = new TextBox
            {
                Name = "txtDisplayName",
                Left = textLeft,
                Top = insertTop,
                Width = textWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Calculate how much space this new row takes and push controls below it down.
            int rowHeight = txtDisplayName.Height;
            int delta = rowHeight + 8;

            foreach (Control c in Controls)
            {
                if (c == null) continue;
                // Do not move the rows above the insertion point.
                if (c == txtDevice || c == comboInterface || c == comboCamera) continue;

                // Push down all controls that start below (or at) the insert position
                if (c.Top >= insertTop)
                    c.Top += delta;
            }

            Controls.Add(lblDisplayName);
            Controls.Add(txtDisplayName);
            lblDisplayName.BringToFront();
            txtDisplayName.BringToFront();

            // Ensure the form can show the extra row even on small resolutions
            this.AutoScroll = true;
            this.MinimumSize = new System.Drawing.Size(760, 520);
        }

        private sealed class DeviceListItem
        {
            public DeviceListItem(string display, string token)
            {
                Display = display;
                Token = token;
            }
            public string Display { get; }
            public string Token { get; }
            public override string ToString() => Display;
        }

        private static (string Display, string Token) ParseDeviceDescriptor(string raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s)) return ("(空)", "");

            string unique = ExtractField(s, "unique_name");
            string user = ExtractField(s, "user_name");

            // Use unique_name as primary token when available (stable & ASCII friendly).
            string token = !string.IsNullOrWhiteSpace(unique) ? ("unique_name:" + unique) : s;

            // Display: prefer user_name if it looks readable (not full of '?'), otherwise show unique_name/value.
            string display;
            if (!string.IsNullOrWhiteSpace(user) && user.Count(ch => ch == '?') < 3)
                display = $"{user}  ({unique})".Trim();
            else if (!string.IsNullOrWhiteSpace(unique))
                display = unique;
            else
                display = s;

            return (display, token);
        }

        private static string ExtractField(string src, string field)
        {
            // matches: field:value until next '|'
            var m = System.Text.RegularExpressions.Regex.Match(src, field + @"\s*:\s*([^|]+)");
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

    }
}