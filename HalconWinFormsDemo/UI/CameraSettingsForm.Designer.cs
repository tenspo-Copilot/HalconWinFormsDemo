namespace HalconWinFormsDemo.UI
{
    partial class CameraSettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.comboCamera = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboInterface = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDevice = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnEnumDevices = new System.Windows.Forms.Button();
            this.btnAggregateEnum = new System.Windows.Forms.Button();
            this.listDevices = new System.Windows.Forms.ListBox();
            this.btnTestOpen = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.btnScanInterfaces = new System.Windows.Forms.Button();
            this.txtInterfaceScan = new System.Windows.Forms.TextBox();
            this.grpMapping = new System.Windows.Forms.GroupBox();
            this.btnAutoAssign = new System.Windows.Forms.Button();
            this.cmbMap1 = new System.Windows.Forms.ComboBox();
            this.cmbMap2 = new System.Windows.Forms.ComboBox();
            this.cmbMap3 = new System.Windows.Forms.ComboBox();
            this.cmbMap4 = new System.Windows.Forms.ComboBox();
            this.cmbMap5 = new System.Windows.Forms.ComboBox();
            this.cmbMap6 = new System.Windows.Forms.ComboBox();
            this.lblMap1 = new System.Windows.Forms.Label();
            this.lblMap2 = new System.Windows.Forms.Label();
            this.lblMap3 = new System.Windows.Forms.Label();
            this.lblMap4 = new System.Windows.Forms.Label();
            this.lblMap5 = new System.Windows.Forms.Label();
            this.lblMap6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboCamera
            // 
            this.comboCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCamera.FormattingEnabled = true;
            this.comboCamera.Location = new System.Drawing.Point(108, 18);
            this.comboCamera.Name = "comboCamera";
            this.comboCamera.Size = new System.Drawing.Size(173, 23);
            this.comboCamera.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "选择相机：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "接口类型：";
            // 
            // comboInterface
            // 
            this.comboInterface.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboInterface.FormattingEnabled = true;
            this.comboInterface.Location = new System.Drawing.Point(108, 54);
            this.comboInterface.Name = "comboInterface";
            this.comboInterface.Size = new System.Drawing.Size(173, 23);
            this.comboInterface.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "Device：";
            // 
            // txtDevice
            // 
            this.txtDevice.Location = new System.Drawing.Point(108, 90);
            this.txtDevice.Name = "txtDevice";
            this.txtDevice.Size = new System.Drawing.Size(475, 23);
            this.txtDevice.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(22, 127);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 15);
            this.label4.TabIndex = 6;
            this.label4.Text = "Port：";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(108, 124);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(173, 23);
            this.txtPort.TabIndex = 7;
            this.txtPort.Text = "0";
            // 
            // btnEnumDevices
            // 
            this.btnEnumDevices.Location = new System.Drawing.Point(309, 123);
            this.btnEnumDevices.Name = "btnEnumDevices";
            this.btnEnumDevices.Size = new System.Drawing.Size(117, 25);
            this.btnEnumDevices.TabIndex = 8;
            this.btnEnumDevices.Text = "枚举设备";
            this.btnEnumDevices.UseVisualStyleBackColor = true;
            // 
            
            // btnAggregateEnum
            this.btnAggregateEnum.Location = new System.Drawing.Point(12, 123);
            this.btnAggregateEnum.Name = "btnAggregateEnum";
            this.btnAggregateEnum.Size = new System.Drawing.Size(90, 25);
            this.btnAggregateEnum.TabIndex = 8;
            this.btnAggregateEnum.Text = "跨接口枚举";
            this.btnAggregateEnum.UseVisualStyleBackColor = true;
            this.btnAggregateEnum.Click += new System.EventHandler(this.btnAggregateEnum_Click);

