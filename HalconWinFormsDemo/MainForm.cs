using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using HalconDotNet;
using HalconWinFormsDemo.Forms;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using HalconWinFormsDemo.Infrastructure;
using HalconWinFormsDemo.Vision;
using HalconWinFormsDemo.UI;
using HalconWinFormsDemo.Diagnostics;

namespace HalconWinFormsDemo
{
    public partial class MainForm : Form
    {
        private readonly VisionController vision = new VisionController();

        private readonly Dictionary<string, HWindowControl> cameraNameToWindow = new(StringComparer.OrdinalIgnoreCase);

        // 方案B：同一相机只能绑定一个画面（View）。用于映射自愈与按钮调参定位。
        private readonly Dictionary<int, string> viewToCamera = new Dictionary<int, string>();
        private readonly Dictionary<int, HWindowControl> viewIndexToWindow = new Dictionary<int, HWindowControl>();


        // UI display throttle to prevent UI stutter when cameras output high FPS.
        private readonly Dictionary<string, long> lastDrawTicks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly long MinDrawIntervalTicks = (long)(Stopwatch.Frequency / 30.0); // ~30 FPS per window

        // Per-camera non-modal tuning windows (one instance per camera)
        private readonly Dictionary<string, CameraTuningForm> tuningForms = new Dictionary<string, CameraTuningForm>(StringComparer.OrdinalIgnoreCase);

        // PLC services
        private PlcSettings plcSettings = new PlcSettings();
        private readonly ModbusPlcService plcA = new ModbusPlcService("PLC_A");
        private readonly ModbusPlcService plcB = new ModbusPlcService("PLC_B");
        private readonly HashSet<string> group1Offline = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> group2Offline = new(StringComparer.OrdinalIgnoreCase);

        // MOCK auto cycle
        private System.Windows.Forms.Timer mockCycleTimer = null;
        private int mockCycleMs = 1000;

        // Real camera naming
        private int realCamCount = 0;

        private RunMode currentMode = RunMode.Mock;

        
        private readonly List<ICamera> allCameras = new();
        private readonly Dictionary<string, bool> cameraOnlineUi = new(StringComparer.OrdinalIgnoreCase);

        // Overview UI
        private StatusStrip _statusStrip = null;
        private ToolStripStatusLabel _lblRunMode = null;
        private ToolStripStatusLabel _lblCamOnline = null;
        private ToolStripStatusLabel _lblPlc1 = null;
        private ToolStripStatusLabel _lblPlc2 = null;
        private ToolStripStatusLabel _lblAlarmSummary = null;
        private ToolStripStatusLabel _lblClock = null;
        private TextBox _txtUiLog = null;
                private Panel _logPanel = null;
        private ToolStripStatusLabel _lblLogToggle = null;
        private int _logPanelHeight = 140;
private System.Windows.Forms.Timer _clockTimer = null;
        private readonly object _uiLogLock = new();
        private int _uiLogLinesMax = 300;

        private readonly Dictionary<int, Label> _viewStatusLabels = new();

        // Per-view status badge (dot + FPS)
        private readonly Dictionary<int, StatusBadge> _viewBadges = new();
        private readonly Dictionary<int, long> _viewLastFrameTick = new();
        private readonly Dictionary<int, double> _viewFpsEma = new();
        private System.Windows.Forms.Timer _badgeTimer = null;


        // Centralized UI logger (level + throttling)
        private readonly ThrottledUiLogger _logger = new ThrottledUiLogger();

        public MainForm()
        {
            InitializeComponent();

            // Wire up runtime event handlers (Designer does not bind events in this layout-refactor build).
            WireUiEvents();

            // Populate top-bar defaults (run mode, interface list, etc.)
            InitTopBarDefaults();

            // Runtime UI enhancements (no Designer changes)
            this.Shown += (_, __) =>
            {
                InitViewStatusBadges();
                InitOverviewBar();

                // Bind logger sink to UI textbox (created in InitOverviewBar)
                _logger.BindUiSink(AppendUiLogLine);

                // Startup self-check (non-blocking if all OK; modal if issues found)
                RunStartupSelfCheckOnce();

                UpdateOverviewMode();
                UpdateOverviewCameraOnline();
                UpdateAlarmSummary();
            };

            // Window mapping Cam1..Cam6
            cameraNameToWindow["Cam1"] = hWindowControl1;
            cameraNameToWindow["Cam2"] = hWindowControl2;
            cameraNameToWindow["Cam3"] = hWindowControl3;
            cameraNameToWindow["Cam4"] = hWindowControl4;
            cameraNameToWindow["Cam5"] = hWindowControl5;
            cameraNameToWindow["Cam6"] = hWindowControl6;

            

            // View slots 1..6 -> windows (stable physical layout)
            viewIndexToWindow[1] = hWindowControl1;
            viewIndexToWindow[2] = hWindowControl2;
            viewIndexToWindow[3] = hWindowControl3;
            viewIndexToWindow[4] = hWindowControl4;
            viewIndexToWindow[5] = hWindowControl5;
            viewIndexToWindow[6] = hWindowControl6;

            // Load & apply persisted view mapping with self-heal (方案B：禁止同一相机映射到多个画面)
            LoadAndApplyViewMapping();
// Add per-view "参数" buttons (方案 1) as overlays (Designer-safe)
            SetupViewTuningButtons();

            // Vision events
            vision.ImageReady += OnImageReady;
            vision.CameraError += (n, msg) =>
            {
                _logger.Warn("CAM", $"[{n}] {msg}", throttleKey: $"cam_err:{n}:{msg}", minIntervalMs: 1500);
            };

            // PLC settings + bind alarm linkage
            ReloadPlcSettingsAndApply();
            WireCameraOfflineToPlcAlarm();

            // Default: Mock mode
            ApplyRunMode(RunMode.Mock);
        }

