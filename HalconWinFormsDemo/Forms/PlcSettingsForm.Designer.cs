namespace HalconWinFormsDemo.Forms
{
    partial class PlcSettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtPlcAIp;
        private System.Windows.Forms.TextBox txtPlcBIp;
        private System.Windows.Forms.TextBox txtPlcAAlarmReg;
        private System.Windows.Forms.TextBox txtPlcBAlarmReg;
        private System.Windows.Forms.TextBox txtSlaveId;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnTestPlcA;
        private System.Windows.Forms.Button btnTestPlcB;
        private System.Windows.Forms.Button btnPlcAOn;
        private System.Windows.Forms.Button btnPlcAOff;
        private System.Windows.Forms.Button btnPlcBOn;
        private System.Windows.Forms.Button btnPlcBOff;
        private System.Windows.Forms.Label lblHint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtPlcAIp = new System.Windows.Forms.TextBox();
            this.txtPlcBIp = new System.Windows.Forms.TextBox();
            this.txtPlcAAlarmReg = new System.Windows.Forms.TextBox();
            this.txtPlcBAlarmReg = new System.Windows.Forms.TextBox();
            this.txtSlaveId = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnTestPlcA = new System.Windows.Forms.Button();
            this.btnTestPlcB = new System.Windows.Forms.Button();
            this.btnPlcAOn = new System.Windows.Forms.Button();
            this.btnPlcAOff = new System.Windows.Forms.Button();
            this.btnPlcBOn = new System.Windows.Forms.Button();
            this.btnPlcBOff = new System.Windows.Forms.Button();
            this.lblHint = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // lblHint
            this.lblHint.AutoSize = true;
            this.lblHint.Left = 15;
            this.lblHint.Top = 10;
            this.lblHint.Text = "地址为 0-based。报警寄存器：1=报警，0=清除";

            int y = 40;
            int labelW = 120;
            int boxW = 180;
            int left = 15;

            System.Windows.Forms.Label MakeLabel(string text, int top)
            {
                var l = new System.Windows.Forms.Label();
                l.AutoSize = true;
                l.Left = left;
                l.Top = top + 5;
                l.Text = text;
                return l;
            }

            void PlaceBox(System.Windows.Forms.TextBox box, int top)
            {
                box.Left = left + labelW;
                box.Top = top;
                box.Width = boxW;
            }

            // PLC A IP
            var lblPlcAIp = MakeLabel("PLC A IP", y);
            PlaceBox(this.txtPlcAIp, y);
            this.btnTestPlcA.Left = left + labelW + boxW + 10;
            this.btnTestPlcA.Top = y;
            this.btnTestPlcA.Width = 70;
            this.btnTestPlcA.Text = "测试";
            this.btnTestPlcA.Click += new System.EventHandler(this.btnTestPlcA_Click);
            y += 35;

            // PLC B IP
            var lblPlcBIp = MakeLabel("PLC B IP", y);
            PlaceBox(this.txtPlcBIp, y);
            this.btnTestPlcB.Left = left + labelW + boxW + 10;
            this.btnTestPlcB.Top = y;
            this.btnTestPlcB.Width = 70;
            this.btnTestPlcB.Text = "测试";
            this.btnTestPlcB.Click += new System.EventHandler(this.btnTestPlcB_Click);
            y += 35;

            // SlaveId
            var lblSlave = MakeLabel("SlaveId", y);
            PlaceBox(this.txtSlaveId, y);
            y += 35;

            // PLC A Alarm Reg + on/off
            var lblAReg = MakeLabel("PLC A 报警寄存器", y);
            PlaceBox(this.txtPlcAAlarmReg, y);
            this.btnPlcAOn.Left = left + labelW + boxW + 10;
            this.btnPlcAOn.Top = y;
            this.btnPlcAOn.Width = 50;
            this.btnPlcAOn.Text = "置1";
            this.btnPlcAOn.Click += new System.EventHandler(this.btnPlcAOn_Click);

            this.btnPlcAOff.Left = this.btnPlcAOn.Left + 55;
            this.btnPlcAOff.Top = y;
            this.btnPlcAOff.Width = 50;
            this.btnPlcAOff.Text = "置0";
            this.btnPlcAOff.Click += new System.EventHandler(this.btnPlcAOff_Click);
            y += 35;

            // PLC B Alarm Reg + on/off
            var lblBReg = MakeLabel("PLC B 报警寄存器", y);
            PlaceBox(this.txtPlcBAlarmReg, y);
            this.btnPlcBOn.Left = left + labelW + boxW + 10;
            this.btnPlcBOn.Top = y;
            this.btnPlcBOn.Width = 50;
            this.btnPlcBOn.Text = "置1";
            this.btnPlcBOn.Click += new System.EventHandler(this.btnPlcBOn_Click);

            this.btnPlcBOff.Left = this.btnPlcBOn.Left + 55;
            this.btnPlcBOff.Top = y;
            this.btnPlcBOff.Width = 50;
            this.btnPlcBOff.Text = "置0";
            this.btnPlcBOff.Click += new System.EventHandler(this.btnPlcBOff_Click);
            y += 45;

            // OK/Cancel
            this.btnOk.Left = left + labelW;
            this.btnOk.Top = y;
            this.btnOk.Width = 100;
            this.btnOk.Text = "确定";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

            this.btnCancel.Left = this.btnOk.Left + 110;
            this.btnCancel.Top = y;
            this.btnCancel.Width = 100;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // Form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(520, y + 60);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(lblPlcAIp);
            this.Controls.Add(this.txtPlcAIp);
            this.Controls.Add(this.btnTestPlcA);
            this.Controls.Add(lblPlcBIp);
            this.Controls.Add(this.txtPlcBIp);
            this.Controls.Add(this.btnTestPlcB);
            this.Controls.Add(lblSlave);
            this.Controls.Add(this.txtSlaveId);
            this.Controls.Add(lblAReg);
            this.Controls.Add(this.txtPlcAAlarmReg);
            this.Controls.Add(this.btnPlcAOn);
            this.Controls.Add(this.btnPlcAOff);
            this.Controls.Add(lblBReg);
            this.Controls.Add(this.txtPlcBAlarmReg);
            this.Controls.Add(this.btnPlcBOn);
            this.Controls.Add(this.btnPlcBOff);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PLC 参数设置";

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