// listDevices
            // 
            this.listDevices.FormattingEnabled = true;
            this.listDevices.ItemHeight = 15;
            this.listDevices.Location = new System.Drawing.Point(108, 163);
            this.listDevices.Name = "listDevices";
            this.listDevices.Size = new System.Drawing.Size(475, 154);
            this.listDevices.TabIndex = 9;
            // 
            // btnTestOpen
            // 
            this.btnTestOpen.Location = new System.Drawing.Point(466, 123);
            this.btnTestOpen.Name = "btnTestOpen";
            this.btnTestOpen.Size = new System.Drawing.Size(117, 25);
            this.btnTestOpen.TabIndex = 10;
            this.btnTestOpen.Text = "测试打开";
            this.btnTestOpen.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(372, 485);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(101, 30);
            this.btnOk.TabIndex = 11;
            this.btnOk.Text = "确定";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(482, 485);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(101, 30);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            
            // btnScanInterfaces
            this.btnScanInterfaces.Location = new System.Drawing.Point(12, 163);
            this.btnScanInterfaces.Name = "btnScanInterfaces";
            this.btnScanInterfaces.Size = new System.Drawing.Size(90, 25);
            this.btnScanInterfaces.TabIndex = 14;
            this.btnScanInterfaces.Text = "扫描接口";
            this.btnScanInterfaces.UseVisualStyleBackColor = true;
            this.btnScanInterfaces.Click += new System.EventHandler(this.btnScanInterfaces_Click);

            // txtInterfaceScan
            this.txtInterfaceScan.Location = new System.Drawing.Point(12, 194);
            this.txtInterfaceScan.Multiline = true;
            this.txtInterfaceScan.Name = "txtInterfaceScan";
            this.txtInterfaceScan.ReadOnly = true;
            this.txtInterfaceScan.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtInterfaceScan.Size = new System.Drawing.Size(90, 123);
            this.txtInterfaceScan.TabIndex = 15;


            // grpMapping
            this.grpMapping.Location = new System.Drawing.Point(108, 325);
            this.grpMapping.Name = "grpMapping";
            this.grpMapping.Size = new System.Drawing.Size(475, 190);
            this.grpMapping.TabIndex = 16;
            this.grpMapping.TabStop = false;
            this.grpMapping.Text = "画面-相机映射（用枚举到的设备编码分配到画面1~6）";

            this.grpMapping.Controls.Add(this.btnAutoAssign);
            this.grpMapping.Controls.Add(this.lblMap1);
            this.grpMapping.Controls.Add(this.cmbMap1);
            this.grpMapping.Controls.Add(this.lblMap2);
            this.grpMapping.Controls.Add(this.cmbMap2);
            this.grpMapping.Controls.Add(this.lblMap3);
            this.grpMapping.Controls.Add(this.cmbMap3);
            this.grpMapping.Controls.Add(this.lblMap4);
            this.grpMapping.Controls.Add(this.cmbMap4);
            this.grpMapping.Controls.Add(this.lblMap5);
            this.grpMapping.Controls.Add(this.cmbMap5);
            this.grpMapping.Controls.Add(this.lblMap6);
            this.grpMapping.Controls.Add(this.cmbMap6);


            // btnAutoAssign
            this.btnAutoAssign.Location = new System.Drawing.Point(360, 20);
            this.btnAutoAssign.Name = "btnAutoAssign";
            this.btnAutoAssign.Size = new System.Drawing.Size(95, 25);
            this.btnAutoAssign.TabIndex = 17;
            this.btnAutoAssign.Text = "自动分配";
            this.btnAutoAssign.UseVisualStyleBackColor = true;
            this.btnAutoAssign.Click += new System.EventHandler(this.btnAutoAssign_Click);

            // lblMap1
            this.lblMap1.AutoSize = true;
            this.lblMap1.Location = new System.Drawing.Point(12, 25);
            this.lblMap1.Name = "lblMap1";
            this.lblMap1.Size = new System.Drawing.Size(51, 15);
            this.lblMap1.TabIndex = 18;
            this.lblMap1.Text = "画面1:";

            // cmbMap1
            this.cmbMap1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap1.FormattingEnabled = true;
            this.cmbMap1.Location = new System.Drawing.Point(70, 22);
            this.cmbMap1.Name = "cmbMap1";
            this.cmbMap1.Size = new System.Drawing.Size(280, 23);
            this.cmbMap1.TabIndex = 19;

            // lblMap2
            this.lblMap2.AutoSize = true;
            this.lblMap2.Location = new System.Drawing.Point(12, 54);
            this.lblMap2.Name = "lblMap2";
            this.lblMap2.Size = new System.Drawing.Size(51, 15);
            this.lblMap2.TabIndex = 20;
            this.lblMap2.Text = "画面2:";

            // cmbMap2
            this.cmbMap2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap2.FormattingEnabled = true;
            this.cmbMap2.Location = new System.Drawing.Point(70, 51);
            this.cmbMap2.Name = "cmbMap2";
            this.cmbMap2.Size = new System.Drawing.Size(280, 23);
            this.cmbMap2.TabIndex = 21;

            // lblMap3
            this.lblMap3.AutoSize = true;
            this.lblMap3.Location = new System.Drawing.Point(12, 83);
            this.lblMap3.Name = "lblMap3";
            this.lblMap3.Size = new System.Drawing.Size(51, 15);
            this.lblMap3.TabIndex = 22;
            this.lblMap3.Text = "画面3:";

            // cmbMap3
            this.cmbMap3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap3.FormattingEnabled = true;
            this.cmbMap3.Location = new System.Drawing.Point(70, 80);
            this.cmbMap3.Name = "cmbMap3";
            this.cmbMap3.Size = new System.Drawing.Size(280, 23);
            this.cmbMap3.TabIndex = 23;

            // lblMap4
            this.lblMap4.AutoSize = true;
            this.lblMap4.Location = new System.Drawing.Point(12, 112);
            this.lblMap4.Name = "lblMap4";
            this.lblMap4.Size = new System.Drawing.Size(51, 15);
            this.lblMap4.TabIndex = 24;
            this.lblMap4.Text = "画面4:";

            // cmbMap4
            this.cmbMap4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap4.FormattingEnabled = true;
            this.cmbMap4.Location = new System.Drawing.Point(70, 109);
            this.cmbMap4.Name = "cmbMap4";
            this.cmbMap4.Size = new System.Drawing.Size(280, 23);
            this.cmbMap4.TabIndex = 25;

            // lblMap5
            this.lblMap5.AutoSize = true;
            this.lblMap5.Location = new System.Drawing.Point(12, 141);
            this.lblMap5.Name = "lblMap5";
            this.lblMap5.Size = new System.Drawing.Size(51, 15);
            this.lblMap5.TabIndex = 26;
            this.lblMap5.Text = "画面5:";

            // cmbMap5
            this.cmbMap5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap5.FormattingEnabled = true;
            this.cmbMap5.Location = new System.Drawing.Point(70, 138);
            this.cmbMap5.Name = "cmbMap5";
            this.cmbMap5.Size = new System.Drawing.Size(280, 23);
            this.cmbMap5.TabIndex = 27;

            // lblMap6
            this.lblMap6.AutoSize = true;
            this.lblMap6.Location = new System.Drawing.Point(12, 170);
            this.lblMap6.Name = "lblMap6";
            this.lblMap6.Size = new System.Drawing.Size(51, 15);
            this.lblMap6.TabIndex = 28;
            this.lblMap6.Text = "画面6:";

            // cmbMap6
            this.cmbMap6.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMap6.FormattingEnabled = true;
            this.cmbMap6.Location = new System.Drawing.Point(70, 167);
            this.cmbMap6.Name = "cmbMap6";
            this.cmbMap6.Size = new System.Drawing.Size(280, 23);
            this.cmbMap6.TabIndex = 29;