        /// <summary>
        /// Load persisted View->Camera mapping (view_mapping.json), self-heal duplicates (方案B：同一相机只能绑定一个画面),
        /// then rebuild cameraNameToWindow so each camera routes to exactly one HWindowControl.
        /// </summary>
        private void LoadAndApplyViewMapping()
        {
            ViewMappingSettings settings;
            try
            {
                settings = ViewMappingStore.Load() ?? new ViewMappingSettings();
            }
            catch
            {
                settings = new ViewMappingSettings();
            }

            // Build ordered mapping (view 1..6)
            var ordered = new Dictionary<int, string>
            {
                [1] = settings.View1,
                [2] = settings.View2,
                [3] = settings.View3,
                [4] = settings.View4,
                [5] = settings.View5,
                [6] = settings.View6,
            };

            // Self-heal: enforce uniqueness (keep smallest view index; clear others)
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changed = false;

            foreach (var k in ordered.Keys.OrderBy(x => x))
            {
                var cam = ordered[k];
                if (string.IsNullOrWhiteSpace(cam))
                {
                    ordered[k] = null;
                    continue;
                }

                if (seen.Contains(cam))
                {
                    ordered[k] = null;
                    changed = true;
                }
                else
                {
                    seen.Add(cam);
                }
            }

            // Apply to runtime dictionaries
            viewToCamera.Clear();
            foreach (var kv in ordered)
                viewToCamera[kv.Key] = kv.Value;

            // Rebuild camera->window routing (1 camera -> 1 window)
            cameraNameToWindow.Clear();
            foreach (var kv in ordered.OrderBy(kv => kv.Key))
            {
                var viewIndex = kv.Key;
                var camName = kv.Value;
                if (string.IsNullOrWhiteSpace(camName)) continue;

                if (viewIndexToWindow.TryGetValue(viewIndex, out var win) && win != null)
                {
                    cameraNameToWindow[camName] = win;
                }
            }

            // Persist healed mapping so next startup won't re-enter the crash loop
            if (changed)
            {
                try
                {
                    settings.View1 = ordered[1];
                    settings.View2 = ordered[2];
                    settings.View3 = ordered[3];
                    settings.View4 = ordered[4];
                    settings.View5 = ordered[5];
                    settings.View6 = ordered[6];
                    ViewMappingStore.Save(settings);
                }
                catch { }
            }

            RefreshAllViewBadgesFromMapping();
        }

        private void InitViewStatusBadges()
        {
            // Create per-view overlay badges (dot + FPS). Must be hosted as sibling of HWindowControl
            // to avoid being overdrawn by HALCON window refresh.
            for (int i = 1; i <= 6; i++)
            {
                if (!viewIndexToWindow.TryGetValue(i, out HalconDotNet.HWindowControl win) || win == null)
                    continue;

                // Ensure the window is hosted in a Panel so we can overlay controls safely.
                var host = win.Parent as System.Windows.Forms.Panel;
                if (host == null)
                {
                    host = new System.Windows.Forms.Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = System.Drawing.Color.Black,
                        Margin = new Padding(6)
                    };

                    var oldParent = win.Parent;
                    if (oldParent != null)
                    {
                        try { oldParent.Controls.Remove(win); } catch { }
                    }
                    win.Dock = DockStyle.Fill;
                    host.Controls.Add(win);

                    // Best-effort reinsert to grid
                    try
                    {
                        int idx = i - 1;
                        int row = idx / 3;
                        int col = idx % 3;
                        if (this.tableLayoutPanel != null)
                            this.tableLayoutPanel.Controls.Add(host, col, row);
                    }
                    catch { }
                }

                // Avoid duplicates
                if (_viewBadges.ContainsKey(i) && _viewBadges[i] != null && !_viewBadges[i].IsDisposed)
                    continue;

                var badge = new StatusBadge
                {
                    Name = $"badgeView{i}",
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };

                host.Controls.Add(badge);

                void Relayout()
                {
                    try
                    {
                        badge.Location = new System.Drawing.Point(6, 6);
                        badge.BringToFront();
                    }
                    catch { }
                }

                host.Resize += (_, __) => Relayout();
                Relayout();

                _viewBadges[i] = badge;
                _viewLastFrameTick[i] = 0;
                _viewFpsEma[i] = 0;
            }

            // Timer to turn badge red when frames stop.
            _badgeTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _badgeTimer.Tick += (_, __) => UpdateBadgesByTimeout();
            _badgeTimer.Start();

            RefreshAllViewBadgesFromMapping();
        }

        private void RefreshAllViewBadgesFromMapping()
        {
            // Set badge mapping state from viewToCamera.
            for (int i = 1; i <= 6; i++)
            {
                if (!_viewBadges.TryGetValue(i, out var badge) || badge == null || badge.IsDisposed)
                    continue;

                var mapped = IsViewMapped(i);
                badge.IsMapped = mapped;
                if (!mapped)
                {
                    badge.IsRunning = false;
                    badge.Fps = 0;
                    _viewLastFrameTick[i] = 0;
                    _viewFpsEma[i] = 0;
                }
            }
        }

        private bool IsViewMapped(int viewIndex)
        {
            return viewToCamera.TryGetValue(viewIndex, out var cam) && !string.IsNullOrWhiteSpace(cam);
        }

