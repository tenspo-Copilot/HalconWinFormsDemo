using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HalconWinFormsDemo.Diagnostics
{
    /// <summary>
    /// Startup self-check report window.
    /// Code-only form to avoid Designer issues.
    /// </summary>
    public sealed class SelfCheckForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnCopy = new Button();
        private readonly Button _btnClose = new Button();
        private readonly Label _lblSummary = new Label();

        public SelfCheckForm(IReadOnlyList<SelfCheckItem> items)
        {
            Text = "运行环境自检报告";
            StartPosition = FormStartPosition.CenterParent;
            Width = 980;
            Height = 520;
            MinimizeBox = false;
            MaximizeBox = true;

            _lblSummary.Dock = DockStyle.Top;
            _lblSummary.Height = 44;
            _lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            _lblSummary.Padding = new Padding(12, 8, 12, 8);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.RowHeadersVisible = false;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "检查项", DataPropertyName = "Name", FillWeight = 22 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "结果", DataPropertyName = "Result", FillWeight = 10 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "详情", DataPropertyName = "Detail", FillWeight = 34 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "建议", DataPropertyName = "Suggestion", FillWeight = 34 });

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 52 };
            _btnCopy.Text = "复制报告";
            _btnCopy.Width = 120;
            _btnCopy.Height = 30;
            _btnCopy.Left = 12;
            _btnCopy.Top = 11;
            _btnCopy.Click += (_, __) => CopyReport(items);

            _btnClose.Text = "关闭";
            _btnClose.Width = 120;
            _btnClose.Height = 30;
            _btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _btnClose.Left = panelBottom.Width - _btnClose.Width - 12;
            _btnClose.Top = 11;
            _btnClose.Click += (_, __) => Close();
            panelBottom.Resize += (_, __) => { _btnClose.Left = panelBottom.Width - _btnClose.Width - 12; };

            panelBottom.Controls.Add(_btnCopy);
            panelBottom.Controls.Add(_btnClose);

            Controls.Add(_grid);
            Controls.Add(panelBottom);
            Controls.Add(_lblSummary);

            Bind(items);
        }

        private void Bind(IReadOnlyList<SelfCheckItem> items)
        {
            var rows = items.Select(x => new
            {
                x.Name,
                Result = x.Passed ? "通过" : "失败",
                x.Detail,
                x.Suggestion
            }).ToList();

            _grid.DataSource = rows;

            // Highlight failed rows
            foreach (DataGridViewRow r in _grid.Rows)
            {
                var result = (r.Cells[1].Value ?? "").ToString();
                if (string.Equals(result, "失败", StringComparison.OrdinalIgnoreCase))
                    r.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
            }

            int fail = items.Count(i => !i.Passed);
            _lblSummary.Text = fail == 0
                ? "自检结果：全部通过。建议切换到 REAL 生产模式前，确认 PLC 与相机连接均为 ONLINE。"
                : $"自检结果：发现 {fail} 项失败。请根据“建议”完成安装/配置后重试（可在程序中再次打开自检报告）。";
        }

        private void CopyReport(IReadOnlyList<SelfCheckItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("运行环境自检报告");
            sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('-', 50));

            foreach (var i in items)
            {
                sb.AppendLine($"[{(i.Passed ? "通过" : "失败")}] {i.Name}");
                if (!string.IsNullOrWhiteSpace(i.Detail)) sb.AppendLine($"  详情：{i.Detail}");
                if (!string.IsNullOrWhiteSpace(i.Suggestion)) sb.AppendLine($"  建议：{i.Suggestion}");
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("自检报告已复制到剪贴板。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
