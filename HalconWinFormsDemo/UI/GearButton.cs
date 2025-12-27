using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HalconWinFormsDemo.UI
{
    /// <summary>
    /// Minimal overlay gear button, visually aligned with StatusBadge.
    /// </summary>
    public sealed class GearButton : Control
    {
        private bool _hover;
        private bool _pressed;

        public GearButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI Symbol", 9f, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(26, 18);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_pressed && e.Button == MouseButtons.Left)
            {
                _pressed = false;
                Invalidate();
                if (ClientRectangle.Contains(e.Location))
                    OnClick(EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect(rect, 9))
            {
                int alpha = _pressed ? 200 : (_hover ? 180 : 160);
                using (var bg = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                using (var border = new Pen(Color.FromArgb(180, 40, 40, 40), 1f))
                {
                    e.Graphics.FillPath(bg, path);
                    e.Graphics.DrawPath(border, path);
                }
            }

            // Gear glyph
            var glyph = "⚙";
            var textRect = new Rectangle(0, -1, Width, Height + 2);
            TextRenderer.DrawText(e.Graphics, glyph, Font, textRect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