        private int TryGetViewIndexByCameraName(string camName)
        {
            if (string.IsNullOrWhiteSpace(camName)) return 0;
            foreach (var kv in viewToCamera)
            {
                if (string.Equals(kv.Value, camName, StringComparison.OrdinalIgnoreCase))
                    return kv.Key;
            }
            return 0;
        }

        private void MarkFrameForCamera(string camName)
        {
            int viewIndex = TryGetViewIndexByCameraName(camName);
            if (viewIndex <= 0) return;

            if (!_viewBadges.TryGetValue(viewIndex, out var badge) || badge == null || badge.IsDisposed)
                return;

            if (!badge.IsMapped)
                badge.IsMapped = true;

            var now = Stopwatch.GetTimestamp();
            var last = _viewLastFrameTick.TryGetValue(viewIndex, out var t) ? t : 0;
            _viewLastFrameTick[viewIndex] = now;

            // EMA FPS
            if (last > 0)
            {
                var dt = (now - last) / (double)Stopwatch.Frequency;
                if (dt > 0.000001 && dt < 5)
                {
                    var inst = 1.0 / dt;
                    var ema = _viewFpsEma.TryGetValue(viewIndex, out var f) ? f : 0;
                    const double alpha = 0.2; // smoothing
                    ema = (ema <= 0) ? inst : (alpha * inst + (1 - alpha) * ema);
                    _viewFpsEma[viewIndex] = ema;
                    badge.Fps = ema;
                }
            }

            badge.IsRunning = true;
        }

        private void UpdateBadgesByTimeout()
        {
            var now = Stopwatch.GetTimestamp();
            var timeoutTicks = (long)(Stopwatch.Frequency * 1.2); // 1.2s without frame => stop

            for (int i = 1; i <= 6; i++)
            {
                if (!_viewBadges.TryGetValue(i, out var badge) || badge == null || badge.IsDisposed)
                    continue;

                if (!IsViewMapped(i))
                {
                    badge.IsMapped = false;
                    badge.IsRunning = false;
                    badge.Fps = 0;
                    _viewLastFrameTick[i] = 0;
                    _viewFpsEma[i] = 0;
                    continue;
                }

                badge.IsMapped = true;

                var last = _viewLastFrameTick.TryGetValue(i, out var t) ? t : 0;
                var running = last > 0 && (now - last) <= timeoutTicks;
                badge.IsRunning = running;

                if (!running)
                {
                    // decay FPS to 0 when stopped
                    badge.Fps = 0;
                    _viewFpsEma[i] = 0;
                }
            }
        }

        private void ToggleLogPanel()
        {
            if (_logPanel == null || _lblLogToggle == null)
                return;

            bool show = !_logPanel.Visible;
            _logPanel.Visible = show;
            _lblLogToggle.Text = show ? "LOG ▾" : "LOG ▸";

            if (show && _txtUiLog != null)
            {
                _txtUiLog.SelectionStart = _txtUiLog.TextLength;
                _txtUiLog.ScrollToCaret();
            }
        }



        private void WireUiEvents()
        {
            // Top bar buttons
            try { btnApplyMode.Click += btnApplyMode_Click; } catch { }
            try { btnMockPreview.Click += btnMockPreview_Click; } catch { }
            try { btnTriggerAll.Click += btnTriggerAll_Click; } catch { }
            try { btnSimPlcA.Click += btnSimPlcA_Click; } catch { }
            try { btnSimPlcB.Click += btnSimPlcB_Click; } catch { }
            try { btnCameraSettings.Click += btnCameraSettings_Click; } catch { }
            try { btnPlcSettings.Click += btnPlcSettings_Click; } catch { }
            try { btnAddCamera.Click += btnAddCamera_Click; } catch { }

            // Mock helpers
            try { chkMockAutoCycle.CheckedChanged += chkMockAutoCycle_CheckedChanged; } catch { }
            try { txtMockCycleMs.TextChanged += txtMockCycleMs_TextChanged; } catch { }
        }

        private void InitTopBarDefaults()
        {
            // Run mode combo
            try
            {
                cmbRunMode.Items.Clear();
                cmbRunMode.Items.Add("MOCK");
                cmbRunMode.Items.Add("REAL");
                cmbRunMode.SelectedIndex = 0;
            }
            catch { }

            // Interface quick list (optional; device can be typed manually)
            try
            {
                comboBoxInterfaceType.Items.Clear();
                comboBoxInterfaceType.Items.Add("GigEVision2");
                comboBoxInterfaceType.Items.Add("USB3Vision");
                comboBoxInterfaceType.Items.Add("DirectShow");
                comboBoxInterfaceType.SelectedIndex = 0;
            }
            catch { }

            // Mock default cycle
            try
            {
                if (string.IsNullOrWhiteSpace(txtMockCycleMs.Text))
                    txtMockCycleMs.Text = "1000";
            }
            catch { }
        }

        
        private bool IsProductionMode() => currentMode == RunMode.Real;

        private void OpenCameraSettingsLocked()
        {
            if (IsProductionMode())
            {
                MessageBox.Show(
                    "当前处于 REAL（生产）模式，已锁定参数修改。\n\n请先切换到 MOCK 模式（或停止生产）后再打开相机设置。",
                    "生产模式锁定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // To ensure enumeration works reliably, release any open framegrabbers before opening the mapping dialog.
            StopMockAutoCycle();
            try { vision.Stop(); } catch { }
            try { vision.Clear(); } catch { }

            using (var dlg = new CameraSettingsForm())
            {
                var dr = dlg.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    // Reload camera settings and rebuild pipeline (MOCK can preview real cameras)
                    ApplyRunMode(currentMode);
                }
                else
                {
                    // Restore previous pipeline (MOCK can preview real cameras)
                    ApplyRunMode(currentMode);
                }
            }
        }

        private void OpenPlcSettingsLocked()
        {
            if (IsProductionMode())
            {
                MessageBox.Show(
                    "当前处于 REAL（生产）模式，已锁定参数修改。\n\n请先切换到 MOCK 模式（或停止生产）后再打开 PLC 设置。",
                    "生产模式锁定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new PlcSettingsForm())
            {
                dlg.ShowDialog();
            }
        }

        private static IEnumerable<Control> FindAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var cc in FindAllControls(c))
                    yield return cc;
            }
        }