// label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(105, 475);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(440, 15);
            this.label5.TabIndex = 13;
            this.label5.Text = "提示：双击设备列表可自动填充到 Device。GigE/USB3Vision 需安装对应接口驱动。";
            // 
            // CameraSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(612, 520);
            this.Controls.Add(this.txtInterfaceScan);
            this.Controls.Add(this.btnScanInterfaces);
            this.Controls.Add(this.grpMapping);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnTestOpen);
            this.Controls.Add(this.listDevices);
            this.Controls.Add(this.btnAggregateEnum);
            this.Controls.Add(this.btnEnumDevices);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtDevice);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboInterface);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboCamera);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CameraSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "相机设置";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboCamera;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboInterface;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDevice;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnEnumDevices;
        private System.Windows.Forms.Button btnAggregateEnum;
        private System.Windows.Forms.ListBox listDevices;
        private System.Windows.Forms.Button btnTestOpen;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnScanInterfaces;
        private System.Windows.Forms.TextBox txtInterfaceScan;
        private System.Windows.Forms.GroupBox grpMapping;
        private System.Windows.Forms.ComboBox cmbMap1;
        private System.Windows.Forms.ComboBox cmbMap2;
        private System.Windows.Forms.ComboBox cmbMap3;
        private System.Windows.Forms.ComboBox cmbMap4;
        private System.Windows.Forms.ComboBox cmbMap5;
        private System.Windows.Forms.ComboBox cmbMap6;
        private System.Windows.Forms.Button btnAutoAssign;
        private System.Windows.Forms.Label lblMap1;
        private System.Windows.Forms.Label lblMap2;
        private System.Windows.Forms.Label lblMap3;
        private System.Windows.Forms.Label lblMap4;
        private System.Windows.Forms.Label lblMap5;
        private System.Windows.Forms.Label lblMap6;
    }
}
