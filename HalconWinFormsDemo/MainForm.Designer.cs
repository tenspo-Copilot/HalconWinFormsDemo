namespace HalconWinFormsDemo
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ComboBox cmbRunMode;
        private System.Windows.Forms.Button btnApplyMode;

        private System.Windows.Forms.Button btnTriggerAll;
        private System.Windows.Forms.Button btnMockPreview;
        private System.Windows.Forms.Button btnSimPlcA;
        private System.Windows.Forms.Button btnSimPlcB;

        private System.Windows.Forms.CheckBox chkMockAutoCycle;
        private System.Windows.Forms.TextBox txtMockCycleMs;
        private System.Windows.Forms.Label lblMockCycle;

        private System.Windows.Forms.ComboBox comboBoxInterfaceType;
        private System.Windows.Forms.ComboBox comboBoxDeviceList;
        private System.Windows.Forms.Button btnAddCamera;

        private System.Windows.Forms.Button btnPlcSettings;
        private System.Windows.Forms.Button btnCameraSettings;

        private HalconDotNet.HWindowControl hWindowControl1;
        private HalconDotNet.HWindowControl hWindowControl2;
        private HalconDotNet.HWindowControl hWindowControl3;
        private HalconDotNet.HWindowControl hWindowControl4;
        private HalconDotNet.HWindowControl hWindowControl5;
        private HalconDotNet.HWindowControl hWindowControl6;

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel topPanel;

        private System.Windows.Forms.Panel viewPanel1;
        private System.Windows.Forms.Panel viewPanel2;
        private System.Windows.Forms.Panel viewPanel3;
        private System.Windows.Forms.Panel viewPanel4;
        private System.Windows.Forms.Panel viewPanel5;
        private System.Windows.Forms.Panel viewPanel6;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.cmbRunMode = new System.Windows.Forms.ComboBox();
            this.btnApplyMode = new System.Windows.Forms.Button();
            this.btnTriggerAll = new System.Windows.Forms.Button();
            this.btnMockPreview = new System.Windows.Forms.Button();
            this.btnSimPlcA = new System.Windows.Forms.Button();
            this.btnSimPlcB = new System.Windows.Forms.Button();
            this.chkMockAutoCycle = new System.Windows.Forms.CheckBox();
            this.txtMockCycleMs = new System.Windows.Forms.TextBox();
            this.lblMockCycle = new System.Windows.Forms.Label();

            this.comboBoxInterfaceType = new System.Windows.Forms.ComboBox();
            this.comboBoxDeviceList = new System.Windows.Forms.ComboBox();
            this.btnAddCamera = new System.Windows.Forms.Button();

            this.btnPlcSettings = new System.Windows.Forms.Button();
            this.btnCameraSettings = new System.Windows.Forms.Button();

            this.hWindowControl1 = new HalconDotNet.HWindowControl();
            this.hWindowControl2 = new HalconDotNet.HWindowControl();
            this.hWindowControl3 = new HalconDotNet.HWindowControl();
            this.hWindowControl4 = new HalconDotNet.HWindowControl();
            this.hWindowControl5 = new HalconDotNet.HWindowControl();
            this.hWindowControl6 = new HalconDotNet.HWindowControl();

            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.topPanel = new System.Windows.Forms.FlowLayoutPanel();

            this.viewPanel1 = new System.Windows.Forms.Panel();
            this.viewPanel2 = new System.Windows.Forms.Panel();
            this.viewPanel3 = new System.Windows.Forms.Panel();
            this.viewPanel4 = new System.Windows.Forms.Panel();
            this.viewPanel5 = new System.Windows.Forms.Panel();
            this.viewPanel6 = new System.Windows.Forms.Panel();

            this.SuspendLayout();

            // MainForm
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Halcon WinForms Demo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(1100, 720);

            // topPanel
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Height = 52;
            this.topPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.topPanel.WrapContents = false;
            this.topPanel.AutoScroll = true;
            this.topPanel.BackColor = System.Drawing.SystemColors.Control;

            // cmbRunMode
            this.cmbRunMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRunMode.Width = 140;
            this.cmbRunMode.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);

            // btnApplyMode
            this.btnApplyMode.Text = "应用模式";
            this.btnApplyMode.Width = 90;
            this.btnApplyMode.Height = 28;
            this.btnApplyMode.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);

            // btnMockPreview
            this.btnMockPreview.Text = "开始预览";
            this.btnMockPreview.Width = 90;
            this.btnMockPreview.Height = 28;
            this.btnMockPreview.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);

            // btnTriggerAll
            this.btnTriggerAll.Text = "触发(3+3)";
            this.btnTriggerAll.Width = 90;
            this.btnTriggerAll.Height = 28;
            this.btnTriggerAll.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);

            // chkMockAutoCycle
            this.chkMockAutoCycle.Text = "预览循环触发";
            this.chkMockAutoCycle.AutoSize = true;
            this.chkMockAutoCycle.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);

            // lblMockCycle
            this.lblMockCycle.Text = "周期(ms)";
            this.lblMockCycle.AutoSize = true;
            this.lblMockCycle.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);

            // txtMockCycleMs
            this.txtMockCycleMs.Width = 70;
            this.txtMockCycleMs.Margin = new System.Windows.Forms.Padding(0, 2, 12, 0);

            // btnSimPlcA / btnSimPlcB
            this.btnSimPlcA.Text = "模拟PLC-A";
            this.btnSimPlcA.Width = 95;
            this.btnSimPlcA.Height = 28;
            this.btnSimPlcA.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            this.btnSimPlcB.Text = "模拟PLC-B";
            this.btnSimPlcB.Width = 95;
            this.btnSimPlcB.Height = 28;
            this.btnSimPlcB.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);

            // btnCameraSettings / btnPlcSettings
            this.btnCameraSettings.Text = "相机映射设置";
            this.btnCameraSettings.Width = 110;
            this.btnCameraSettings.Height = 28;
            this.btnCameraSettings.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            this.btnPlcSettings.Text = "PLC设置";
            this.btnPlcSettings.Width = 90;
            this.btnPlcSettings.Height = 28;
            this.btnPlcSettings.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);

            // (optional) interface/device quick controls
            this.comboBoxInterfaceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInterfaceType.Width = 160;
            this.comboBoxInterfaceType.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            this.comboBoxDeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDeviceList.Width = 220;
            this.comboBoxDeviceList.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            this.btnAddCamera.Text = "添加";
            this.btnAddCamera.Width = 60;
            this.btnAddCamera.Height = 28;
            this.btnAddCamera.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);

            // topPanel controls order
            this.topPanel.Controls.Add(this.cmbRunMode);
            this.topPanel.Controls.Add(this.btnApplyMode);
            this.topPanel.Controls.Add(this.btnMockPreview);
            this.topPanel.Controls.Add(this.btnTriggerAll);
            this.topPanel.Controls.Add(this.chkMockAutoCycle);
            this.topPanel.Controls.Add(this.lblMockCycle);
            this.topPanel.Controls.Add(this.txtMockCycleMs);
            this.topPanel.Controls.Add(this.btnSimPlcA);
            this.topPanel.Controls.Add(this.btnSimPlcB);
            this.topPanel.Controls.Add(this.btnCameraSettings);
            this.topPanel.Controls.Add(this.btnPlcSettings);
            this.topPanel.Controls.Add(this.comboBoxInterfaceType);
            this.topPanel.Controls.Add(this.comboBoxDeviceList);
            this.topPanel.Controls.Add(this.btnAddCamera);

            // tableLayoutPanel
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.ColumnCount = 3;
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(8);
            this.tableLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
            this.tableLayoutPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));

            // view panels (each contains HWindowControl and overlay buttons at runtime)
            System.Windows.Forms.Panel[] panels = new[] {
                this.viewPanel1, this.viewPanel2, this.viewPanel3, this.viewPanel4, this.viewPanel5, this.viewPanel6
            };
            foreach (var p in panels)
            {
                p.Dock = System.Windows.Forms.DockStyle.Fill;
                p.Margin = new System.Windows.Forms.Padding(6);
                p.BackColor = System.Drawing.Color.Black;
            }

            // HWindowControls
            HalconDotNet.HWindowControl[] views = new[] {
                this.hWindowControl1, this.hWindowControl2, this.hWindowControl3, this.hWindowControl4, this.hWindowControl5, this.hWindowControl6
            };
            foreach (var v in views)
            {
                v.Dock = System.Windows.Forms.DockStyle.Fill;
                v.BackColor = System.Drawing.Color.Black;
                v.BorderColor = System.Drawing.Color.DimGray;
            }

            // add views into panels
            this.viewPanel1.Controls.Add(this.hWindowControl1);
            this.viewPanel2.Controls.Add(this.hWindowControl2);
            this.viewPanel3.Controls.Add(this.hWindowControl3);
            this.viewPanel4.Controls.Add(this.hWindowControl4);
            this.viewPanel5.Controls.Add(this.hWindowControl5);
            this.viewPanel6.Controls.Add(this.hWindowControl6);

            // add panels into grid
            this.tableLayoutPanel.Controls.Add(this.viewPanel1, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.viewPanel2, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.viewPanel3, 2, 0);
            this.tableLayoutPanel.Controls.Add(this.viewPanel4, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.viewPanel5, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.viewPanel6, 2, 1);

            // add to form
            this.Controls.Add(this.tableLayoutPanel);
            this.Controls.Add(this.topPanel);

            this.ResumeLayout(false);
        }
    }
}