        private void SetupViewTuningButtons()
        {
            // 方案1：每个画面右上角一个“参数”按钮（非模态弹窗调参）。
            // 优先复用 Designer 中的 viewPanel1..6（稳定、可缩放），若检测不到则回退到运行时包裹。
            for (int i = 1; i <= 6; i++)
            {
                // Per-view overlay button. The button will tune the camera currently mapped to this view.
                if (!viewIndexToWindow.TryGetValue(i, out HalconDotNet.HWindowControl win) || win == null)
                    continue;
var host = win.Parent as System.Windows.Forms.Panel;

                // If the HWindowControl is not already hosted in a Panel, wrap it into a Panel inside the tableLayoutPanel.
                if (host == null)
                {
                    host = new System.Windows.Forms.Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = System.Drawing.Color.Black,
                        Margin = new Padding(6)
                    };

                    // Move the window into the host panel
                    var oldParent = win.Parent;
                    if (oldParent != null)
                    {
                        try { oldParent.Controls.Remove(win); } catch { }
                    }
                    win.Dock = DockStyle.Fill;
                    host.Controls.Add(win);

                    // Put host into the grid (best-effort)
                    try
                    {
                        int idx = i - 1;
                        int row = idx / 3;
                        int col = idx % 3;
                        if (this.tableLayoutPanel != null)
                            this.tableLayoutPanel.Controls.Add(host, col, row);
                    }
                    catch { }
                }

                // Avoid duplicate buttons
                foreach (Control c in host.Controls)
                {
                    var b = c as GearButton;
                    if (b != null && b.Tag is int && (int)b.Tag == i)
                        goto NEXT_CAM;
                }

                // Overlay "gear" button aligned with StatusBadge style
                var btn = new GearButton
                {
                    Name = $"btnTune_View{i}",
                    Tag = i,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btn.Click += TuningButton_Click;

                host.Controls.Add(btn);

                void Relayout()
                {
                    try
                    {
                        btn.Location = new System.Drawing.Point(
                            Math.Max(0, host.ClientSize.Width - btn.Width - 6),
                            6
                        );
                        btn.BringToFront();
                    }
                    catch { }
                }

                host.Resize += (_, __) => Relayout();
                Relayout();

            NEXT_CAM:
                ;
            }
        }


