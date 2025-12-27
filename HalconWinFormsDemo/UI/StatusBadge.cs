using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HalconWinFormsDemo.UI
{
    /// <summary>
    /// Minimal per-view status badge: running dot (red/green) + FPS text.
    /// Lightweight (no bitmap buffers). Intended to overlay on a view panel (sibling of HWindowControl).
    /// </summary>
    public sealed class StatusBadge : Control
    {
        private bool _isMapped;
        private bool _isRunning;
        private double _fps;

        public bool IsMapped
        {
            get => _isMapped;
            set { _isMapped = value; Invalidate(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; Invalidate(); }
        }

        public double Fps
        {
            get => _fps;
            set { _fps = value; Invalidate(); }
        }

        public StatusBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);

            Size = new Size(74, 18);
            IsMapped = false;
            IsRunning = false;
            Fps = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Background capsule
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect(rect, 9))
            using (var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            using (var border = new Pen(Color.FromArgb(180, 40, 40, 40), 1f))
            {
                e.Graphics.FillPath(bg, path);
                e.Graphics.DrawPath(border, path);
            }

            // Dot
            Color dotColor;
            if (!IsMapped) dotColor = Color.DimGray;
            else dotColor = IsRunning ? Color.LimeGreen : Color.Red;

            var dotRect = new Rectangle(5, 5, 8, 8);
            using (var b = new SolidBrush(dotColor))
            using (var p = new Pen(Color.Black, 1f))
            {
                e.Graphics.FillEllipse(b, dotRect);
                e.Graphics.DrawEllipse(p, dotRect);
            }

            // Text
            string text;
            if (!IsMapped) text = "--";
            else
            {
                var fpsInt = (int)Math.Round(Math.Max(0, Fps));
                text = fpsInt <= 0 ? "0 FPS" : $"{fpsInt} FPS";
            }

            var textRect = new Rectangle(16, 2, Width - 18, Height - 4);
            TextRenderer.DrawText(e.Graphics, text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