        private void TuningButton_Click(object sender, EventArgs e)
        {
            var btn = sender as Control;
            string camName = null;

            // New: Tag stores view index (方案B). Backward compatible with older builds that store camera name.
            if (btn?.Tag is int viewIndex)
            {
                viewToCamera.TryGetValue(viewIndex, out camName);
            }
            else
            {
                camName = btn?.Tag as string;
            }

            if (string.IsNullOrWhiteSpace(camName)) return;
// Require preview/running pipeline; otherwise there's no handle to tune.
            ICamera cam;
            if (!vision.TryGetCamera(camName, out cam))
            {
                MessageBox.Show($"{camName} 当前未加载。\n\n请先点击“开始预览”（MOCK）或启动 REAL 采集后再调参。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (tuningForms.TryGetValue(camName, out var existing) && existing != null && !existing.IsDisposed)
            {
                try { existing.Activate(); } catch { }
                return;
            }

            var settings = CameraSettingsStore.Load();
            CameraConfig cfg = null;
            if (string.Equals(camName, "Cam1", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam1;
            else if (string.Equals(camName, "Cam2", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam2;
            else if (string.Equals(camName, "Cam3", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam3;
            else if (string.Equals(camName, "Cam4", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam4;
            else if (string.Equals(camName, "Cam5", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam5;
            else if (string.Equals(camName, "Cam6", StringComparison.OrdinalIgnoreCase)) cfg = settings.Cam6;

            if (cfg == null)
            {
                MessageBox.Show("未找到相机配置（camera_settings.json）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var form = new CameraTuningForm(camName, cfg, cam);
            // Place near main form for usability
            try
            {
                var p = this.PointToScreen(new System.Drawing.Point(80, 120));
                form.Location = new System.Drawing.Point(p.X, p.Y);
            }
            catch { }

            form.FormClosed += (_, __) =>
            {
                try { tuningForms.Remove(camName); } catch { }
            };

            tuningForms[camName] = form;
            form.Show(this);
        }

        private void ApplyProductionLockUiEnhanced()
        {
            bool locked = IsProductionMode();
            string[] lockKeywords = { "Setting", "Settings", "Config", "Camera", "PLC", "Plc", "Parameter", "Param" };

            foreach (var c in FindAllControls(this))
            {
                if (c is Button || c is TextBox || c is ComboBox || c is NumericUpDown || c is CheckBox || c is RadioButton)
                {
                    var n = c.Name ?? "";
                    bool isProductionAction =
                        n.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Start", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Stop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Reset", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isProductionAction) continue;

                    bool shouldLock = lockKeywords.Any(k => n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (shouldLock) c.Enabled = !locked;
                }
            }

            // MenuStrip support if present
            foreach (var menu in FindAllControls(this).OfType<MenuStrip>())
            {
                foreach (ToolStripItem item in menu.Items)
                {
                    ApplyMenuItemLock(item, locked, lockKeywords);
                }
            }
        }

        private void ApplyMenuItemLock(ToolStripItem item, bool locked, string[] lockKeywords)
        {
            var n = item.Name ?? "";
            bool shouldLock = lockKeywords.Any(k => n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            if (shouldLock) item.Enabled = !locked;

            if (item is ToolStripMenuItem mi)
            {
                foreach (ToolStripItem sub in mi.DropDownItems)
                    ApplyMenuItemLock(sub, locked, lockKeywords);
            }
        }

        private void InitViewStatusOverlays()
        {
            // Create status overlays within each HWindowControl (so label is tied to each view)
            var hWins = FindAllControls(this).OfType<HWindowControl>().ToList();
            for (int i = 0; i < hWins.Count; i++)
            {
                var hWin = hWins[i];
                if (hWin == null || hWin.IsDisposed) continue;

                var viewIndex = i + 1;
                if (_viewStatusLabels.ContainsKey(viewIndex))
                    continue;

                var lbl = new Label
                {
                    AutoSize = true,
                    BackColor = System.Drawing.Color.Black,
                    ForeColor = System.Drawing.Color.Gainsboro,
                    Text = "UNMAPPED",
                    Padding = new Padding(4, 2, 4, 2)
                };

                // Add into HWindowControl so it overlays inside the image area
                hWin.Controls.Add(lbl);
                lbl.BringToFront();

                void Reposition()
                {
                    // bottom-left inside the view
                    var x = 6;
                    var y = Math.Max(0, hWin.ClientSize.Height - lbl.Height - 6);
                    lbl.Location = new System.Drawing.Point(x, y);
                }

                lbl.SizeChanged += (_, __) => Reposition();
                hWin.Resize += (_, __) => Reposition();
                Reposition();

                _viewStatusLabels[viewIndex] = lbl;
            }
        }

private void UpdateViewStatus(int viewIndex, string text, System.Drawing.Color color)
        {
            if (!_viewStatusLabels.TryGetValue(viewIndex, out var lbl)) return;

            if (lbl.InvokeRequired)
            {
                lbl.BeginInvoke(new Action(() => UpdateViewStatus(viewIndex, text, color)));
                return;
            }

            lbl.Text = text;
            lbl.ForeColor = color;

            // Positioning handled by InitViewStatusOverlays
        }

        
        private int? TryResolveViewIndexForCamera(string camName)
        {
            if (string.IsNullOrWhiteSpace(camName)) return null;
            foreach (var kv in viewToCamera)
            {
                if (string.Equals(kv.Value, camName, StringComparison.OrdinalIgnoreCase))
                    return kv.Key;
            }
            return null;
        }

        private void RefreshAllViewStatusFromMapping()
        {
            for (int i = 1; i <= 6; i++)
            {
                if (!viewToCamera.TryGetValue(i, out var cam) || string.IsNullOrWhiteSpace(cam))
                {
                    UpdateViewStatus(i, "UNMAPPED", System.Drawing.Color.DarkGray);
                    continue;
                }

                if (cameraOnlineUi.TryGetValue(cam, out var online))
                {
                    UpdateViewStatus(i, online ? "ONLINE" : "OFFLINE", online ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red);
                }
                else
                {
                    UpdateViewStatus(i, "INIT", System.Drawing.Color.Gainsboro);
                }
            }
        }

private void InitOverviewBar()
        {
            if (_statusStrip != null) return;

            _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            _lblRunMode = new ToolStripStatusLabel("MODE: INIT");
            _lblCamOnline = new ToolStripStatusLabel("CAM: 0/6");
            _lblPlc1 = new ToolStripStatusLabel("PLC1: ?");
            _lblPlc2 = new ToolStripStatusLabel("PLC2: ?");
            _lblAlarmSummary = new ToolStripStatusLabel("ALARM: -") { Spring = true };
            _lblClock = new ToolStripStatusLabel(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            _statusStrip.Items.Add(_lblRunMode);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblCamOnline);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblPlc1);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblPlc2);
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblAlarmSummary);
            _statusStrip.Items.Add(_lblClock);

            // Log panel (collapsible; default hidden)
            _logPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = _logPanelHeight,
                Visible = false
            };

            _txtUiLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            _logPanel.Controls.Add(_txtUiLog);

            Controls.Add(_logPanel);
            Controls.Add(_statusStrip);
            _statusStrip.BringToFront();

            // Toggle button on status bar
            _lblLogToggle = new ToolStripStatusLabel("LOG ▸")
            {
                IsLink = true
            };
            _lblLogToggle.Click += (_, __) => ToggleLogPanel();
            _statusStrip.Items.Add(new ToolStripSeparator());
            _statusStrip.Items.Add(_lblLogToggle);
_clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (_, __) =>
            {
                if (_lblClock != null) _lblClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            _clockTimer.Start();

            _logger.Info("UI", "Overview bar initialized.");

            // Quick access: click MODE label to open self-check report
            if (_lblRunMode != null)
            {
                _lblRunMode.IsLink = true;
                _lblRunMode.Click += (_, __) => ShowSelfCheckReport(forceReRun: false);
            }
        }

        private bool _startupSelfCheckDone;
        private List<SelfCheckItem> _lastSelfCheck = null;

        private void RunStartupSelfCheckOnce()
        {
            if (_startupSelfCheckDone) return;
            _startupSelfCheckDone = true;
            ShowSelfCheckReport(forceReRun: true);
        }

        private void ShowSelfCheckReport(bool forceReRun)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ShowSelfCheckReport(forceReRun)));
                return;
            }

            if (forceReRun || _lastSelfCheck == null)
            {
                var checker = new StartupSelfChecker();
                _lastSelfCheck = checker.RunAll();

                var fail = _lastSelfCheck.Count(x => !x.Passed);
                if (fail == 0)
                {
                    _logger.Info("SELF", "运行环境自检全部通过。", throttleKey: "selfcheck_ok", minIntervalMs: 5000);
                    return; // don't bother user if everything is OK
                }

                _logger.Warn("SELF", $"运行环境自检发现 {fail} 项失败，已弹出报告窗口。", throttleKey: "selfcheck_fail", minIntervalMs: 3000);
            }

            using (var dlg = new SelfCheckForm(_lastSelfCheck))
            {
                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// Append a single formatted log line to the right-side UI log panel.
        /// This is the logger sink (thread-safe).
        /// </summary>
        private void AppendUiLogLine(string formattedLine)
        {
            if (_txtUiLog == null) return;

            if (_txtUiLog.InvokeRequired)
            {
                _txtUiLog.BeginInvoke(new Action(() => AppendUiLogLine(formattedLine)));
                return;
            }

            lock (_uiLogLock)
            {
                _txtUiLog.AppendText(formattedLine + Environment.NewLine);

                // Keep last N lines to avoid memory growth
                var text = _txtUiLog.Text;
                var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length > _uiLogLinesMax)
                {
                    _txtUiLog.Text = string.Join(Environment.NewLine, lines.Skip(lines.Length - _uiLogLinesMax));
                    _txtUiLog.SelectionStart = _txtUiLog.Text.Length;
                    _txtUiLog.ScrollToCaret();
                }
            }
        }

        // Backward-compatible helper for existing calls
        private void UiLog(string line) => _logger.Info("APP", line);

        private void UpdateOverviewMode()
        {
            if (_lblRunMode == null) return;
            if (InvokeRequired) { BeginInvoke(new Action(UpdateOverviewMode)); return; }
            _lblRunMode.Text = $"MODE: {currentMode}";
        }

        private void UpdateOverviewCameraOnline()
        {
            if (_lblCamOnline == null) return;
            if (InvokeRequired) { BeginInvoke(new Action(UpdateOverviewCameraOnline)); return; }

            int total = allCameras.Count;
            int online = 0;

            foreach (var c in allCameras)
            {
                if (c is HalconFramegrabberCamera hc)
                {
                    if (hc.State == CameraState.Online) online++;
                }
                else
                {
                    if (cameraOnlineUi.TryGetValue(c.Name, out var on) && on) online++;
                }
            }

            _lblCamOnline.Text = total > 0 ? $"CAM: {online}/{total}" : "CAM: -";
        }

        private void UpdateAlarmSummary()
        {
            if (_lblAlarmSummary == null) return;
            if (InvokeRequired) { BeginInvoke(new Action(UpdateAlarmSummary)); return; }

            var parts = new List<string>();

            int camOffline = 0;
            foreach (var c in allCameras)
            {
                if (c is HalconFramegrabberCamera hc)
                {
                    if (hc.State != CameraState.Online) camOffline++;
                }
            }
            if (camOffline > 0) parts.Add($"CAM_OFFLINE({camOffline})");

            if (plcA != null && !plcA.IsConnected) parts.Add("PLC1_OFFLINE");
            if (plcB != null && !plcB.IsConnected) parts.Add("PLC2_OFFLINE");

            _lblAlarmSummary.Text = parts.Count == 0 ? "ALARM: -" : "ALARM: " + string.Join(" | ", parts);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMockAutoCycle();
            try { vision.Dispose(); } catch { }
            try { plcA.Dispose(); } catch { }
            try { plcB.Dispose(); } catch { }
            base.OnFormClosing(e);
        }

        private void ReloadPlcSettingsAndApply()
        {
            plcSettings = PlcSettingsStore.Load();
            // 基本校验；不合法就保持默认但不抛异常
            if (!IPAddress.TryParse(plcSettings.PlcAIp, out _)) plcSettings.PlcAIp = "192.168.0.10";
            if (!IPAddress.TryParse(plcSettings.PlcBIp, out _)) plcSettings.PlcBIp = "192.168.0.11";

            plcA.Configure(plcSettings.PlcAIp, plcSettings.SlaveId);
            plcB.Configure(plcSettings.PlcBIp, plcSettings.SlaveId);
        }

        private void WireCameraOfflineToPlcAlarm()
        {
            // 只绑定一次：使用集合统计组内离线情况
            vision.CameraOnlineChanged += (camName, online) =>
            {
                cameraOnlineUi[camName] = online;
                var mappedView = TryResolveViewIndexForCamera(camName);
                if (mappedView != null)
                {
                    if (online) UpdateViewStatus(mappedView.Value, "ONLINE", System.Drawing.Color.LimeGreen);
                    else UpdateViewStatus(mappedView.Value, "OFFLINE", System.Drawing.Color.Red);
                }
                UpdateOverviewCameraOnline();
                UpdateAlarmSummary();

                var group = ResolveGroup(camName);
                if (group == TriggerGroup.Group1)
                {
                    if (!online) group1Offline.Add(camName);
                    else group1Offline.Remove(camName);

                    plcA.SetAlarmRegister(plcSettings.PlcAAlarmRegister, group1Offline.Count > 0);
                }
                else
                {
                    if (!online) group2Offline.Add(camName);
                    else group2Offline.Remove(camName);

                    plcB.SetAlarmRegister(plcSettings.PlcBAlarmRegister, group2Offline.Count > 0);
                }
            };
        }

        private static TriggerGroup ResolveGroup(string camName)
        {
            return (camName is "Cam1" or "Cam2" or "Cam3") ? TriggerGroup.Group1 : TriggerGroup.Group2;
        }

        private void OnImageReady(string name, HObject image)
        {
            if (IsDisposed) { image?.Dispose(); return; }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnImageReady(name, image)));
                return;
            }

            try
            {
                if (cameraNameToWindow.TryGetValue(name, out var win) && win != null)
                {
                    // Throttle draw to keep UI responsive.
                    var now = Stopwatch.GetTimestamp();
                    if (lastDrawTicks.TryGetValue(name, out var last) && (now - last) < MinDrawIntervalTicks)
                        return;

                    lastDrawTicks[name] = now;

                    win.HalconWindow.ClearWindow();
                    HOperatorSet.DispObj(image, win.HalconWindow);
                    MarkFrameForCamera(name);
                }
            }
            catch
            {
                // display errors should not crash acquisition
            }
            finally
            {
                image?.Dispose();
            }
        }

        private void ApplyRunMode(RunMode mode)
        {
            // 收口：停止旧链路
            StopMockAutoCycle();
            try { vision.Stop(); } catch { }
            try { vision.Clear(); } catch { }
            group1Offline.Clear();
            group2Offline.Clear();
            realCamCount = 0;
            allCameras.Clear();

            if (mode == RunMode.Mock)
            {
                // MOCK: 支持真实相机预览（不依赖 PLC）。若某路未配置，则用 Mock 相机占位。
                var camSettings = CameraSettingsStore.Load();
                var cams = new[]
                {
                    camSettings.Cam1, camSettings.Cam2, camSettings.Cam3,
                    camSettings.Cam4, camSettings.Cam5, camSettings.Cam6
                };

                for (int i = 0; i < cams.Length; i++)
                {
                    var cfg = cams[i];
                    if (string.IsNullOrWhiteSpace(cfg.Name))
                        cfg.Name = $"Cam{i + 1}";

                    ICamera cam;
                    bool hasDevice = !string.IsNullOrWhiteSpace(cfg.Device) && !string.Equals(cfg.Device.Trim(), "default", StringComparison.OrdinalIgnoreCase);

                    if (hasDevice)
                    {
                        // 真实相机（HALCON）
                        cam = new HalconFramegrabberCamera(cfg);
                    }
                    else
                    {
                        // 占位 Mock 相机
                        cam = new MockCamera(cfg.Name)
                        {
                            FaultRate = 0.05,
                            RecoveryMs = 1500
                        };
                    }

                    allCameras.Add(cam);
                    int initialViewIndex = i + 1;

                    cam.StatusChanged += (n, st, err) =>
                    {
                        var resolvedViewIndex = TryResolveViewIndexForCamera(n);
                        if (resolvedViewIndex == null)
                        {
                            // If this camera is not bound to any view, do not toggle any view label.
                            return;
                        }

                        string shortErr = (err ?? "").Replace("\r", " ").Replace("\n", " ");
                        if (shortErr.Length > 40) shortErr = shortErr.Substring(0, 40) + "...";

                        if (st == CameraState.Online)
                        {
                            UpdateViewStatus(resolvedViewIndex.Value, "ONLINE", System.Drawing.Color.LimeGreen);
                        }
                        else if (st == CameraState.Connecting)
                        {
                            UpdateViewStatus(resolvedViewIndex.Value, "CONNECTING...", System.Drawing.Color.Orange);
                        }
                        else
                        {
                            int remain = 0;
                            try { remain = Math.Max(0, (int)(cam.NextRetryAt - DateTime.Now).TotalSeconds); } catch { }
                            var msg = remain > 0 ? $"OFFLINE (retry {remain}s)" : "OFFLINE";
                            if (!string.IsNullOrWhiteSpace(shortErr)) msg += " | " + shortErr;
                            UpdateViewStatus(resolvedViewIndex.Value, msg, System.Drawing.Color.Red);
                        }

                        UpdateOverviewMode();
                        UpdateOverviewCameraOnline();
                        UpdateAlarmSummary();
                    };

                    vision.AddCamera(cam, (i < 3) ? TriggerGroup.Group1 : TriggerGroup.Group2);
                }

                vision.Start();
                StartMockAutoCycle();
            }
            else
            {
                // Real mode: load persisted camera settings and build 6 cameras automatically.
                var camSettings = CameraSettingsStore.Load();
                var cams = new[]
                {
                    camSettings.Cam1, camSettings.Cam2, camSettings.Cam3,
                    camSettings.Cam4, camSettings.Cam5, camSettings.Cam6
                };

                for (int i = 0; i < cams.Length; i++)
                {
                    var cfg = cams[i];
                    if (string.IsNullOrWhiteSpace(cfg.Name))
                        cfg.Name = $"Cam{i+1}";

                    var cam = new HalconFramegrabberCamera(cfg);
                    allCameras.Add(cam);
                    int viewIndex = i + 1;
                    cam.StatusChanged += (n, st, err) =>
                    {
                        var resolvedViewIndex = TryResolveViewIndexForCamera(n);
                        if (resolvedViewIndex == null)
                        {
                            // If this camera is not bound to any view, do not toggle any view label.
                            return;
                        }

                        string shortErr = (err ?? "").Replace("\r", " ").Replace("\n", " ");
                        if (shortErr.Length > 40) shortErr = shortErr.Substring(0, 40) + "...";

                        if (st == CameraState.Online)
                        {
                            UpdateViewStatus(resolvedViewIndex.Value, "ONLINE", System.Drawing.Color.LimeGreen);
                        }
                        else if (st == CameraState.Connecting)
                        {
                            UpdateViewStatus(resolvedViewIndex.Value, "CONNECTING...", System.Drawing.Color.Orange);
                        }
                        else
                        {
                            int remain = 0;
                            try { remain = Math.Max(0, (int)(cam.NextRetryAt - DateTime.Now).TotalSeconds); } catch { }
                            var msg = remain > 0 ? $"OFFLINE (retry {remain}s)" : "OFFLINE";
                            if (!string.IsNullOrWhiteSpace(shortErr)) msg += " | " + shortErr;
                            UpdateViewStatus(resolvedViewIndex.Value, msg, System.Drawing.Color.Red);
                        }

                        UpdateOverviewMode();
                        UpdateOverviewCameraOnline();
                        UpdateAlarmSummary();
                    };
                    vision.AddCamera(cam, (i < 3) ? TriggerGroup.Group1 : TriggerGroup.Group2);
                }

                vision.Start();
            }

            currentMode = mode;
            AppRuntimeState.ProductionLocked = (mode == RunMode.Real);
            ApplyProductionLockUiEnhanced();
            UpdateOverviewMode();
            UpdateOverviewCameraOnline();
            UpdateAlarmSummary();
        }

        private void StartMockAutoCycle()
        {
            if (!chkMockAutoCycle.Checked) return;

            StopMockAutoCycle();
            mockCycleTimer = new System.Windows.Forms.Timer();
            mockCycleTimer.Interval = Math.Max(50, mockCycleMs);
            mockCycleTimer.Tick += (_, __) =>
            {
                // 3+3 group trigger
                vision.TriggerGroup(TriggerGroup.Group1);
                vision.TriggerGroup(TriggerGroup.Group2);
            };
            mockCycleTimer.Start();
        }

        private void StopMockAutoCycle()
        {
            if (mockCycleTimer == null) return;
            try { mockCycleTimer.Stop(); } catch { }
            try { mockCycleTimer.Dispose(); } catch { }
            mockCycleTimer = null;
        }

        // UI Events
        private void btnApplyMode_Click(object sender, EventArgs e)
        {
            var mode = (cmbRunMode.SelectedIndex == 0) ? RunMode.Mock : RunMode.Real;
            ApplyRunMode(mode);
        }

        private void btnTriggerAll_Click(object sender, EventArgs e)
        {
            vision.TriggerOnceAll();
        }

        private void btnSimPlcA_Click(object sender, EventArgs e)
        {
            // Simulate PLC A trigger (group1)
            vision.TriggerGroup(TriggerGroup.Group1);
        }

        private void btnSimPlcB_Click(object sender, EventArgs e)
        {
            // Simulate PLC B trigger (group2)
            vision.TriggerGroup(TriggerGroup.Group2);
        }

        private void chkMockAutoCycle_CheckedChanged(object sender, EventArgs e)
        {
            if (cmbRunMode.SelectedIndex != 0) return; // only mock
            if (chkMockAutoCycle.Checked)
            {
                StartMockAutoCycle();
                try { btnMockPreview.Text = "停止预览"; } catch { }
            }
            else
            {
                StopMockAutoCycle();
                try { btnMockPreview.Text = "开始预览"; } catch { }
            }
        }

        private void btnMockPreview_Click(object sender, EventArgs e)
        {
            if (cmbRunMode.SelectedIndex != 0)
            {
                MessageBox.Show("预览仅在 MOCK 模式可用。\n\n请先切换到 MOCK 模式。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Toggle preview loop
            chkMockAutoCycle.Checked = !chkMockAutoCycle.Checked;
        }

        private void txtMockCycleMs_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtMockCycleMs.Text.Trim(), out var ms) && ms >= 50)
            {
                mockCycleMs = ms;
                if (cmbRunMode.SelectedIndex == 0 && chkMockAutoCycle.Checked)
                {
                    StartMockAutoCycle();
                }
            }
        }

        private void btnPlcSettings_Click(object sender, EventArgs e)
        {
            OpenPlcSettingsLocked();
        }

        
        private void btnAddCamera_Click(object sender, EventArgs e)
        {
            if (currentMode != RunMode.Real)
            {
                MessageBox.Show("请先切换到 Real 模式再添加真实相机。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (realCamCount >= 6)
            {
                MessageBox.Show("最多添加 6 台相机。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ifaceText = comboBoxInterfaceType.SelectedItem?.ToString() ?? "GigEVision2";
            var device = comboBoxDeviceList.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(device) || device.StartsWith("("))
            {
                MessageBox.Show("请输入或选择设备标识（Device）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CameraInterfaceType it = CameraInterfaceType.GigEVision2;
            if (ifaceText.Contains("USB3", StringComparison.OrdinalIgnoreCase))
                it = CameraInterfaceType.USB3Vision;
            else if (ifaceText.Contains("DirectShow", StringComparison.OrdinalIgnoreCase))
                it = CameraInterfaceType.DirectShow;

            realCamCount++;
            string name = $"Cam{realCamCount}";

            var cfg = new CameraConfig
            {
                Name = name,
                InterfaceType = it,
                Device = device,
                Port = 0
            };

            ICamera cam = new HalconFramegrabberCamera(cfg);

            // 默认按 1-3 / 4-6 分组
            var group = (realCamCount <= 3) ? TriggerGroup.Group1 : TriggerGroup.Group2;
            vision.AddCamera(cam, group);

            MessageBox.Show($"已添加 {name}（{it}）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void btnCameraSettings_Click(object sender, EventArgs e)
        {
            OpenCameraSettingsLocked();
        }

}
}
