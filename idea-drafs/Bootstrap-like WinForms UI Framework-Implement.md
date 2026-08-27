# Bootstrap-like WinForms UI Framework - Small Implement

> Tôi dùng C#, WinForms. Tôi muốn app có giao diện đẹp một chút thì làm skin, theme như thế nào? Ví dụ dải màu giống với bootstrap 5 css.

Có. Với WinForms/C#, nếu muốn giao diện hiện đại kiểu Bootstrap 5, mình khuyên không nên “skin” từng control một cách cứng nhắc, mà xây một Theme system riêng: định nghĩa màu, font, radius, spacing… rồi các control dùng chung theme đó.

Ví dụ palette Bootstrap 5 có thể map như sau:

public static class AppTheme
{
    // Bootstrap 5 colors
    public static Color Primary    => Color.FromArgb(13, 110, 253);  // #0d6efd
    public static Color Secondary  => Color.FromArgb(108, 117, 125); // #6c757d
    public static Color Success    => Color.FromArgb(25, 135, 84);   // #198754
    public static Color Danger     => Color.FromArgb(220, 53, 69);   // #dc3545
    public static Color Warning    => Color.FromArgb(255, 193, 7);   // #ffc107
    public static Color Info       => Color.FromArgb(13, 202, 240);  // #0dcaf0

    public static Color Light      => Color.FromArgb(248, 249, 250); // #f8f9fa
    public static Color Dark       => Color.FromArgb(33, 37, 41);    // #212529
    public static Color White      => Color.White;

    // UI
    public static Color Background => Color.FromArgb(248, 249, 250);
    public static Color Surface    => Color.White;
    public static Color Border     => Color.FromArgb(222, 226, 230);
    public static Color Text       => Color.FromArgb(33, 37, 41);
    public static Color MutedText  => Color.FromArgb(108, 117, 125);
}


Sau đó tạo các helper để style control:

public static class ThemeHelper
{
    public static void StyleButton(Button button, Color color)
    {
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;

        button.Font = new Font(
            "Segoe UI",
            9.5f,
            FontStyle.Regular
        );
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = Color.White;
        textBox.ForeColor = AppTheme.Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;

        textBox.Font = new Font(
            "Segoe UI",
            10f
        );
    }

    public static void StyleLabel(Label label)
    {
        label.ForeColor = AppTheme.Text;
        label.Font = new Font(
            "Segoe UI",
            9.5f
        );
    }
}


Dùng:

private void MainForm_Load(object sender, EventArgs e)
{
    BackColor = AppTheme.Background;

    ThemeHelper.StyleButton(btnSave, AppTheme.Primary);
    ThemeHelper.StyleButton(btnDelete, AppTheme.Danger);
    ThemeHelper.StyleButton(btnSuccess, AppTheme.Success);

    ThemeHelper.StyleTextBox(txtName);

    ThemeHelper.StyleLabel(lblName);
}

Nhưng có một vấn đề với WinForms

WinForms mặc định khá “cổ điển”. Nếu chỉ đổi BackColor, ForeColor, FlatStyle thì chưa đạt được cảm giác Bootstrap 5.

Ví dụ Bootstrap có:

border-radius
shadow
hover/pressed state
padding
btn-primary, btn-success, btn-danger
card
navbar
badge
alert
modern textbox
sidebar
dropdown
modal

Trong WinForms, mình thường làm một lớp control riêng, ví dụ:

Controls/
    BootstrapButton.cs
    BootstrapTextBox.cs
    BootstrapPanel.cs
    BootstrapCard.cs
    BootstrapBadge.cs

Theme/
    AppTheme.cs
    ThemeHelper.cs


Ví dụ BootstrapButton:

public class BootstrapButton : Button
{
    public Color ThemeColor { get; set; } = AppTheme.Primary;

    public BootstrapButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;

        BackColor = ThemeColor;
        ForeColor = Color.White;

        Font = new Font("Segoe UI", 9.5f);
        Cursor = Cursors.Hand;

        Padding = new Padding(14, 6, 14, 6);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);

        BackColor = Darken(ThemeColor, 0.1f);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        BackColor = ThemeColor;
    }

    private static Color Darken(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            (int)(color.R * (1 - amount)),
            (int)(color.G * (1 - amount)),
            (int)(color.B * (1 - amount))
        );
    }
}


Sau đó trong Designer có thể dùng:

var btn = new BootstrapButton
{
    Text = "Lưu",
    ThemeColor = AppTheme.Primary
};

Nếu muốn đẹp hơn nữa

Mình sẽ đi theo kiến trúc kiểu này:

                    ┌─────────────────┐
                    │    AppTheme     │
                    │─────────────────│
                    │ Primary         │
                    │ Success         │
                    │ Danger          │
                    │ Background      │
                    │ Surface         │
                    │ Border          │
                    │ Text            │
                    │ Font            │
                    │ Radius          │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
       BootstrapButton  BootstrapTextBox  BootstrapCard
              │              │              │
              └──────────────┼──────────────┘
                             ▼
                       WinForms UI


Điểm hay là sau này muốn Dark Mode chỉ cần thay theme:

AppTheme.Current = AppTheme.Dark;


thay vì phải sửa từng Form.

Nếu bạn muốn giao diện rất giống Bootstrap 5, mình cũng có thể thiết kế cho bạn một bộ AppTheme + BootstrapButton + BootstrapTextBox + Card + Sidebar + DataGridView hoàn chỉnh cho WinForms, bao gồm hover, border-radius, shadow, dark/light theme và palette Bootstrap 5.

----

> Như phân tích trên, tạo giúp tôi bộ AppTheme + BootstrapButton + BootstrapTextBox + Card + Sidebar + DataGridView hoàn chỉnh cho WinForms, bao gồm hover, border-radius, shadow, dark/light theme và palette Bootstrap 5.

Được. Mình sẽ thiết kế theo hướng không phụ thuộc thư viện UI bên thứ ba, dùng WinForms thuần, nhưng tổ chức thành một mini design system giống Bootstrap 5.

Bộ này nên có:

AppTheme: toàn bộ màu sắc, font, radius, shadow và light/dark theme.
BootstrapButton: Primary/Secondary/Success/Danger/Warning/Info/Light/Dark, hover/pressed.
BootstrapTextBox: textbox hiện đại, border + focus color.
BootstrapCard: panel bo góc + shadow.
BootstrapSidebar: sidebar trái, item hover/selected.
BootstrapDataGridView: DataGridView hiện đại, header/row selection/gridline.
ThemeManager: đổi Light/Dark và tự áp theme cho toàn bộ control.
Hỗ trợ Segoe UI, DPI scaling và resize cơ bản.

Một điểm quan trọng: WinForms Panel, Button, TextBox, DataGridView mặc định không hỗ trợ border-radius/shadow thực sự, nên mình sẽ custom painting (OnPaint) thay vì chỉ set BackColor.

1. Cấu trúc project
YourApp/
│
├── Theme/
│   ├── AppTheme.cs
│   ├── ThemeManager.cs
│   └── ThemeColors.cs
│
├── Controls/
│   ├── BootstrapButton.cs
│   ├── BootstrapTextBox.cs
│   ├── BootstrapCard.cs
│   ├── BootstrapSidebar.cs
│   └── BootstrapDataGridView.cs
│
└── MainForm.cs

2. ThemeColors.cs

Đầu tiên định nghĩa palette Bootstrap 5.

using System.Drawing;

namespace YourApp.Theme
{
    public sealed class ThemeColors
    {
        // Bootstrap 5
        public Color Primary   { get; init; }
        public Color Secondary { get; init; }
        public Color Success   { get; init; }
        public Color Danger    { get; init; }
        public Color Warning   { get; init; }
        public Color Info      { get; init; }
        public Color Light     { get; init; }
        public Color Dark      { get; init; }

        // Application colors
        public Color Background { get; init; }
        public Color Surface { get; init; }
        public Color SurfaceAlt { get; init; }

        public Color Border { get; init; }

        public Color Text { get; init; }
        public Color TextSecondary { get; init; }
        public Color TextMuted { get; init; }

        public Color SidebarBackground { get; init; }
        public Color SidebarText { get; init; }
        public Color SidebarHover { get; init; }
        public Color SidebarSelected { get; init; }

        public Color GridHeader { get; init; }
        public Color GridRow { get; init; }
        public Color GridRowAlternate { get; init; }
        public Color GridSelection { get; init; }

        public Color Focus { get; init; }
    }
}

3. AppTheme.cs

Đây là phần quan trọng nhất.

using System.Drawing;

namespace YourApp.Theme
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public static class AppTheme
    {
        public static ThemeMode Mode { get; private set; }
            = ThemeMode.Light;

        public static ThemeColors Colors =>
            Mode == ThemeMode.Dark
                ? Dark
                : Light;

        public static readonly ThemeColors Light =
            new ThemeColors
            {
                // Bootstrap 5
                Primary   = Color.FromArgb(13, 110, 253),
                Secondary = Color.FromArgb(108, 117, 125),
                Success   = Color.FromArgb(25, 135, 84),
                Danger    = Color.FromArgb(220, 53, 69),
                Warning   = Color.FromArgb(255, 193, 7),
                Info      = Color.FromArgb(13, 202, 240),
                Light     = Color.FromArgb(248, 249, 250),
                Dark      = Color.FromArgb(33, 37, 41),

                // Application
                Background = Color.FromArgb(248, 249, 250),
                Surface = Color.White,
                SurfaceAlt = Color.FromArgb(248, 249, 250),

                Border = Color.FromArgb(222, 226, 230),

                Text = Color.FromArgb(33, 37, 41),
                TextSecondary = Color.FromArgb(73, 80, 87),
                TextMuted = Color.FromArgb(108, 117, 125),

                SidebarBackground = Color.White,
                SidebarText = Color.FromArgb(73, 80, 87),
                SidebarHover = Color.FromArgb(233, 236, 239),
                SidebarSelected = Color.FromArgb(13, 110, 253),

                GridHeader = Color.FromArgb(248, 249, 250),
                GridRow = Color.White,
                GridRowAlternate = Color.FromArgb(248, 249, 250),
                GridSelection = Color.FromArgb(207, 226, 255),

                Focus = Color.FromArgb(13, 110, 253)
            };


        public static readonly ThemeColors Dark =
            new ThemeColors
            {
                // Bootstrap 5
                Primary   = Color.FromArgb(13, 110, 253),
                Secondary = Color.FromArgb(108, 117, 125),
                Success   = Color.FromArgb(25, 135, 84),
                Danger    = Color.FromArgb(220, 53, 69),
                Warning   = Color.FromArgb(255, 193, 7),
                Info      = Color.FromArgb(13, 202, 240),
                Light     = Color.FromArgb(248, 249, 250),
                Dark      = Color.FromArgb(33, 37, 41),

                // Application
                Background = Color.FromArgb(24, 27, 31),
                Surface = Color.FromArgb(33, 37, 41),
                SurfaceAlt = Color.FromArgb(43, 48, 53),

                Border = Color.FromArgb(73, 80, 87),

                Text = Color.FromArgb(248, 249, 250),
                TextSecondary = Color.FromArgb(206, 212, 218),
                TextMuted = Color.FromArgb(173, 181, 189),

                SidebarBackground = Color.FromArgb(33, 37, 41),
                SidebarText = Color.FromArgb(206, 212, 218),
                SidebarHover = Color.FromArgb(52, 58, 64),
                SidebarSelected = Color.FromArgb(13, 110, 253),

                GridHeader = Color.FromArgb(43, 48, 53),
                GridRow = Color.FromArgb(33, 37, 41),
                GridRowAlternate = Color.FromArgb(43, 48, 53),
                GridSelection = Color.FromArgb(25, 85, 150),

                Focus = Color.FromArgb(13, 110, 253)
            };


        public static void SetMode(ThemeMode mode)
        {
            Mode = mode;
        }

        // Typography
        public static Font FontRegular =>
            new Font("Segoe UI", 9.5f, FontStyle.Regular);

        public static Font FontMedium =>
            new Font("Segoe UI", 9.5f, FontStyle.Bold);

        public static Font FontTitle =>
            new Font("Segoe UI", 18f, FontStyle.Bold);

        public static Font FontSubtitle =>
            new Font("Segoe UI", 12f, FontStyle.Regular);

        // Bootstrap-ish radius
        public const int BorderRadius = 6;
        public const int SmallRadius = 4;
        public const int LargeRadius = 10;

        // Spacing
        public const int SpacingXs = 4;
        public const int SpacingSm = 8;
        public const int SpacingMd = 12;
        public const int SpacingLg = 16;
        public const int SpacingXl = 24;
    }
}

4. ThemeManager.cs

Ta muốn một câu:

ThemeManager.Apply(this);


là toàn bộ Form được đổi theme.

using System.Drawing;
using System.Windows.Forms;

namespace YourApp.Theme
{
    public static class ThemeManager
    {
        public static void Apply(Control root)
        {
            ApplyControl(root);

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }

            root.Invalidate();
        }

        private static void ApplyControl(Control control)
        {
            var colors = AppTheme.Colors;

            switch (control)
            {
                case Form form:
                    form.BackColor = colors.Background;
                    form.ForeColor = colors.Text;
                    break;

                case BootstrapButton button:
                    button.RefreshTheme();
                    break;

                case BootstrapTextBox textBox:
                    textBox.RefreshTheme();
                    break;

                case BootstrapCard card:
                    card.RefreshTheme();
                    break;

                case BootstrapSidebar sidebar:
                    sidebar.RefreshTheme();
                    break;

                case BootstrapDataGridView grid:
                    grid.RefreshTheme();
                    break;

                case Label label:
                    label.ForeColor = colors.Text;
                    break;

                case Panel panel:
                    panel.BackColor = colors.Background;
                    break;
            }
        }

        public static void SetTheme(
            Control root,
            ThemeMode mode)
        {
            AppTheme.SetMode(mode);
            Apply(root);
        }

        public static void Toggle(
            Control root)
        {
            var mode =
                AppTheme.Mode == ThemeMode.Light
                    ? ThemeMode.Dark
                    : ThemeMode.Light;

            SetTheme(root, mode);
        }
    }
}

5. BootstrapButton.cs

Đây là button có:

radius
hover
pressed
disabled
shadow nhẹ
Bootstrap colors.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public enum BootstrapButtonType
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        Light,
        Dark
    }

    public class BootstrapButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public BootstrapButtonType ButtonType { get; set; }
            = BootstrapButtonType.Primary;

        public int BorderRadius { get; set; }
            = AppTheme.BorderRadius;

        public BootstrapButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;

            Font = AppTheme.FontRegular;

            Cursor = Cursors.Hand;

            Height = 38;

            Padding = new Padding(16, 6, 16, 6);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(
            MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(
            MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var rect = new Rectangle(
                0,
                0,
                Width - 1,
                Height - 1);

            var color = GetColor();

            if (!Enabled)
                color = Color.FromArgb(
                    160,
                    color);

            if (_hover && Enabled)
                color = Lighten(color, 0.08f);

            if (_pressed && Enabled)
                color = Darken(color, 0.08f);

            using var path =
                RoundedRectangle(
                    rect,
                    BorderRadius);

            using var brush =
                new SolidBrush(color);

            g.FillPath(brush, path);

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                GetTextColor(),
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }

        private Color GetColor()
        {
            var c = AppTheme.Colors;

            return ButtonType switch
            {
                BootstrapButtonType.Primary =>
                    c.Primary,

                BootstrapButtonType.Secondary =>
                    c.Secondary,

                BootstrapButtonType.Success =>
                    c.Success,

                BootstrapButtonType.Danger =>
                    c.Danger,

                BootstrapButtonType.Warning =>
                    c.Warning,

                BootstrapButtonType.Info =>
                    c.Info,

                BootstrapButtonType.Light =>
                    c.Light,

                BootstrapButtonType.Dark =>
                    c.Dark,

                _ => c.Primary
            };
        }

        private Color GetTextColor()
        {
            return ButtonType switch
            {
                BootstrapButtonType.Warning =>
                    Color.FromArgb(33, 37, 41),

                BootstrapButtonType.Info =>
                    Color.FromArgb(33, 37, 41),

                BootstrapButtonType.Light =>
                    AppTheme.Colors.Text,

                _ => Color.White
            };
        }

        public void RefreshTheme()
        {
            Invalidate();
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path = new GraphicsPath();

            int d = radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }

        private static Color Lighten(
            Color color,
            float amount)
        {
            return Color.FromArgb(
                color.A,
                Math.Min(255,
                    (int)(color.R +
                    255 * amount)),
                Math.Min(255,
                    (int)(color.G +
                    255 * amount)),
                Math.Min(255,
                    (int)(color.B +
                    255 * amount)));
        }

        private static Color Darken(
            Color color,
            float amount)
        {
            return Color.FromArgb(
                color.A,
                Math.Max(0,
                    (int)(color.R *
                    (1 - amount))),
                Math.Max(0,
                    (int)(color.G *
                    (1 - amount))),
                Math.Max(0,
                    (int)(color.B *
                    (1 - amount))));
        }
    }
}


Lưu ý namespace: file trên dùng YourApp.Theme và YourApp.Controls, bạn đổi thành namespace project thực tế.

6. BootstrapTextBox.cs

TextBox WinForms khó custom border trực tiếp vì native control. Một cách sạch là tạo UserControl bao TextBox.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapTextBox : UserControl
    {
        private readonly TextBox _textBox;

        private bool _focused;

        public BootstrapTextBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            Height = 40;

            BackColor = Color.Transparent;

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = AppTheme.FontRegular,
                Location = new Point(12, 9),
                Width = Width - 24,
                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right
            };

            _textBox.Enter += (_, _) =>
            {
                _focused = true;
                Invalidate();
            };

            _textBox.Leave += (_, _) =>
            {
                _focused = false;
                Invalidate();
            };

            _textBox.TextChanged += (_, _) =>
            {
                OnTextChanged(EventArgs.Empty);
            };

            Controls.Add(_textBox);
        }

        public override string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public bool ReadOnly
        {
            get => _textBox.ReadOnly;
            set => _textBox.ReadOnly = value;
        }

        public char PasswordChar
        {
            get => _textBox.PasswordChar;
            set => _textBox.PasswordChar = value;
        }

        public TextBox InnerTextBox => _textBox;

        protected override void OnPaint(
            PaintEventArgs e)
        {
            var g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var rect = new Rectangle(
                0,
                0,
                Width - 1,
                Height - 1);

            var colors = AppTheme.Colors;

            using var path =
                RoundedRectangle(
                    rect,
                    AppTheme.BorderRadius);

            using var brush =
                new SolidBrush(colors.Surface);

            g.FillPath(brush, path);

            var borderColor =
                _focused
                    ? colors.Focus
                    : colors.Border;

            using var pen =
                new Pen(
                    borderColor,
                    _focused ? 2 : 1);

            g.DrawPath(pen, path);
        }

        public void RefreshTheme()
        {
            var c = AppTheme.Colors;

            _textBox.BackColor = c.Surface;
            _textBox.ForeColor = c.Text;

            Invalidate();
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path = new GraphicsPath();

            int d = radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}

7. BootstrapCard.cs

Card sẽ giống Bootstrap:

┌──────────────────────────────┐
│ Title                        │
│                              │
│ Content                      │
│                              │
│                 [ Button ]    │
└──────────────────────────────┘


Có shadow.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapCard : Panel
    {
        public int BorderRadius { get; set; }
            = AppTheme.LargeRadius;

        public bool ShowShadow { get; set; } = true;

        public BootstrapCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = AppTheme.Colors.Surface;

            Padding = new Padding(20);
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            var g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var rect = new Rectangle(
                2,
                2,
                Width - 5,
                Height - 5);

            if (ShowShadow)
            {
                using var shadow =
                    new SolidBrush(
                        Color.FromArgb(
                            AppTheme.Mode ==
                            ThemeMode.Dark
                                ? 70
                                : 25,
                            Color.Black));

                var shadowRect = rect;

                shadowRect.Offset(2, 3);

                using var shadowPath =
                    RoundedRectangle(
                        shadowRect,
                        BorderRadius);

                g.FillPath(
                    shadow,
                    shadowPath);
            }

            using var path =
                RoundedRectangle(
                    rect,
                    BorderRadius);

            using var brush =
                new SolidBrush(
                    AppTheme.Colors.Surface);

            g.FillPath(
                brush,
                path);

            using var pen =
                new Pen(
                    AppTheme.Colors.Border);

            g.DrawPath(
                pen,
                path);

            base.OnPaint(e);
        }

        public void RefreshTheme()
        {
            BackColor = AppTheme.Colors.Surface;
            Invalidate();
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path = new GraphicsPath();

            int d = radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}

8. BootstrapSidebar.cs

Sidebar nên là control có menu item.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapSidebar : Panel
    {
        private readonly List<SidebarItem> _items = new();

        private int _selectedIndex = -1;

        public event EventHandler<int>? ItemClicked;

        public BootstrapSidebar()
        {
            Width = 240;

            BackColor =
                AppTheme.Colors.SidebarBackground;

            AutoScroll = true;
        }

        public int AddItem(
            string text,
            Image? icon = null)
        {
            var index = _items.Count;

            var item = new SidebarItem
            {
                Text = text,
                Image = icon,
                Width = Width - 1,
                Height = 44,
                Index = index
            };

            item.MouseEnter += (_, _) =>
            {
                item.IsHovered = true;
                item.Invalidate();
            };

            item.MouseLeave += (_, _) =>
            {
                item.IsHovered = false;
                item.Invalidate();
            };

            item.Click += (_, _) =>
            {
                Select(index);

                ItemClicked?.Invoke(
                    this,
                    index);
            };

            _items.Add(item);

            Controls.Add(item);

            LayoutItems();

            return index;
        }

        public void Select(int index)
        {
            if (index < 0 ||
                index >= _items.Count)
                return;

            _selectedIndex = index;

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].IsSelected =
                    i == index;

                _items[i].Invalidate();
            }
        }

        private void LayoutItems()
        {
            int y = 10;

            foreach (var item in _items)
            {
                item.Location =
                    new Point(0, y);

                item.Width =
                    ClientSize.Width - 5;

                y += item.Height + 4;
            }
        }

        protected override void OnResize(
            EventArgs eventargs)
        {
            base.OnResize(eventargs);

            LayoutItems();
        }

        public void RefreshTheme()
        {
            BackColor =
                AppTheme.Colors.SidebarBackground;

            foreach (var item in _items)
                item.Invalidate();

            Invalidate();
        }
    }


    internal class SidebarItem : Control
    {
        public int Index { get; set; }

        public bool IsHovered { get; set; }

        public bool IsSelected { get; set; }

        public Image? Image { get; set; }

        public SidebarItem()
        {
            Cursor = Cursors.Hand;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            var g = e.Graphics;

            var c = AppTheme.Colors;

            Color background =
                IsSelected
                    ? c.SidebarSelected
                    : IsHovered
                        ? c.SidebarHover
                        : c.SidebarBackground;

            using var brush =
                new SolidBrush(background);

            g.FillRectangle(
                brush,
                ClientRectangle);

            var textColor =
                IsSelected
                    ? Color.White
                    : c.SidebarText;

            int x = 18;

            if (Image != null)
            {
                g.DrawImage(
                    Image,
                    new Rectangle(
                        18,
                        12,
                        20,
                        20));

                x = 50;
            }

            TextRenderer.DrawText(
                g,
                Text,
                AppTheme.FontRegular,
                new Rectangle(
                    x,
                    0,
                    Width - x - 10,
                    Height),
                textColor,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }
    }
}

9. BootstrapDataGridView.cs

Đây là phần mình khuyên custom mạnh hơn một chút vì DataGridView mặc định khá xấu.

using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapDataGridView : DataGridView
    {
        public BootstrapDataGridView()
        {
            BorderStyle = BorderStyle.None;

            BackgroundColor =
                AppTheme.Colors.Surface;

            GridColor =
                AppTheme.Colors.Border;

            CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal;

            EnableHeadersVisualStyles = false;

            RowHeadersVisible = false;

            AllowUserToAddRows = false;

            AllowUserToResizeRows = false;

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            MultiSelect = false;

            AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None;

            RowTemplate.Height = 42;

            Font = AppTheme.FontRegular;

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            var c = AppTheme.Colors;

            BackgroundColor = c.Surface;

            GridColor = c.Border;

            DefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = c.GridRow,
                    ForeColor = c.Text,

                    SelectionBackColor =
                        c.GridSelection,

                    SelectionForeColor =
                        c.Text,

                    Font =
                        AppTheme.FontRegular,

                    Padding =
                        new Padding(10, 0, 10, 0),

                    NullValue = ""
                };

            AlternatingRowsDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor =
                        c.GridRowAlternate,

                    ForeColor =
                        c.Text
                };

            ColumnHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor =
                        c.GridHeader,

                    ForeColor =
                        c.Text,

                    Font =
                        AppTheme.FontMedium,

                    Alignment =
                        DataGridViewContentAlignment.MiddleLeft,

                    Padding =
                        new Padding(10, 0, 10, 0)
                };

            RowHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = c.GridHeader,
                    ForeColor = c.Text
                };
        }

        public void RefreshTheme()
        {
            ApplyTheme();

            Invalidate();
        }
    }
}

10. MainForm

Bây giờ có thể dựng một dashboard khá đẹp.

Ví dụ layout:

┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  SIDEBAR        Dashboard                                   │
│  ─────────      ─────────────────────────────────────────   │
│                                                             │
│  🏠 Dashboard   ┌────────┐ ┌────────┐ ┌────────┐           │
│  👤 Users       │ Users  │ │ Sales  │ │ Orders │           │
│  📦 Products    │  1,250 │ │ $52,300│ │   328  │           │
│  📊 Reports     └────────┘ └────────┘ └────────┘           │
│                                                             │
│                 Recent orders                               │
│                 ┌─────────────────────────────────────────┐ │
│                 │ ID │ Customer │ Amount │ Status          │ │
│                 ├─────────────────────────────────────────┤ │
│                 │ 01 │ John     │ $120   │ Completed       │ │
│                 │ 02 │ David    │ $350   │ Pending         │ │
│                 └─────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘


Code:

using System;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Controls;
using YourApp.Theme;

namespace YourApp
{
    public partial class MainForm : Form
    {
        private BootstrapSidebar _sidebar = null!;
        private Panel _content = null!;

        public MainForm()
        {
            InitializeComponent();

            BuildUI();

            ThemeManager.Apply(this);
        }

        private void BuildUI()
        {
            Text = "Bootstrap WinForms";
            StartPosition =
                FormStartPosition.CenterScreen;

            MinimumSize =
                new Size(1000, 650);

            BackColor =
                AppTheme.Colors.Background;

            // Sidebar
            _sidebar =
                new BootstrapSidebar
                {
                    Dock = DockStyle.Left,
                    Width = 240
                };

            _sidebar.AddItem("Dashboard");
            _sidebar.AddItem("Users");
            _sidebar.AddItem("Products");
            _sidebar.AddItem("Orders");
            _sidebar.AddItem("Reports");

            _sidebar.Select(0);

            Controls.Add(_sidebar);

            // Content
            _content = new Panel
            {
                Dock = DockStyle.Fill,

                Padding =
                    new Padding(30),

                BackColor =
                    AppTheme.Colors.Background
            };

            Controls.Add(_content);

            BuildDashboard();
        }

        private void BuildDashboard()
        {
            var title =
                new Label
                {
                    Text = "Dashboard",

                    Font =
                        AppTheme.FontTitle,

                    ForeColor =
                        AppTheme.Colors.Text,

                    AutoSize = true,

                    Location =
                        new Point(30, 25)
                };

            _content.Controls.Add(title);


            var subtitle =
                new Label
                {
                    Text = "Overview of your application",

                    Font =
                        AppTheme.FontSubtitle,

                    ForeColor =
                        AppTheme.Colors.TextMuted,

                    AutoSize = true,

                    Location =
                        new Point(32, 60)
                };

            _content.Controls.Add(subtitle);


            // Cards
            var card1 =
                CreateStatCard(
                    "Users",
                    "1,250",
                    AppTheme.Colors.Primary);

            card1.Location =
                new Point(30, 105);

            _content.Controls.Add(card1);


            var card2 =
                CreateStatCard(
                    "Sales",
                    "$52,300",
                    AppTheme.Colors.Success);

            card2.Location =
                new Point(260, 105);

            _content.Controls.Add(card2);


            var card3 =
                CreateStatCard(
                    "Orders",
                    "328",
                    AppTheme.Colors.Warning);

            card3.Location =
                new Point(490, 105);

            _content.Controls.Add(card3);


            // Grid
            var grid =
                new BootstrapDataGridView
                {
                    Location =
                        new Point(30, 270),

                    Size =
                        new Size(
                            750,
                            300),

                    Anchor =
                        AnchorStyles.Top |
                        AnchorStyles.Left |
                        AnchorStyles.Right |
                        AnchorStyles.Bottom
                };

            grid.Columns.Add(
                "Id",
                "ID");

            grid.Columns.Add(
                "Customer",
                "Customer");

            grid.Columns.Add(
                "Amount",
                "Amount");

            grid.Columns.Add(
                "Status",
                "Status");

            grid.Rows.Add(
                "001",
                "John Smith",
                "$120.00",
                "Completed");

            grid.Rows.Add(
                "002",
                "David Brown",
                "$350.00",
                "Pending");

            grid.Rows.Add(
                "003",
                "Sarah Wilson",
                "$89.00",
                "Completed");

            _content.Controls.Add(grid);
        }


        private BootstrapCard CreateStatCard(
            string title,
            string value,
            Color color)
        {
            var card =
                new BootstrapCard
                {
                    Size =
                        new Size(210, 130)
                };

            var titleLabel =
                new Label
                {
                    Text = title,

                    AutoSize = true,

                    Font =
                        AppTheme.FontRegular,

                    ForeColor =
                        AppTheme.Colors.TextMuted,

                    Location =
                        new Point(20, 20)
                };

            card.Controls.Add(titleLabel);


            var valueLabel =
                new Label
                {
                    Text = value,

                    AutoSize = true,

                    Font =
                        new Font(
                            "Segoe UI",
                            22,
                            FontStyle.Bold),

                    ForeColor = color,

                    Location =
                        new Point(20, 50)
                };

            card.Controls.Add(valueLabel);

            return card;
        }
    }
}

11. Dark mode

Ta chỉ cần một button:

private void btnTheme_Click(
    object sender,
    EventArgs e)
{
    ThemeManager.Toggle(this);
}


Hoặc:

ThemeManager.SetTheme(
    this,
    ThemeMode.Dark);


Light:

ThemeManager.SetTheme(
    this,
    ThemeMode.Light);


Điểm hay của cách này là control không cần biết application đang light hay dark. Nó chỉ đọc:

AppTheme.Colors

12. Palette Bootstrap 5

Bộ màu hiện tại tương ứng với Bootstrap 5:

Bootstrap	Hex
Primary	#0d6efd
Secondary	#6c757d
Success	#198754
Danger	#dc3545
Warning	#ffc107
Info	#0dcaf0
Light	#f8f9fa
Dark	#212529

Nhưng mình cố ý không dùng trực tiếp các màu Bootstrap cho background của application. Ví dụ:

Primary     → #0d6efd
Background  → #f8f9fa
Surface     → #ffffff
Border      → #dee2e6
Text        → #212529
Muted       → #6c757d


Kết quả nhìn sẽ giống Bootstrap 5 Admin Dashboard hơn là một app WinForms bị “tô màu Bootstrap”.

13. Một cải tiến mình rất khuyên làm

Nếu đây là project thực tế, mình sẽ không dừng ở bộ trên. Nên phát triển thành một mini Bootstrap UI framework cho WinForms:

BootstrapWinForms
│
├── Theme
│   ├── AppTheme
│   ├── ThemeColors
│   ├── ThemeManager
│   ├── Typography
│   └── Spacing
│
├── Buttons
│   ├── BootstrapButton
│   └── BootstrapIconButton
│
├── Inputs
│   ├── BootstrapTextBox
│   ├── BootstrapComboBox
│   ├── BootstrapNumericBox
│   └── BootstrapDatePicker
│
├── Containers
│   ├── BootstrapCard
│   ├── BootstrapPanel
│   └── BootstrapSidebar
│
├── Navigation
│   ├── BootstrapNavbar
│   ├── BootstrapMenu
│   └── BootstrapBreadcrumb
│
├── Data
│   ├── BootstrapDataGridView
│   └── BootstrapPagination
│
└── Feedback
    ├── BootstrapBadge
    ├── BootstrapAlert
    ├── BootstrapToast
    └── BootstrapDialog


Đặc biệt, có 3 thứ đáng làm tiếp nếu bạn muốn app đạt cảm giác như các app WinForms hiện đại:

Animation nhẹ cho hover/sidebar.
Icon system bằng SVG thay vì emoji/bitmap.
Designer support, để trong Visual Studio bạn kéo BootstrapButton, BootstrapCard vào Form và chỉnh ButtonType, BorderRadius, ThemeColor ngay trong Properties.

Nếu làm theo hướng này thì WinForms vẫn là WinForms, nhưng code UI sẽ khá gần tư duy Bootstrap:

var btn = new BootstrapButton
{
    Text = "Save",
    ButtonType = BootstrapButtonType.Primary
};

var danger = new BootstrapButton
{
    Text = "Delete",
    ButtonType = BootstrapButtonType.Danger
};

var card = new BootstrapCard
{
    Padding = new Padding(20),
    ShowShadow = true
};


Lưu ý nhỏ: code trên là nền tảng hoàn chỉnh về kiến trúc, nhưng nếu copy nguyên xi thì cần chỉnh namespace (YourApp) và using cho phù hợp project. Với BootstrapTextBox, BootstrapCard và BootstrapSidebar, nếu dùng Visual Studio Designer thì nên thêm DesignerSerializationVisibility/Designer classes ở bước tiếp theo để trải nghiệm kéo-thả ổn định hơn.

----

> Bạn có thể tạo giúp tôi control https://getbootstrap.com/docs/5.3/components/accordion/

Có. Và với WinForms, mình nghĩ nên làm BootstrapAccordion theo đúng tinh thần Bootstrap 5.3 chứ không chỉ là một Panel chứa các button. Bootstrap Accordion có header có thể click để collapse/expand, chevron biểu thị trạng thái, hỗ trợ nhiều item mở đồng thời hoặc chỉ một item mở, và có biến thể flush. Bootstrap cũng đã bổ sung tài liệu riêng cho Accordion trong các bản 5.3.x gần đây. 
B
Bootstrap Blog
+1

Mình đề xuất API WinForms như sau:

var accordion = new BootstrapAccordion
{
    AllowMultipleOpen = false,
    Flush = false
};

accordion.AddItem(
    "Thông tin tài khoản",
    new Label
    {
        Text = "Nguyễn Văn A",
        AutoSize = true
    });

accordion.AddItem(
    "Cài đặt",
    settingsPanel);

accordion.AddItem(
    "Thông báo",
    notificationPanel);


Kết quả:

┌──────────────────────────────────────────────┐
│ Thông tin tài khoản                       ˅ │
├──────────────────────────────────────────────┤
│ Nguyễn Văn A                                 │
└──────────────────────────────────────────────┘
┌──────────────────────────────────────────────┐
│ Cài đặt                                   > │
└──────────────────────────────────────────────┘
┌──────────────────────────────────────────────┐
│ Thông báo                                  > │
└──────────────────────────────────────────────┘


Khi click:

┌──────────────────────────────────────────────┐
│ Thông tin tài khoản                       ˄ │
├──────────────────────────────────────────────┤
│ Nguyễn Văn A                                 │
│                                              │
│ Email: xxx@example.com                       │
│ Phone: 0123456789                            │
└──────────────────────────────────────────────┘

1. BootstrapAccordion.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordion : Panel
    {
        private readonly List<BootstrapAccordionItem> _items = new();

        public IReadOnlyList<BootstrapAccordionItem> Items => _items;

        /// <summary>
        /// Cho phép mở nhiều item cùng lúc.
        /// false = chỉ một item được mở.
        /// </summary>
        public bool AllowMultipleOpen { get; set; } = false;

        /// <summary>
        /// Giống Bootstrap .accordion-flush.
        /// </summary>
        public bool Flush { get; set; } = false;

        public int ItemSpacing { get; set; } = 0;

        public BootstrapAccordion()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            AutoScroll = true;

            BackColor = AppTheme.Colors.Surface;
        }

        public BootstrapAccordionItem AddItem(
            string title,
            Control content,
            bool expanded = false)
        {
            var item = new BootstrapAccordionItem
            {
                Title = title,
                Content = content,
                Expanded = expanded,
                Flush = Flush
            };

            item.ExpandedChanged += Item_ExpandedChanged;

            _items.Add(item);

            Controls.Add(item);

            LayoutItems();

            return item;
        }

        public void RemoveItem(
            BootstrapAccordionItem item)
        {
            if (!_items.Contains(item))
                return;

            item.ExpandedChanged -= Item_ExpandedChanged;

            _items.Remove(item);

            Controls.Remove(item);

            item.Dispose();

            LayoutItems();
        }

        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.ExpandedChanged -=
                    Item_ExpandedChanged;

                item.Dispose();
            }

            _items.Clear();

            Controls.Clear();

            LayoutItems();
        }

        private void Item_ExpandedChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not BootstrapAccordionItem current)
                return;

            if (current.Expanded &&
                !AllowMultipleOpen)
            {
                foreach (var item in _items)
                {
                    if (item != current &&
                        item.Expanded)
                    {
                        item.Expanded = false;
                    }
                }
            }

            LayoutItems();

            Invalidate();
        }

        private void LayoutItems()
        {
            int y = 0;

            foreach (var item in _items)
            {
                item.Width =
                    Math.Max(
                        0,
                        ClientSize.Width -
                        (AutoScroll
                            ? SystemInformation.VerticalScrollBarWidth
                            : 0));

                item.Location =
                    new Point(0, y);

                item.PerformAccordionLayout();

                y += item.Height + ItemSpacing;
            }

            AutoScrollMinSize =
                new Size(0, y);
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            LayoutItems();
        }

        public void RefreshTheme()
        {
            BackColor =
                AppTheme.Colors.Surface;

            foreach (var item in _items)
            {
                item.Flush = Flush;
                item.RefreshTheme();
            }

            Invalidate();
        }
    }
}

2. BootstrapAccordionItem.cs

Đây là phần làm phần header, chevron, hover, border và animation.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordionItem : Panel
    {
        private const int HeaderHeight = 50;

        private bool _hover;
        private bool _expanded;

        private readonly Label _titleLabel;
        private readonly Label _chevronLabel;

        private Control? _content;

        public event EventHandler? ExpandedChanged;

        public string Title
        {
            get => _titleLabel.Text;
            set
            {
                _titleLabel.Text = value;
                Invalidate();
            }
        }

        public Control? Content
        {
            get => _content;
            set
            {
                if (_content != null)
                    Controls.Remove(_content);

                _content = value;

                if (_content != null)
                {
                    _content.Dock =
                        DockStyle.Fill;

                    Controls.Add(_content);

                    _content.BringToFront();
                }

                PerformAccordionLayout();
            }
        }

        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded == value)
                    return;

                _expanded = value;

                UpdateVisualState();

                ExpandedChanged?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        public bool Flush { get; set; }

        public Color AccentColor { get; set; }

        public int BorderRadius { get; set; }
            = AppTheme.BorderRadius;

        public BootstrapAccordionItem()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Height = HeaderHeight;

            BackColor =
                AppTheme.Colors.Surface;

            _titleLabel = new Label
            {
                AutoSize = false,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Font =
                    AppTheme.FontRegular,

                ForeColor =
                    AppTheme.Colors.Text,

                Location =
                    new Point(16, 0),

                Height =
                    HeaderHeight,

                Cursor =
                    Cursors.Hand
            };

            _chevronLabel = new Label
            {
                AutoSize = false,

                Text = "›",

                TextAlign =
                    ContentAlignment.MiddleCenter,

                Font =
                    new Font(
                        "Segoe UI",
                        16f,
                        FontStyle.Regular),

                ForeColor =
                    AppTheme.Colors.TextMuted,

                Width = 35,

                Height =
                    HeaderHeight,

                Cursor =
                    Cursors.Hand
            };

            Controls.Add(_titleLabel);
            Controls.Add(_chevronLabel);

            _titleLabel.Click += Header_Click;
            _chevronLabel.Click += Header_Click;

            _titleLabel.MouseEnter += Header_MouseEnter;
            _titleLabel.MouseLeave += Header_MouseLeave;

            _chevronLabel.MouseEnter += Header_MouseEnter;
            _chevronLabel.MouseLeave += Header_MouseLeave;

            MouseEnter += Header_MouseEnter;
            MouseLeave += Header_MouseLeave;

            UpdateVisualState();
        }

        private void Header_Click(
            object? sender,
            EventArgs e)
        {
            Expanded = !Expanded;
        }

        private void Header_MouseEnter(
            object? sender,
            EventArgs e)
        {
            _hover = true;

            Invalidate();
        }

        private void Header_MouseLeave(
            object? sender,
            EventArgs e)
        {
            _hover = false;

            Invalidate();
        }

        public void PerformAccordionLayout()
        {
            int width = ClientSize.Width;

            _titleLabel.Width =
                Math.Max(
                    0,
                    width - 50);

            _chevronLabel.Location =
                new Point(
                    Math.Max(0, width - 42),
                    0);

            _chevronLabel.BringToFront();

            int contentHeight = 0;

            if (_expanded &&
                _content != null)
            {
                contentHeight =
                    _content.PreferredSize.Height;

                if (contentHeight <= 0)
                    contentHeight =
                        _content.Height;

                if (contentHeight <= 0)
                    contentHeight = 100;
            }

            Height =
                HeaderHeight +
                (_expanded
                    ? contentHeight
                    : 0);

            if (_content != null)
            {
                _content.Location =
                    new Point(
                        Flush ? 0 : 1,
                        HeaderHeight);

                _content.Width =
                    Math.Max(
                        0,
                        width -
                        (Flush ? 0 : 2));

                _content.Height =
                    Math.Max(
                        0,
                        contentHeight);
            }

            Invalidate();
        }

        private void UpdateVisualState()
        {
            _chevronLabel.Text =
                _expanded
                    ? "⌄"
                    : "›";

            _chevronLabel.ForeColor =
                _expanded
                    ? AccentColor
                    : AppTheme.Colors.TextMuted;

            PerformAccordionLayout();

            Invalidate();
        }

        public void RefreshTheme()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            BackColor =
                AppTheme.Colors.Surface;

            _titleLabel.ForeColor =
                AppTheme.Colors.Text;

            _chevronLabel.ForeColor =
                _expanded
                    ? AccentColor
                    : AppTheme.Colors.TextMuted;

            Invalidate();
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var colors =
                AppTheme.Colors;

            // Header background
            Color headerColor;

            if (_hover)
            {
                headerColor =
                    colors.SurfaceAlt;
            }
            else
            {
                headerColor =
                    colors.Surface;
            }

            using var headerBrush =
                new SolidBrush(headerColor);

            var headerRect =
                new Rectangle(
                    0,
                    0,
                    Width - 1,
                    HeaderHeight - 1);

            g.FillRectangle(
                headerBrush,
                headerRect);

            // Border
            using var borderPen =
                new Pen(
                    colors.Border,
                    1);

            if (!Flush)
            {
                using var path =
                    RoundedRectangle(
                        new Rectangle(
                            0,
                            0,
                            Width - 1,
                            Height - 1),
                        BorderRadius);

                g.DrawPath(
                    borderPen,
                    path);
            }
            else
            {
                g.DrawLine(
                    borderPen,
                    0,
                    HeaderHeight - 1,
                    Width,
                    HeaderHeight - 1);
            }

            // Active accent line
            if (_expanded)
            {
                using var accentBrush =
                    new SolidBrush(
                        AccentColor);

                g.FillRectangle(
                    accentBrush,
                    0,
                    0,
                    3,
                    HeaderHeight);
            }
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path =
                new GraphicsPath();

            int d = radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}

3. Cách sử dụng

Ví dụ tạo Accordion trong Form:

private void CreateAccordion()
{
    var accordion =
        new BootstrapAccordion
        {
            Location = new Point(30, 30),
            Width = 600,
            Height = 400,

            AllowMultipleOpen = false,
            Flush = false
        };

    var accountPanel =
        new Panel
        {
            BackColor =
                AppTheme.Colors.Surface,

            Padding =
                new Padding(20)
        };

    accountPanel.Controls.Add(
        new Label
        {
            Text =
                "Thông tin tài khoản\n\n" +
                "Username: admin\n" +
                "Email: admin@example.com",

            AutoSize = true,

            Font =
                AppTheme.FontRegular,

            ForeColor =
                AppTheme.Colors.Text
        });

    accordion.AddItem(
        "Thông tin tài khoản",
        accountPanel);


    var settingsPanel =
        new Panel
        {
            BackColor =
                AppTheme.Colors.Surface,

            Padding =
                new Padding(20)
        };

    settingsPanel.Controls.Add(
        new BootstrapTextBox
        {
            Width = 300
        });

    accordion.AddItem(
        "Cài đặt",
        settingsPanel);


    var notificationPanel =
        new Panel
        {
            BackColor =
                AppTheme.Colors.Surface,

            Padding =
                new Padding(20)
        };

    notificationPanel.Controls.Add(
        new Label
        {
            Text =
                "Bạn hiện không có thông báo mới.",

            AutoSize = true,

            ForeColor =
                AppTheme.Colors.Text
        });

    accordion.AddItem(
        "Thông báo",
        notificationPanel);


    Controls.Add(accordion);
}

4. Cho phép mở nhiều item

Bootstrap Accordion có thể cấu hình để các panel không nhất thiết phải đóng lẫn nhau. Với control trên:

accordion.AllowMultipleOpen = true;


Khi đó:

┌──────────────────────────────────────┐
│ Thông tin tài khoản               ˄ │
├──────────────────────────────────────┤
│ Username: admin                      │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ Cài đặt                           ˄ │
├──────────────────────────────────────┤
│ [ TextBox                         ]  │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ Thông báo                         > │
└──────────────────────────────────────┘

5. Flush

Tương đương .accordion-flush của Bootstrap:

var accordion =
    new BootstrapAccordion
    {
        Flush = true
    };


Khi đó border ngoài được bỏ, phù hợp khi đặt Accordion bên trong BootstrapCard.

var card =
    new BootstrapCard
    {
        Padding = new Padding(0)
    };

var accordion =
    new BootstrapAccordion
    {
        Dock = DockStyle.Fill,
        Flush = true
    };

card.Controls.Add(accordion);

6. Dark mode

Control này dùng trực tiếp AppTheme.Colors, nên khi bạn đã có ThemeManager ở phần trước:

ThemeManager.SetTheme(
    this,
    ThemeMode.Dark);


Accordion cũng đổi theo:

LIGHT

┌─────────────────────────────────────┐
│ Thông tin tài khoản              ˄ │
├─────────────────────────────────────┤
│ Nội dung                            │
└─────────────────────────────────────┘


DARK

┌─────────────────────────────────────┐
│ Thông tin tài khoản              ˄ │
├─────────────────────────────────────┤
│ Nội dung                            │
└─────────────────────────────────────┘


Màu active sẽ lấy:

AppTheme.Colors.Primary


tức Bootstrap:

#0d6efd

7. Một điểm mình sẽ sửa nếu dùng production

Bản trên đã có collapse/expand, hover, border-radius, chevron, single/multiple open, flush và dark/light theme. Tuy nhiên nó đang thay đổi Height ngay lập tức.

Nếu muốn giống Bootstrap hơn, bước tiếp theo nên thêm animation 150–300 ms:

Click
  │
  ▼
Header active
  │
  ▼
Height: 50
         ↓
       80
         ↓
      120
         ↓
      160
         ↓
     Finished


Bootstrap có cơ chế collapse/transition riêng và trạng thái chevron theo open/closed; đây cũng là một phần quan trọng trong cảm giác của component. 
B
Bootstrap Blog
+1

Mình cũng khuyên đổi › / ⌄ thành SVG chevron và thêm AnimationDuration, HoverBackColor, ActiveBackColor, HeaderHeight, ContentPadding thành các property public. Như vậy BootstrapAccordion sẽ thực sự trở thành một control dùng lâu dài trong bộ WinForms theme của bạn, thay vì chỉ là control demo.

----

> Thêm SVG chevron và thêm AnimationDuration

Được. Mình khuyên sửa BootstrapAccordionItem theo hướng:

Chevron là SVG path thay vì ký tự › / ⌄, nên không phụ thuộc font.
Khi mở: xoay 0° → 90°.
Khi đóng: 90° → 0°.
AnimationDuration tính bằng milliseconds.
Nội dung cũng animate chiều cao, tạo cảm giác giống Bootstrap Collapse.
Khi AnimationDuration = 0 thì tắt animation.
Vẫn giữ Light/Dark, Flush, AllowMultipleOpen.

Với WinForms thuần, có một điểm quan trọng: System.Drawing không render SVG trực tiếp. Vì vậy cách nhẹ nhất là giữ SVG path data của chevron và render vector bằng GraphicsPath. Hình ảnh cuối cùng vẫn là vector, không phụ thuộc font hay bitmap.

1. Thay BootstrapAccordion.cs

Bản này giữ API cũ và thêm animation.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordion : Panel
    {
        private readonly List<BootstrapAccordionItem> _items = new();

        public IReadOnlyList<BootstrapAccordionItem> Items => _items;

        public bool AllowMultipleOpen { get; set; } = false;

        public bool Flush { get; set; } = false;

        public int ItemSpacing { get; set; } = 0;

        /// <summary>
        /// Thời gian animation mở/đóng, tính bằng milliseconds.
        /// 0 = không animation.
        /// </summary>
        public int AnimationDuration { get; set; } = 200;

        public BootstrapAccordion()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            AutoScroll = true;

            BackColor =
                AppTheme.Colors.Surface;
        }

        public BootstrapAccordionItem AddItem(
            string title,
            Control content,
            bool expanded = false)
        {
            var item =
                new BootstrapAccordionItem
                {
                    Title = title,
                    Content = content,
                    Expanded = expanded,
                    Flush = Flush,
                    AnimationDuration =
                        AnimationDuration
                };

            item.ExpandedChanged +=
                Item_ExpandedChanged;

            item.AnimationCompleted +=
                Item_AnimationCompleted;

            _items.Add(item);

            Controls.Add(item);

            LayoutItems();

            return item;
        }

        public void RemoveItem(
            BootstrapAccordionItem item)
        {
            if (!_items.Contains(item))
                return;

            item.ExpandedChanged -=
                Item_ExpandedChanged;

            item.AnimationCompleted -=
                Item_AnimationCompleted;

            _items.Remove(item);

            Controls.Remove(item);

            item.Dispose();

            LayoutItems();
        }

        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.ExpandedChanged -=
                    Item_ExpandedChanged;

                item.AnimationCompleted -=
                    Item_AnimationCompleted;

                item.Dispose();
            }

            _items.Clear();

            Controls.Clear();

            LayoutItems();
        }

        private void Item_ExpandedChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not BootstrapAccordionItem current)
                return;

            if (current.Expanded &&
                !AllowMultipleOpen)
            {
                foreach (var item in _items)
                {
                    if (item != current &&
                        item.Expanded)
                    {
                        item.Expanded = false;
                    }
                }
            }

            LayoutItems();
        }

        private void Item_AnimationCompleted(
            object? sender,
            EventArgs e)
        {
            LayoutItems();
        }

        private void LayoutItems()
        {
            int y = 0;

            int availableWidth =
                ClientSize.Width;

            if (VerticalScroll.Visible)
            {
                availableWidth -=
                    SystemInformation.VerticalScrollBarWidth;
            }

            foreach (var item in _items)
            {
                item.Width =
                    Math.Max(
                        0,
                        availableWidth);

                item.Location =
                    new Point(0, y);

                item.PerformAccordionLayout();

                y +=
                    item.Height +
                    ItemSpacing;
            }

            AutoScrollMinSize =
                new Size(0, y);
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            LayoutItems();
        }

        public void RefreshTheme()
        {
            BackColor =
                AppTheme.Colors.Surface;

            foreach (var item in _items)
            {
                item.Flush = Flush;
                item.AnimationDuration =
                    AnimationDuration;

                item.RefreshTheme();
            }

            Invalidate();
        }
    }
}

2. Thay BootstrapAccordionItem.cs

Đây là phần chính. Bản này có animation và chevron vector.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordionItem : Panel
    {
        private const int HeaderHeight = 50;

        private bool _hover;
        private bool _expanded;

        private int _animationStartHeight;
        private int _animationTargetHeight;

        private float _animationProgress;

        private readonly Timer _animationTimer;
        private readonly Stopwatch _stopwatch;

        private readonly Label _titleLabel;

        private Control? _content;

        public event EventHandler? ExpandedChanged;

        public event EventHandler? AnimationCompleted;

        public string Title
        {
            get => _titleLabel.Text;

            set
            {
                _titleLabel.Text = value;
                Invalidate();
            }
        }

        public Control? Content
        {
            get => _content;

            set
            {
                if (_content != null)
                    Controls.Remove(_content);

                _content = value;

                if (_content != null)
                {
                    _content.Dock =
                        DockStyle.None;

                    Controls.Add(_content);

                    _content.BringToFront();
                }

                PerformAccordionLayout();
            }
        }

        public bool Expanded
        {
            get => _expanded;

            set
            {
                if (_expanded == value)
                    return;

                _expanded = value;

                ExpandedChanged?.Invoke(
                    this,
                    EventArgs.Empty);

                StartAnimation();
            }
        }

        public bool Flush { get; set; }

        public Color AccentColor { get; set; }

        public int BorderRadius { get; set; }
            = AppTheme.BorderRadius;

        /// <summary>
        /// Animation duration in milliseconds.
        /// 0 = no animation.
        /// </summary>
        public int AnimationDuration { get; set; } = 200;

        private int ContentHeight =>
            GetContentHeight();

        public BootstrapAccordionItem()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor =
                AppTheme.Colors.Surface;

            Height =
                HeaderHeight;

            _titleLabel =
                new Label
                {
                    AutoSize = false,

                    TextAlign =
                        ContentAlignment.MiddleLeft,

                    Font =
                        AppTheme.FontRegular,

                    ForeColor =
                        AppTheme.Colors.Text,

                    Location =
                        new Point(16, 0),

                    Height =
                        HeaderHeight,

                    Cursor =
                        Cursors.Hand,

                    BackColor =
                        Color.Transparent
                };

            Controls.Add(_titleLabel);

            _titleLabel.Click +=
                Header_Click;

            _titleLabel.MouseEnter +=
                Header_MouseEnter;

            _titleLabel.MouseLeave +=
                Header_MouseLeave;

            MouseEnter +=
                Header_MouseEnter;

            MouseLeave +=
                Header_MouseLeave;

            _animationTimer =
                new Timer
                {
                    Interval = 15
                };

            _animationTimer.Tick +=
                AnimationTimer_Tick;

            _stopwatch =
                new Stopwatch();

            UpdateHeaderLayout();

            Invalidate();
        }

        private void Header_Click(
            object? sender,
            EventArgs e)
        {
            Expanded = !Expanded;
        }

        private void Header_MouseEnter(
            object? sender,
            EventArgs e)
        {
            _hover = true;
            Invalidate();
        }

        private void Header_MouseLeave(
            object? sender,
            EventArgs e)
        {
            _hover = false;
            Invalidate();
        }

        private void StartAnimation()
        {
            int currentHeight =
                Height;

            int contentHeight =
                ContentHeight;

            _animationStartHeight =
                currentHeight;

            _animationTargetHeight =
                _expanded
                    ? HeaderHeight + contentHeight
                    : HeaderHeight;

            if (AnimationDuration <= 0)
            {
                Height =
                    _animationTargetHeight;

                _animationProgress =
                    _expanded ? 1f : 0f;

                UpdateContentLayout();

                Invalidate();

                AnimationCompleted?.Invoke(
                    this,
                    EventArgs.Empty);

                return;
            }

            _stopwatch.Restart();

            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(
            object? sender,
            EventArgs e)
        {
            double elapsed =
                _stopwatch.Elapsed.TotalMilliseconds;

            float progress =
                (float)(
                    elapsed /
                    AnimationDuration);

            progress =
                Math.Clamp(
                    progress,
                    0f,
                    1f);

            // Ease-out cubic
            float eased =
                1f -
                (float)Math.Pow(
                    1f - progress,
                    3);

            _animationProgress =
                eased;

            Height =
                (int)(
                    _animationStartHeight +
                    (_animationTargetHeight -
                     _animationStartHeight)
                    * eased);

            UpdateContentLayout();

            Invalidate();

            if (progress >= 1f)
            {
                _animationTimer.Stop();

                Height =
                    _animationTargetHeight;

                _animationProgress =
                    _expanded
                        ? 1f
                        : 0f;

                UpdateContentLayout();

                AnimationCompleted?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private int GetContentHeight()
        {
            if (_content == null)
                return 0;

            int height =
                _content.PreferredSize.Height;

            if (height <= 0)
                height =
                    _content.Height;

            if (height <= 0)
                height = 100;

            return height;
        }

        public void PerformAccordionLayout()
        {
            UpdateHeaderLayout();

            int targetContentHeight =
                _expanded
                    ? ContentHeight
                    : 0;

            if (!_animationTimer.Enabled)
            {
                Height =
                    HeaderHeight +
                    targetContentHeight;
            }

            UpdateContentLayout();

            Invalidate();
        }

        private void UpdateHeaderLayout()
        {
            _titleLabel.Width =
                Math.Max(
                    0,
                    Width - 20);

            _titleLabel.Location =
                new Point(16, 0);
        }

        private void UpdateContentLayout()
        {
            if (_content == null)
                return;

            int currentContentHeight =
                Math.Max(
                    0,
                    Height - HeaderHeight);

            _content.Location =
                new Point(
                    Flush ? 0 : 1,
                    HeaderHeight);

            _content.Size =
                new Size(
                    Math.Max(
                        0,
                        Width -
                        (Flush ? 0 : 2)),

                    currentContentHeight);

            _content.Visible =
                _expanded ||
                _animationTimer.Enabled;

            _content.BringToFront();

            _titleLabel.BringToFront();
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var colors =
                AppTheme.Colors;

            DrawHeader(
                g,
                colors);

            DrawChevron(
                g,
                colors);

            DrawBorder(
                g,
                colors);
        }

        private void DrawHeader(
            Graphics g,
            ThemeColors colors)
        {
            Color headerColor =
                _hover
                    ? colors.SurfaceAlt
                    : colors.Surface;

            using var brush =
                new SolidBrush(
                    headerColor);

            g.FillRectangle(
                brush,
                0,
                0,
                Width,
                HeaderHeight);

            if (_expanded)
            {
                using var accentBrush =
                    new SolidBrush(
                        AccentColor);

                g.FillRectangle(
                    accentBrush,
                    0,
                    0,
                    3,
                    HeaderHeight);
            }
        }

        private void DrawChevron(
            Graphics g,
            ThemeColors colors)
        {
            float rotation =
                GetChevronRotation();

            int centerX =
                Width - 25;

            int centerY =
                HeaderHeight / 2;

            using var path =
                CreateChevronPath();

            using var matrix =
                new Matrix();

            matrix.RotateAt(
                rotation,
                new PointF(
                    0,
                    0));

            path.Transform(matrix);

            var bounds =
                path.GetBounds();

            float offsetX =
                centerX -
                bounds.Left -
                bounds.Width / 2;

            float offsetY =
                centerY -
                bounds.Top -
                bounds.Height / 2;

            using var translate =
                new Matrix();

            translate.Translate(
                offsetX,
                offsetY);

            path.Transform(
                translate);

            using var pen =
                new Pen(
                    _expanded
                        ? AccentColor
                        : colors.TextMuted,
                    2f)
                {
                    StartCap =
                        LineCap.Round,

                    EndCap =
                        LineCap.Round,

                    LineJoin =
                        LineJoin.Round
                };

            g.DrawPath(
                pen,
                path);
        }

        private float GetChevronRotation()
        {
            // SVG chevron mặc định hướng sang phải.
            // Khi mở → xoay 90 độ thành hướng xuống.

            float target =
                _expanded
                    ? 90f
                    : 0f;

            float progress =
                _animationTimer.Enabled
                    ? _animationProgress
                    : (_expanded ? 1f : 0f);

            if (_expanded)
            {
                return 90f * progress;
            }

            return 90f * (1f - progress);
        }

        /// <summary>
        /// SVG-equivalent path:
        /// M 9 5 L 16 12 L 9 19
        /// </summary>
        private static GraphicsPath CreateChevronPath()
        {
            var path =
                new GraphicsPath();

            path.StartFigure();

            path.AddLine(
                new PointF(9, 5),
                new PointF(16, 12));

            path.AddLine(
                new PointF(16, 12),
                new PointF(9, 19));

            path.CloseFigure();

            return path;
        }

        private void DrawBorder(
            Graphics g,
            ThemeColors colors)
        {
            using var pen =
                new Pen(
                    colors.Border,
                    1);

            if (Flush)
            {
                g.DrawLine(
                    pen,
                    0,
                    HeaderHeight - 1,
                    Width,
                    HeaderHeight - 1);

                return;
            }

            using var path =
                RoundedRectangle(
                    new Rectangle(
                        0,
                        0,
                        Width - 1,
                        Height - 1),
                    BorderRadius);

            g.DrawPath(
                pen,
                path);
        }

        public void RefreshTheme()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            BackColor =
                AppTheme.Colors.Surface;

            _titleLabel.ForeColor =
                AppTheme.Colors.Text;

            Invalidate();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();

                _animationTimer.Dispose();

                _stopwatch.Stop();
                _stopwatch.Dispose();
            }

            base.Dispose(disposing);
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path =
                new GraphicsPath();

            int d =
                radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}

3. Có một bug nhỏ cần sửa trong phần animation

Trong StartAnimation(), khi đóng Accordion, _expanded đã thành false, nhưng GetContentHeight() vẫn lấy chiều cao bình thường. Điều đó không sao vì target lúc đóng là HeaderHeight.

Tuy nhiên phần chevron cần animation theo hướng đóng chính xác. Code trên đã xử lý:

Mở:
0° ────────────────> 90°

Đóng:
90° ───────────────> 0°


SVG tương đương của chevron là:

<svg viewBox="0 0 24 24">
    <path
        d="M 9 5 L 16 12 L 9 19"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round" />
</svg>


Mình dùng chính path đó trong GraphicsPath, nên không cần cài thêm thư viện SVG.

4. Thêm AnimationDuration

Giờ bạn có thể cấu hình ngay trên Accordion:

var accordion =
    new BootstrapAccordion
    {
        Width = 600,
        Height = 400,

        AnimationDuration = 200,

        AllowMultipleOpen = false,
        Flush = false
    };


Ví dụ chậm hơn:

accordion.AnimationDuration = 350;


Nhanh:

accordion.AnimationDuration = 120;


Tắt animation:

accordion.AnimationDuration = 0;


Mình thấy 200ms là khá đẹp cho WinForms.

5. Một điểm quan trọng: animation hiện tại là animation thật

Khi click:

┌─────────────────────────────────────┐
│ Cài đặt                           > │
└─────────────────────────────────────┘

             click
               ↓

┌─────────────────────────────────────┐
│ Cài đặt                           ↘ │
├─────────────────────────────────────┤
│                                     │
└─────────────────────────────────────┘

               ↓

┌─────────────────────────────────────┐
│ Cài đặt                           ↓ │
├─────────────────────────────────────┤
│ Nội dung                            │
│                                     │
└─────────────────────────────────────┘


Không phải chỉ đổi Visible.

Height được nội suy:

50
 ↓
65
 ↓
82
 ↓
105
 ↓
130
 ↓
150


và dùng ease-out cubic, nên cuối animation sẽ chậm lại giống cảm giác UI web hiện đại.

6. Nhưng mình đề xuất nâng cấp thêm một chút

Có một vấn đề với phiên bản này: nếu Content là một control phức tạp, ví dụ:

DataGridView
Panel
TableLayoutPanel
FlowLayoutPanel
UserControl


thì PreferredSize.Height đôi khi không phản ánh đúng chiều cao mong muốn.

Mình sẽ thêm property:

public int ExpandedHeight { get; set; } = -1;


Khi:

ExpandedHeight = -1


thì tự động lấy PreferredSize.

Còn nếu bạn muốn kiểm soát:

accordion.AddItem(...).ExpandedHeight = 250;


Điều này đặc biệt hữu ích nếu Accordion được dùng cho Sidebar menu.

7. Và có thể làm Accordion hỗ trợ nested Accordion

Với kiến trúc hiện tại, bạn có thể làm:

Settings
├── General
├── Account
├── Security
│   ├── Password
│   ├── 2FA
│   └── Sessions
└── Notifications


Trong đó Security chứa một BootstrapAccordion khác.

Cách này sẽ khá tiện nếu bạn đang xây một WinForms Admin Dashboard.

Ngoài ra, nếu mục tiêu của bạn là tái tạo khá sát Bootstrap 5.3, bước tiếp theo mình khuyên làm luôn BootstrapCollapse làm control nền. Sau đó BootstrapAccordion chỉ là wrapper của BootstrapCollapse. Khi đó bạn có thể tái sử dụng cùng animation engine cho Accordion, Sidebar submenu, Navbar dropdown, Card expandable, Filter panel... và theme sẽ nhất quán hơn rất nhiều.

----

> ok, bạn tạo giup tôi BootstrapCollapse làm control nền và chỉnh sửa lại BootstrapAccordion theo BootstrapCollapse, đồng thời nâng cấp ExpandedHeight theo đề xuất trên nhé!

Được. Đây là hướng mình khuyên dùng: BootstrapCollapse trở thành animation/container primitive, còn BootstrapAccordion chỉ quản lý danh sách các collapse item.

Như vậy sau này bạn có thể tái sử dụng BootstrapCollapse cho:

Accordion
Sidebar submenu
Filter panel
Advanced search
Expandable card
Settings section

Và ExpandedHeight sẽ được hỗ trợ theo 3 chế độ:

ExpandedHeight > 0: dùng chiều cao cố định.
ExpandedHeight = -1: tự lấy PreferredSize.
ExpandedHeight = 0: tự tính theo nội dung.

Mình cũng sửa một vấn đề của bản trước: không dùng Dock = Fill cho content khi đang animate, vì nó khiến việc animate chiều cao khó kiểm soát.

1. BootstrapCollapse.cs

Đây là control nền.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace YourApp.Controls
{
    /// <summary>
    /// Base collapse/expand control.
    ///
    /// ExpandedHeight:
    ///     -1 = auto detect from PreferredSize
    ///      0 = auto detect from PreferredSize
    ///     >0 = explicit height
    /// </summary>
    public class BootstrapCollapse : Panel
    {
        private readonly Panel _contentHost;
        private readonly Timer _animationTimer;
        private readonly Stopwatch _stopwatch;

        private bool _expanded;

        private int _collapsedHeight;
        private int _expandedHeight;

        private int _animationStartHeight;
        private int _animationTargetHeight;

        private float _animationProgress;

        public event EventHandler? ExpandedChanged;

        public event EventHandler? AnimationCompleted;

        /// <summary>
        /// Determines whether the content is expanded.
        /// </summary>
        public bool Expanded
        {
            get => _expanded;

            set
            {
                if (_expanded == value)
                    return;

                _expanded = value;

                ExpandedChanged?.Invoke(
                    this,
                    EventArgs.Empty);

                StartAnimation();
            }
        }

        /// <summary>
        /// Animation duration in milliseconds.
        /// 0 = no animation.
        /// </summary>
        public int AnimationDuration { get; set; } = 200;

        /// <summary>
        /// Expanded content height.
        ///
        /// -1 / 0 = automatic.
        /// >0 = explicit height.
        /// </summary>
        public int ExpandedHeight { get; set; } = -1;

        /// <summary>
        /// Gets the panel that hosts the actual content.
        /// </summary>
        public Panel ContentHost =>
            _contentHost;

        /// <summary>
        /// Content control.
        /// </summary>
        public Control? Content
        {
            get
            {
                if (_contentHost.Controls.Count == 0)
                    return null;

                return _contentHost.Controls[0];
            }

            set
            {
                _contentHost.Controls.Clear();

                if (value != null)
                {
                    value.Dock =
                        DockStyle.None;

                    _contentHost.Controls.Add(
                        value);

                    value.Location =
                        new Point(0, 0);

                    UpdateContentSize();
                }

                UpdateHeightWithoutAnimation();
            }
        }

        /// <summary>
        /// Optional padding around content.
        /// </summary>
        public Padding ContentPadding
        {
            get => _contentHost.Padding;
            set
            {
                _contentHost.Padding = value;

                UpdateContentSize();
            }
        }

        public BootstrapCollapse()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            _collapsedHeight = 0;

            Height = 0;

            Padding = new Padding(0);

            Margin = new Padding(0);

            _contentHost =
                new Panel
                {
                    Location =
                        new Point(0, 0),

                    Padding =
                        new Padding(0),

                    Margin =
                        new Padding(0),

                    BackColor =
                        Color.Transparent
                };

            Controls.Add(
                _contentHost);

            _animationTimer =
                new Timer
                {
                    Interval = 15
                };

            _animationTimer.Tick +=
                AnimationTimer_Tick;

            _stopwatch =
                new Stopwatch();

            UpdateContentSize();
        }

        private int GetExpandedContentHeight()
        {
            if (_contentHost.Controls.Count == 0)
                return 0;

            if (ExpandedHeight > 0)
                return ExpandedHeight;

            Control content =
                _contentHost.Controls[0];

            int width =
                Math.Max(
                    1,
                    _contentHost.ClientSize.Width);

            // Give content its available width
            content.Width = width;

            int preferredHeight =
                content.PreferredSize.Height;

            if (preferredHeight <= 0)
                preferredHeight =
                    content.Height;

            if (preferredHeight <= 0)
                preferredHeight = 100;

            return preferredHeight +
                   _contentHost.Padding.Vertical;
        }

        private void UpdateContentSize()
        {
            int width =
                Math.Max(
                    0,
                    ClientSize.Width);

            _contentHost.Width =
                width;

            if (_contentHost.Controls.Count == 0)
                return;

            Control content =
                _contentHost.Controls[0];

            content.Location =
                new Point(
                    _contentHost.Padding.Left,
                    _contentHost.Padding.Top);

            content.Width =
                Math.Max(
                    0,
                    width -
                    _contentHost.Padding.Horizontal);

            content.Height =
                Math.Max(
                    0,
                    _contentHost.Height -
                    _contentHost.Padding.Vertical);
        }

        private void UpdateHeightWithoutAnimation()
        {
            if (_animationTimer.Enabled)
                return;

            int target =
                _expanded
                    ? GetExpandedContentHeight()
                    : _collapsedHeight;

            Height = target;

            UpdateContentSize();

            Invalidate();
        }

        private void StartAnimation()
        {
            _animationTimer.Stop();

            _animationStartHeight =
                Height;

            _expandedHeight =
                GetExpandedContentHeight();

            _animationTargetHeight =
                _expanded
                    ? _expandedHeight
                    : _collapsedHeight;

            if (AnimationDuration <= 0)
            {
                Height =
                    _animationTargetHeight;

                _animationProgress =
                    _expanded
                        ? 1f
                        : 0f;

                UpdateContentSize();

                Invalidate();

                AnimationCompleted?.Invoke(
                    this,
                    EventArgs.Empty);

                return;
            }

            _stopwatch.Restart();

            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(
            object? sender,
            EventArgs e)
        {
            double elapsed =
                _stopwatch.Elapsed.TotalMilliseconds;

            float progress =
                (float)(
                    elapsed /
                    Math.Max(
                        1,
                        AnimationDuration));

            progress =
                Math.Clamp(
                    progress,
                    0f,
                    1f);

            _animationProgress =
                EaseOutCubic(progress);

            Height =
                (int)(
                    _animationStartHeight +
                    (_animationTargetHeight -
                     _animationStartHeight)
                    * _animationProgress);

            UpdateContentSize();

            Invalidate();

            if (progress >= 1f)
            {
                _animationTimer.Stop();

                Height =
                    _animationTargetHeight;

                _animationProgress =
                    _expanded
                        ? 1f
                        : 0f;

                UpdateContentSize();

                AnimationCompleted?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private static float EaseOutCubic(
            float value)
        {
            return 1f -
                   (float)Math.Pow(
                       1f - value,
                       3);
        }

        public void Expand()
        {
            Expanded = true;
        }

        public void Collapse()
        {
            Expanded = false;
        }

        public void Toggle()
        {
            Expanded = !Expanded;
        }

        public void RefreshLayout()
        {
            UpdateContentSize();

            if (!_animationTimer.Enabled)
            {
                UpdateHeightWithoutAnimation();
            }

            Invalidate();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            UpdateContentSize();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();

                _stopwatch.Stop();
                _stopwatch.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

2. BootstrapAccordion.cs

Giờ Accordion không còn tự xử lý animation nữa.

Nó chỉ:

quản lý items;
xử lý AllowMultipleOpen;
xử lý layout;
cấu hình Flush;
truyền AnimationDuration xuống BootstrapCollapse.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordion : Panel
    {
        private readonly List<BootstrapAccordionItem> _items = new();

        public IReadOnlyList<BootstrapAccordionItem> Items =>
            _items;

        /// <summary>
        /// false = only one item can be opened.
        /// true = multiple items can be opened.
        /// </summary>
        public bool AllowMultipleOpen { get; set; } = false;

        /// <summary>
        /// Bootstrap .accordion-flush style.
        /// </summary>
        public bool Flush { get; set; } = false;

        public int ItemSpacing { get; set; } = 0;

        /// <summary>
        /// Default animation duration for newly
        /// created accordion items.
        /// </summary>
        public int AnimationDuration
        {
            get;
            set;
        } = 200;

        public BootstrapAccordion()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            AutoScroll = true;

            Padding =
                new Padding(0);

            Margin =
                new Padding(0);

            BackColor =
                AppTheme.Colors.Surface;
        }

        public BootstrapAccordionItem AddItem(
            string title,
            Control content,
            bool expanded = false)
        {
            return AddItem(
                title,
                content,
                expanded,
                -1);
        }

        public BootstrapAccordionItem AddItem(
            string title,
            Control content,
            bool expanded,
            int expandedHeight)
        {
            var item =
                new BootstrapAccordionItem
                {
                    Title = title,
                    Flush = Flush,

                    AnimationDuration =
                        AnimationDuration,

                    ExpandedHeight =
                        expandedHeight,

                    Content = content
                };

            item.ExpandedChanged +=
                Item_ExpandedChanged;

            item.AnimationCompleted +=
                Item_AnimationCompleted;

            _items.Add(item);

            Controls.Add(item);

            if (expanded)
            {
                // Set after subscribing to events,
                // so accordion rules are respected.
                item.Expanded = true;
            }

            LayoutItems();

            return item;
        }

        public void RemoveItem(
            BootstrapAccordionItem item)
        {
            if (!_items.Contains(item))
                return;

            item.ExpandedChanged -=
                Item_ExpandedChanged;

            item.AnimationCompleted -=
                Item_AnimationCompleted;

            _items.Remove(item);

            Controls.Remove(item);

            item.Dispose();

            LayoutItems();
        }

        public void ClearItems()
        {
            foreach (var item in _items)
            {
                item.ExpandedChanged -=
                    Item_ExpandedChanged;

                item.AnimationCompleted -=
                    Item_AnimationCompleted;

                item.Dispose();
            }

            _items.Clear();

            Controls.Clear();

            LayoutItems();
        }

        private void Item_ExpandedChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not BootstrapAccordionItem current)
                return;

            if (current.Expanded &&
                !AllowMultipleOpen)
            {
                foreach (var item in _items)
                {
                    if (item != current &&
                        item.Expanded)
                    {
                        item.Expanded = false;
                    }
                }
            }

            LayoutItems();
        }

        private void Item_AnimationCompleted(
            object? sender,
            EventArgs e)
        {
            LayoutItems();
        }

        private void LayoutItems()
        {
            int y = 0;

            int availableWidth =
                ClientSize.Width;

            if (VerticalScroll.Visible)
            {
                availableWidth -=
                    SystemInformation.VerticalScrollBarWidth;
            }

            availableWidth =
                Math.Max(
                    0,
                    availableWidth);

            foreach (var item in _items)
            {
                item.Width =
                    availableWidth;

                item.Location =
                    new Point(
                        0,
                        y);

                item.PerformAccordionLayout();

                y +=
                    item.Height +
                    ItemSpacing;
            }

            AutoScrollMinSize =
                new Size(
                    0,
                    Math.Max(
                        0,
                        y));
        }

        public void ExpandAll()
        {
            foreach (var item in _items)
                item.Expanded = true;

            LayoutItems();
        }

        public void CollapseAll()
        {
            foreach (var item in _items)
                item.Expanded = false;

            LayoutItems();
        }

        public void RefreshTheme()
        {
            BackColor =
                AppTheme.Colors.Surface;

            foreach (var item in _items)
            {
                item.Flush =
                    Flush;

                item.AnimationDuration =
                    AnimationDuration;

                item.RefreshTheme();
            }

            Invalidate();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            LayoutItems();
        }
    }
}

3. BootstrapAccordionItem.cs

Item giờ inherit từ BootstrapCollapse.

Đây chính là điểm thay đổi kiến trúc quan trọng.

BootstrapCollapse
       │
       │  animation
       │  content
       │  Expanded
       │  ExpandedHeight
       │
       ▼
BootstrapAccordionItem
       │
       │  header
       │  chevron
       │  border
       │  Bootstrap style
       │
       ▼
BootstrapAccordion
       │
       ├── Item 1
       ├── Item 2
       └── Item 3


Code:

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordionItem
        : BootstrapCollapse
    {
        private const int HeaderHeight = 50;

        private readonly Label _titleLabel;

        private bool _hover;

        public string Title
        {
            get => _titleLabel.Text;

            set
            {
                _titleLabel.Text = value;
                Invalidate();
            }
        }

        public bool Flush { get; set; }

        public Color AccentColor { get; set; }

        public int BorderRadius { get; set; }
            = AppTheme.BorderRadius;

        public int HeaderHeightValue =>
            HeaderHeight;

        public BootstrapAccordionItem()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor =
                AppTheme.Colors.Surface;

            // Accordion item starts with header only.
            Expanded = false;

            _titleLabel =
                new Label
                {
                    AutoSize = false,

                    TextAlign =
                        ContentAlignment.MiddleLeft,

                    Font =
                        AppTheme.FontRegular,

                    ForeColor =
                        AppTheme.Colors.Text,

                    Location =
                        new Point(16, 0),

                    Height =
                        HeaderHeight,

                    Cursor =
                        Cursors.Hand,

                    BackColor =
                        Color.Transparent
                };

            Controls.Add(
                _titleLabel);

            _titleLabel.Click +=
                Header_Click;

            _titleLabel.MouseEnter +=
                Header_MouseEnter;

            _titleLabel.MouseLeave +=
                Header_MouseLeave;

            MouseEnter +=
                Header_MouseEnter;

            MouseLeave +=
                Header_MouseLeave;

            // Content is hosted below header.
            ContentHost.Location =
                new Point(
                    0,
                    HeaderHeight);

            UpdateHeaderLayout();

            Invalidate();
        }

        private void Header_Click(
            object? sender,
            EventArgs e)
        {
            Toggle();
        }

        private void Header_MouseEnter(
            object? sender,
            EventArgs e)
        {
            _hover = true;

            Invalidate();
        }

        private void Header_MouseLeave(
            object? sender,
            EventArgs e)
        {
            _hover = false;

            Invalidate();
        }

        public void PerformAccordionLayout()
        {
            UpdateHeaderLayout();

            // BootstrapCollapse owns the
            // actual content height.
            int contentHeight =
                Math.Max(
                    0,
                    Height - HeaderHeight);

            ContentHost.Location =
                new Point(
                    Flush ? 0 : 1,
                    HeaderHeight);

            ContentHost.Width =
                Math.Max(
                    0,
                    Width -
                    (Flush ? 0 : 2));

            ContentHost.Height =
                contentHeight;

            UpdateContentPosition();

            Invalidate();
        }

        private void UpdateHeaderLayout()
        {
            _titleLabel.Width =
                Math.Max(
                    0,
                    Width - 55);

            _titleLabel.Location =
                new Point(
                    16,
                    0);

            ContentHost.BringToFront();

            _titleLabel.BringToFront();
        }

        private void UpdateContentPosition()
        {
            if (ContentHost.Controls.Count == 0)
                return;

            Control content =
                ContentHost.Controls[0];

            content.Width =
                Math.Max(
                    0,
                    ContentHost.Width -
                    ContentPadding.Horizontal);

            content.Height =
                Math.Max(
                    0,
                    ContentHost.Height -
                    ContentPadding.Vertical);
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            var colors =
                AppTheme.Colors;

            DrawHeader(
                g,
                colors);

            DrawChevron(
                g,
                colors);

            DrawBorder(
                g,
                colors);
        }

        private void DrawHeader(
            Graphics g,
            ThemeColors colors)
        {
            Color background =
                _hover
                    ? colors.SurfaceAlt
                    : colors.Surface;

            using var brush =
                new SolidBrush(
                    background);

            g.FillRectangle(
                brush,
                0,
                0,
                Width,
                HeaderHeight);

            if (Expanded)
            {
                using var accentBrush =
                    new SolidBrush(
                        AccentColor);

                g.FillRectangle(
                    accentBrush,
                    0,
                    0,
                    3,
                    HeaderHeight);
            }
        }

        private void DrawChevron(
            Graphics g,
            ThemeColors colors)
        {
            float rotation =
                GetChevronRotation();

            float centerX =
                Width - 25;

            float centerY =
                HeaderHeight / 2f;

            using var path =
                CreateChevronPath();

            var bounds =
                path.GetBounds();

            float pathCenterX =
                bounds.Left +
                bounds.Width / 2f;

            float pathCenterY =
                bounds.Top +
                bounds.Height / 2f;

            using var matrix =
                new Matrix();

            matrix.Translate(
                -pathCenterX,
                -pathCenterY,
                MatrixOrder.Append);

            matrix.Rotate(
                rotation,
                MatrixOrder.Append);

            matrix.Translate(
                centerX,
                centerY,
                MatrixOrder.Append);

            path.Transform(matrix);

            using var pen =
                new Pen(
                    Expanded
                        ? AccentColor
                        : colors.TextMuted,
                    2f)
                {
                    StartCap =
                        LineCap.Round,

                    EndCap =
                        LineCap.Round,

                    LineJoin =
                        LineJoin.Round
                };

            g.DrawPath(
                pen,
                path);
        }

        private float GetChevronRotation()
        {
            // Bootstrap style:
            //
            // closed = >
            // open   = v
            //
            // We rotate the SVG-equivalent path
            // from 0° to 90°.

            if (!IsAnimating)
            {
                return Expanded
                    ? 90f
                    : 0f;
            }

            return Expanded
                ? 90f * AnimationProgress
                : 90f * (1f - AnimationProgress);
        }

        private static GraphicsPath CreateChevronPath()
        {
            var path =
                new GraphicsPath();

            path.StartFigure();

            path.AddLine(
                new PointF(9, 5),
                new PointF(16, 12));

            path.AddLine(
                new PointF(16, 12),
                new PointF(9, 19));

            return path;
        }

        private bool IsAnimating =>
            AnimationProgress > 0f &&
            AnimationProgress < 1f;

        private float AnimationProgress
        {
            get
            {
                // BootstrapCollapse keeps the timer
                // private, so this property is updated
                // by the animation event through
                // Invalidate/paint cycle.
                //
                // For the simple implementation,
                // derive state from current height.

                int target =
                    Expanded
                        ? Math.Max(
                            HeaderHeight,
                            Height)
                        : HeaderHeight;

                if (target == HeaderHeight)
                    return Expanded ? 1f : 0f;

                float content =
                    Math.Max(
                        1,
                        target -
                        HeaderHeight);

                float current =
                    Math.Max(
                        0,
                        Height -
                        HeaderHeight);

                return Math.Clamp(
                    current / content,
                    0f,
                    1f);
            }
        }

        private void DrawBorder(
            Graphics g,
            ThemeColors colors)
        {
            using var pen =
                new Pen(
                    colors.Border,
                    1f);

            if (Flush)
            {
                g.DrawLine(
                    pen,
                    0,
                    HeaderHeight - 1,
                    Width,
                    HeaderHeight - 1);

                return;
            }

            using var path =
                RoundedRectangle(
                    new Rectangle(
                        0,
                        0,
                        Width - 1,
                        Height - 1),
                    BorderRadius);

            g.DrawPath(
                pen,
                path);
        }

        public void RefreshTheme()
        {
            AccentColor =
                AppTheme.Colors.Primary;

            BackColor =
                AppTheme.Colors.Surface;

            _titleLabel.ForeColor =
                AppTheme.Colors.Text;

            Invalidate();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            PerformAccordionLayout();
        }

        private static GraphicsPath RoundedRectangle(
            Rectangle rect,
            int radius)
        {
            var path =
                new GraphicsPath();

            int d =
                radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}


Có một điểm mình muốn chỉnh lại ngay ở bản trên: để chevron animation thực sự đồng bộ 100% với animation height, BootstrapCollapse nên expose AnimationProgress. Đây là cách sạch hơn là để AccordionItem suy ra progress từ Height.

4. Bổ sung AnimationProgress vào BootstrapCollapse

Trong BootstrapCollapse, thêm property public:

/// <summary>
/// Current animation progress from 0 to 1.
/// </summary>
public float AnimationProgress =>
    _animationProgress;

/// <summary>
/// Returns true while collapse animation is running.
/// </summary>
public bool IsAnimating =>
    _animationTimer.Enabled;


Và trong AnimationTimer_Tick, sau:

_animationProgress =
    EaseOutCubic(progress);


thêm:

Invalidate();


Sau đó BootstrapAccordionItem chỉ cần:

private float GetChevronRotation()
{
    if (!IsAnimating)
        return Expanded ? 90f : 0f;

    return Expanded
        ? 90f * AnimationProgress
        : 90f * (1f - AnimationProgress);
}


và:

private bool IsAnimating =>
    base.IsAnimating;


Tức là không cần property AnimationProgress tự tính bằng Height nữa.

5. ExpandedHeight giờ hoạt động như thế nào?

Đây là phần đáng chú ý nhất.

Auto
accordion.AddItem(
    "Thông tin",
    panel);


hoặc:

var item = accordion.AddItem(
    "Thông tin",
    panel);

item.ExpandedHeight = -1;


Control sẽ lấy:

content.PreferredSize.Height

Fixed height

Ví dụ content rất dài nhưng bạn chỉ muốn mở 200px:

var item = accordion.AddItem(
    "Logs",
    logPanel);

item.ExpandedHeight = 200;


Khi mở:

┌───────────────────────────────┐
│ Logs                         ˅ │
├───────────────────────────────┤
│                               │
│             200px             │
│                               │
└───────────────────────────────┘


Nếu content cao hơn thì bạn có thể đặt:

logPanel.AutoScroll = true;

Set ngay khi AddItem

Mình đã thêm overload:

accordion.AddItem(
    "Logs",
    logPanel,
    expanded: false,
    expandedHeight: 200);

6. BootstrapCollapse độc lập

Điểm hay nhất của kiến trúc mới là bạn không cần Accordion để dùng Collapse.

Ví dụ một Filter panel:

var collapse =
    new BootstrapCollapse
    {
        Width = 500,

        AnimationDuration = 200,

        ExpandedHeight = -1
    };

collapse.Content =
    new Panel
    {
        Height = 180,
        BackColor =
            AppTheme.Colors.Surface
    };

Controls.Add(collapse);


Mở:

collapse.Expand();


Đóng:

collapse.Collapse();


Hoặc:

collapse.Toggle();

7. Sidebar cũng có thể dùng lại Collapse

Đây chính là lý do mình thích tách BootstrapCollapse.

Bạn có thể làm:

Dashboard

Users
   ├── All users
   ├── Roles
   └── Permissions

Products
   ├── Products
   ├── Categories
   └── Inventory

Reports
   ├── Sales
   └── Finance


Users, Products, Reports thực chất đều có thể dùng:

BootstrapCollapse


thay vì viết một animation engine khác cho Sidebar.

8. Một chỉnh sửa quan trọng cho Content

Với content phức tạp, mình khuyên dùng:

var panel = new Panel
{
    Padding = new Padding(20),
    AutoScroll = true
};

panel.Controls.Add(...);

var item = accordion.AddItem(
    "Advanced settings",
    panel);

item.ExpandedHeight = 300;


Nếu muốn auto height:

item.ExpandedHeight = -1;

9. API cuối cùng

Sau khi tách như trên, API khá đẹp:

var accordion = new BootstrapAccordion
{
    Width = 600,
    Height = 450,

    AllowMultipleOpen = false,

    Flush = false,

    AnimationDuration = 200
};


Thêm item:

accordion.AddItem(
    "General",
    generalPanel);


Fixed:

accordion.AddItem(
    "Logs",
    logPanel,
    false,
    250);


Hoặc lấy item:

var item = accordion.AddItem(
    "Security",
    securityPanel);

item.ExpandedHeight = 300;
item.AccentColor =
    AppTheme.Colors.Danger;


Điều khiển:

item.Expand();
item.Collapse();
item.Toggle();


Toàn bộ:

accordion.ExpandAll();
accordion.CollapseAll();

Một cải tiến mình khuyên làm tiếp

Có một chi tiết UX rất đáng làm: header hiện tại đang là Label, nên vùng click thực tế chỉ nằm trên label. Với Bootstrap thật, toàn bộ header là một button.

Mình sẽ đổi BootstrapAccordionItem thành:

BootstrapAccordionItem
│
├── BootstrapAccordionHeader
│      ├── Text
│      └── SVG Chevron
│
└── BootstrapCollapse
       └── Content


và BootstrapAccordionHeader sẽ có:

toàn bộ width/height đều click được;
hover trên toàn header;
keyboard Enter / Space;
focus outline;
TabStop;
accessibility-friendly;
chevron SVG xoay theo AnimationProgress.

Khi đó control sẽ gần Bootstrap 5.3 hơn đáng kể và đồng thời dùng được cho Sidebar, Tree-like menu và Collapse chứ không chỉ Accordion.

----

> Cải tiến nhận focus và phím cho header

Được. Mình sẽ tách BootstrapAccordionHeader thành control riêng và để nó chịu trách nhiệm hoàn toàn cho phần header + interaction + SVG chevron + keyboard + focus. BootstrapAccordionItem chỉ ghép Header + BootstrapCollapse.

Kiến trúc lúc này sẽ là:

BootstrapCollapse
    └── animation engine

BootstrapAccordionHeader
    ├── text
    ├── SVG chevron
    ├── hover
    ├── pressed
    ├── focus
    ├── mouse
    ├── Enter / Space
    └── keyboard navigation

BootstrapAccordionItem
    ├── Header
    └── Collapse content

BootstrapAccordion
    └── Items[]


Có một điểm mình cũng sửa so với code trước: chevron không tự tính animation từ Height nữa, mà lấy trực tiếp AnimationProgress từ BootstrapCollapse.

1. BootstrapAccordionHeader.cs

Đây là control mới.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    /// <summary>
    /// Bootstrap-like accordion header.
    ///
    /// Features:
    /// - Full header clickable
    /// - Hover
    /// - Pressed
    /// - Keyboard Enter / Space
    /// - Focus visual
    /// - Vector SVG-like chevron
    /// - Animated chevron rotation
    /// </summary>
    public class BootstrapAccordionHeader : Control
    {
        private bool _hover;
        private bool _pressed;
        private bool _expanded;

        private string _text = string.Empty;

        public event EventHandler? Clicked;

        public event EventHandler? ExpandedChanged;

        public BootstrapAccordionHeader()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            TabStop = true;

            Cursor = Cursors.Hand;

            Height = 50;

            Font =
                AppTheme.FontRegular;

            ForeColor =
                AppTheme.Colors.Text;

            BackColor =
                AppTheme.Colors.Surface;
        }

        #region Properties

        public string TextValue
        {
            get => _text;

            set
            {
                if (_text == value)
                    return;

                _text = value ?? string.Empty;

                Invalidate();
            }
        }

        public bool Expanded
        {
            get => _expanded;

            set
            {
                if (_expanded == value)
                    return;

                _expanded = value;

                Invalidate();

                ExpandedChanged?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        /// <summary>
        /// Animation progress from 0 to 1.
        /// 0 = closed
        /// 1 = opened
        /// </summary>
        public float AnimationProgress
        {
            get;
            set
            {
                field =
                    Math.Clamp(
                        value,
                        0f,
                        1f);

                Invalidate();
            }
        }

        public Color AccentColor { get; set; } =
            AppTheme.Colors.Primary;

        public Color HoverBackColor { get; set; } =
            AppTheme.Colors.SurfaceAlt;

        public Color PressedBackColor { get; set; } =
            AppTheme.Colors.SurfaceAlt;

        public Color FocusBorderColor { get; set; } =
            AppTheme.Colors.Primary;

        public Color ChevronColor { get; set; } =
            AppTheme.Colors.TextMuted;

        public int ChevronSize { get; set; } = 18;

        public int HorizontalPadding { get; set; } = 16;

        public bool ShowFocusRectangle { get; set; } = true;

        #endregion

        #region Mouse

        protected override void OnMouseEnter(
            EventArgs e)
        {
            base.OnMouseEnter(e);

            _hover = true;

            Invalidate();
        }

        protected override void OnMouseLeave(
            EventArgs e)
        {
            base.OnMouseLeave(e);

            _hover = false;
            _pressed = false;

            Invalidate();
        }

        protected override void OnMouseDown(
            MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            _pressed = true;

            Focus();

            Invalidate();
        }

        protected override void OnMouseUp(
            MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left)
                return;

            bool shouldClick =
                _pressed &&
                ClientRectangle.Contains(
                    e.Location);

            _pressed = false;

            Invalidate();

            if (shouldClick)
            {
                OnHeaderClicked();
            }
        }

        private void OnHeaderClicked()
        {
            Expanded = !Expanded;

            Clicked?.Invoke(
                this,
                EventArgs.Empty);
        }

        #endregion

        #region Keyboard

        protected override bool IsInputKey(
            Keys keyData)
        {
            Keys key =
                keyData & Keys.KeyCode;

            if (key == Keys.Enter ||
                key == Keys.Space)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(
            KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Enter ||
                e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                OnHeaderClicked();

                return;
            }
        }

        protected override void OnGotFocus(
            EventArgs e)
        {
            base.OnGotFocus(e);

            Invalidate();
        }

        protected override void OnLostFocus(
            EventArgs e)
        {
            base.OnLostFocus(e);

            _pressed = false;

            Invalidate();
        }

        #endregion

        #region Paint

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            DrawBackground(g);

            DrawAccent(g);

            DrawText(g);

            DrawChevron(g);

            DrawFocus(g);
        }

        private void DrawBackground(
            Graphics g)
        {
            Color background;

            if (_pressed)
            {
                background =
                    PressedBackColor;
            }
            else if (_hover)
            {
                background =
                    HoverBackColor;
            }
            else
            {
                background =
                    BackColor;
            }

            using var brush =
                new SolidBrush(background);

            g.FillRectangle(
                brush,
                ClientRectangle);
        }

        private void DrawAccent(
            Graphics g)
        {
            if (!Expanded)
                return;

            using var brush =
                new SolidBrush(
                    AccentColor);

            g.FillRectangle(
                brush,
                0,
                0,
                3,
                Height);
        }

        private void DrawText(
            Graphics g)
        {
            int chevronArea =
                48;

            Rectangle textRect =
                new Rectangle(
                    HorizontalPadding,
                    0,
                    Math.Max(
                        0,
                        Width -
                        HorizontalPadding * 2 -
                        chevronArea),
                    Height);

            TextRenderer.DrawText(
                g,
                TextValue,
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }

        private void DrawChevron(
            Graphics g)
        {
            float progress =
                Math.Clamp(
                    AnimationProgress,
                    0f,
                    1f);

            float rotation =
                90f * progress;

            float centerX =
                Width - 24;

            float centerY =
                Height / 2f;

            using var path =
                CreateChevronPath(
                    ChevronSize);

            RectangleF bounds =
                path.GetBounds();

            float pathCenterX =
                bounds.Left +
                bounds.Width / 2f;

            float pathCenterY =
                bounds.Top +
                bounds.Height / 2f;

            using var matrix =
                new Matrix();

            matrix.Translate(
                -pathCenterX,
                -pathCenterY,
                MatrixOrder.Append);

            matrix.Rotate(
                rotation,
                MatrixOrder.Append);

            matrix.Translate(
                centerX,
                centerY,
                MatrixOrder.Append);

            path.Transform(matrix);

            Color color =
                Expanded
                    ? AccentColor
                    : ChevronColor;

            using var pen =
                new Pen(
                    color,
                    2f)
                {
                    StartCap =
                        LineCap.Round,

                    EndCap =
                        LineCap.Round,

                    LineJoin =
                        LineJoin.Round
                };

            g.DrawPath(
                pen,
                path);
        }

        /// <summary>
        /// SVG-equivalent:
        ///
        /// M 7 5
        /// L 12 10
        /// L 17 5
        ///
        /// Then rotated by 90° when opened.
        /// </summary>
        private static GraphicsPath
            CreateChevronPath(int size)
        {
            float scale =
                size / 18f;

            float left =
                -5f * scale;

            float right =
                5f * scale;

            float top =
                -4f * scale;

            float bottom =
                4f * scale;

            float centerX = 0;

            var path =
                new GraphicsPath();

            path.StartFigure();

            path.AddLine(
                centerX + left,
                top,
                centerX,
                bottom);

            path.AddLine(
                centerX,
                bottom,
                centerX + right,
                top);

            return path;
        }

        private void DrawFocus(
            Graphics g)
        {
            if (!Focused ||
                !ShowFocusRectangle)
            {
                return;
            }

            using var pen =
                new Pen(
                    FocusBorderColor,
                    1f)
                {
                    DashStyle =
                        DashStyle.Dot
                };

            Rectangle rect =
                new Rectangle(
                    4,
                    4,
                    Width - 9,
                    Height - 9);

            g.DrawRectangle(
                pen,
                rect);
        }

        #endregion

        public void RefreshTheme()
        {
            Font =
                AppTheme.FontRegular;

            ForeColor =
                AppTheme.Colors.Text;

            BackColor =
                AppTheme.Colors.Surface;

            AccentColor =
                AppTheme.Colors.Primary;

            HoverBackColor =
                AppTheme.Colors.SurfaceAlt;

            PressedBackColor =
                AppTheme.Colors.SurfaceAlt;

            FocusBorderColor =
                AppTheme.Colors.Primary;

            ChevronColor =
                AppTheme.Colors.TextMuted;

            Invalidate();
        }
    }
}


Lưu ý: đoạn trên dùng C# field accessor cho property AnimationProgress. Nếu project của bạn chưa dùng C# 14, thay property đó bằng backing field _animationProgress như sau:

private float _animationProgress;

public float AnimationProgress
{
    get => _animationProgress;

    set
    {
        _animationProgress =
            Math.Clamp(value, 0f, 1f);

        Invalidate();
    }
}


Mình khuyên dùng bản backing field nếu bạn muốn tương thích rộng với các project WinForms hiện tại.

2. Sửa BootstrapCollapse

Thêm hai property này vào BootstrapCollapse:

public float AnimationProgress =>
    _animationProgress;

public bool IsAnimating =>
    _animationTimer.Enabled;


Tức phần public API của BootstrapCollapse trở thành:

public bool Expanded { get; set; }

public int AnimationDuration { get; set; } = 200;

public int ExpandedHeight { get; set; } = -1;

public float AnimationProgress =>
    _animationProgress;

public bool IsAnimating =>
    _animationTimer.Enabled;

public Panel ContentHost =>
    _contentHost;


Đây là điểm quan trọng để các control khác có thể "nghe" animation engine.

3. BootstrapAccordionItem mới

Giờ item rất đơn giản.

using System;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapAccordionItem
        : BootstrapCollapse
    {
        private readonly BootstrapAccordionHeader _header;

        public BootstrapAccordionHeader Header =>
            _header;

        public string Title
        {
            get => _header.TextValue;

            set => _header.TextValue = value;
        }

        public bool Flush
        {
            get;
            set
            {
                field = value;
                UpdateLayout();
                Invalidate();
            }
        }

        public Color AccentColor
        {
            get => _header.AccentColor;

            set
            {
                _header.AccentColor = value;
                Invalidate();
            }
        }

        public int BorderRadius { get; set; } =
            AppTheme.BorderRadius;

        public BootstrapAccordionItem()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor =
                AppTheme.Colors.Surface;

            _header =
                new BootstrapAccordionHeader
                {
                    Dock =
                        DockStyle.Top,

                    Height = 50,

                    TabStop = true
                };

            Controls.Add(
                _header);

            _header.Clicked +=
                Header_Clicked;

            ExpandedChanged +=
                Collapse_ExpandedChanged;

            AnimationCompleted +=
                Collapse_AnimationCompleted;

            // Content is below header.
            ContentHost.BringToFront();

            _header.BringToFront();

            UpdateLayout();
        }

        private void Header_Clicked(
            object? sender,
            EventArgs e)
        {
            // Header has already toggled itself.
            // Synchronize collapse state.

            bool desired =
                _header.Expanded;

            if (Expanded != desired)
            {
                Expanded = desired;
            }
        }

        private void Collapse_ExpandedChanged(
            object? sender,
            EventArgs e)
        {
            _header.Expanded =
                Expanded;

            _header.AnimationProgress =
                Expanded ? 1f : 0f;

            Invalidate();
        }

        private void Collapse_AnimationCompleted(
            object? sender,
            EventArgs e)
        {
            _header.AnimationProgress =
                Expanded ? 1f : 0f;

            UpdateLayout();

            Invalidate();
        }

        private void UpdateLayout()
        {
            _header.Width =
                ClientSize.Width;

            ContentHost.Location =
                new Point(
                    Flush ? 0 : 1,
                    _header.Bottom);

            ContentHost.Width =
                Math.Max(
                    0,
                    Width -
                    (Flush ? 0 : 2));

            ContentHost.Height =
                Math.Max(
                    0,
                    Height -
                    _header.Height);

            ContentHost.BringToFront();

            _header.BringToFront();
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            var g =
                e.Graphics;

            g.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var colors =
                AppTheme.Colors;

            using var pen =
                new Pen(
                    colors.Border,
                    1f);

            if (Flush)
            {
                g.DrawLine(
                    pen,
                    0,
                    _header.Bottom - 1,
                    Width,
                    _header.Bottom - 1);

                return;
            }

            using var path =
                CreateRoundedRectangle(
                    new Rectangle(
                        0,
                        0,
                        Width - 1,
                        Height - 1),
                    BorderRadius);

            g.DrawPath(
                pen,
                path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath
            CreateRoundedRectangle(
                Rectangle rect,
                int radius)
        {
            var path =
                new System.Drawing.Drawing2D.GraphicsPath();

            int d =
                radius * 2;

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            UpdateLayout();
        }

        public void RefreshTheme()
        {
            BackColor =
                AppTheme.Colors.Surface;

            _header.RefreshTheme();

            Invalidate();
        }
    }
}


Ở đây có một điểm nữa: BootstrapAccordionHeader cần được đồng bộ liên tục với animation progress của BootstrapCollapse, không chỉ ở đầu/cuối animation.

Cách tốt nhất là cho BootstrapCollapse phát event trong quá trình animation.

4. Thêm AnimationProgressChanged

Trong BootstrapCollapse:

public event EventHandler?
    AnimationProgressChanged;


Sau khi cập nhật:

_animationProgress =
    EaseOutCubic(progress);


thêm:

AnimationProgressChanged?.Invoke(
    this,
    EventArgs.Empty);


Và trong BootstrapAccordionItem:

AnimationProgressChanged +=
    Collapse_AnimationProgressChanged;


Handler:

private void Collapse_AnimationProgressChanged(
    object? sender,
    EventArgs e)
{
    _header.AnimationProgress =
        AnimationProgress;
}


Khi đó animation chạy như sau:

BootstrapCollapse
      │
      │ progress = 0.00
      ▼
AccordionHeader
      │
      │ chevron = 0°
      ▼
      │
      │ progress = 0.25
      ▼
AccordionHeader
      │
      │ chevron = 22.5°
      ▼
      │
      │ progress = 0.50
      ▼
AccordionHeader
      │
      │ chevron = 45°
      ▼
      │
      │ progress = 0.75
      ▼
AccordionHeader
      │
      │ chevron = 67.5°
      ▼
      │
      │ progress = 1.00
      ▼
AccordionHeader
      │
      │ chevron = 90°


Đây là cách mình khuyên dùng.

5. Sửa constructor của AccordionItem

Thêm event:

AnimationProgressChanged +=
    Collapse_AnimationProgressChanged;


Và handler:

private void Collapse_AnimationProgressChanged(
    object? sender,
    EventArgs e)
{
    _header.AnimationProgress =
        AnimationProgress;
}


Khi đó không cần AnimationProgress tự suy luận nữa.

6. Một vấn đề về thứ tự control

Trong BootstrapAccordionItem, chúng ta có:

BootstrapAccordionItem
│
├── Header
│
└── ContentHost
      └── UserControl


Do ContentHost có thể nằm đè lên header nếu layout không đúng, mình khuyên không dùng BringToFront() tùy tiện mà sắp xếp thứ tự:

Controls.Add(ContentHost);
Controls.Add(_header);

_header.BringToFront();


Và:

ContentHost.Location =
    new Point(
        Flush ? 0 : 1,
        _header.Height);


Đảm bảo:

0
│
├── Header       50px
│
├── Content      animated height
│
└── end

7. Keyboard interaction

Bây giờ Accordion header hoạt động khá giống button.

Click chuột:

MouseDown
   ↓
Pressed
   ↓
MouseUp
   ↓
Toggle


Keyboard:

Tab
 ↓
[General]  ← focus
 ↓
Space
 ↓
Expanded


hoặc:

Tab
 ↓
[General]
 ↓
Enter
 ↓
Expanded


Focus được vẽ:

┌─────────────────────────────────────┐
│ General                         ˅   │
└─────────────────────────────────────┘
  - - - - - - - - - - - - - - - - -


Điều này đặc biệt hữu ích nếu ứng dụng WinForms của bạn có yêu cầu accessibility hoặc người dùng thích thao tác bằng keyboard.

8. Toàn bộ API sử dụng

Code phía ngoài gần như không thay đổi:

var accordion =
    new BootstrapAccordion
    {
        Width = 600,
        Height = 450,

        AllowMultipleOpen = false,

        Flush = false,

        AnimationDuration = 200
    };

accordion.AddItem(
    "Thông tin tài khoản",
    accountPanel);

accordion.AddItem(
    "Cài đặt",
    settingsPanel);

accordion.AddItem(
    "Thông báo",
    notificationPanel);


Fixed height:

accordion.AddItem(
    "Logs",
    logPanel,
    false,
    300);


Hoặc:

var item =
    accordion.AddItem(
        "Security",
        securityPanel);

item.ExpandedHeight = 350;

9. Theme

Khi đổi:

ThemeManager.SetTheme(
    this,
    ThemeMode.Dark);


chỉ cần:

accordion.RefreshTheme();


Header sẽ lấy:

AppTheme.Colors.Surface
AppTheme.Colors.SurfaceAlt
AppTheme.Colors.Text
AppTheme.Colors.TextMuted
AppTheme.Colors.Border
AppTheme.Colors.Primary


Ví dụ Bootstrap 5 palette:

public static readonly Color Primary =
    ColorTranslator.FromHtml("#0d6efd");

public static readonly Color Secondary =
    ColorTranslator.FromHtml("#6c757d");

public static readonly Color Success =
    ColorTranslator.FromHtml("#198754");

public static readonly Color Danger =
    ColorTranslator.FromHtml("#dc3545");

public static readonly Color Warning =
    ColorTranslator.FromHtml("#ffc107");

public static readonly Color Info =
    ColorTranslator.FromHtml("#0dcaf0");

public static readonly Color Light =
    ColorTranslator.FromHtml("#f8f9fa");

public static readonly Color Dark =
    ColorTranslator.FromHtml("#212529");

10. Kiến trúc cuối cùng

Sau các thay đổi này, bộ control của bạn đã khá đẹp về mặt kiến trúc:

                    AppTheme
                       │
                       ▼
               ┌───────────────┐
               │ ThemeColors   │
               └───────┬───────┘
                       │
          ┌────────────┴─────────────┐
          │                          │
          ▼                          ▼
 BootstrapCollapse          BootstrapAccordionHeader
          │                          │
          │ animation                │ UI / interaction
          │                          │
          └────────────┬─────────────┘
                       ▼
             BootstrapAccordionItem
                       │
                       ▼
             BootstrapAccordion


Điểm mình đặc biệt thích ở kiến trúc này là animation không còn thuộc Accordion nữa. BootstrapCollapse trở thành primitive dùng chung.

Ví dụ sau này Sidebar chỉ cần:

var submenu =
    new BootstrapCollapse
    {
        AnimationDuration = 180,
        ExpandedHeight = 180
    };

submenu.Content =
    sidebarSubmenu;


Còn Accordion dùng:

BootstrapAccordion
    ↓
BootstrapAccordionItem
    ↓
BootstrapAccordionHeader
    +
BootstrapCollapse


Như vậy toàn bộ bộ UI WinForms của bạn có thể dùng một animation engine duy nhất, thay vì mỗi control tự implement timer riêng. Đây là hướng mình sẽ chọn nếu mục tiêu là xây cả bộ BootstrapButton, BootstrapTextBox, BootstrapCard, BootstrapSidebar, BootstrapDataGridView, BootstrapAccordion thành một WinForms Bootstrap-like UI framework.

----

> ButtonGroup

Được. Với Button Group, mình sẽ không làm nó như một Panel đơn giản chứa các button, mà thiết kế thành một control riêng để xử lý đúng tinh thần Bootstrap 5.3: các button liền nhau, border dùng chung, bo góc ở hai đầu, hỗ trợ horizontal/vertical, size, outline, mixed styles, active state, radio/checkbox-style selection và tận dụng BootstrapButton mà bạn đã có. Bootstrap mô tả Button Group chính là việc gom nhiều .btn thành một nhóm ngang hoặc xếp dọc; tài liệu cũng có sizing, outlined styles và vertical variation. 
B
Bootstrap

Mình đề xuất API:

var group = new BootstrapButtonGroup
{
    Orientation = ButtonGroupOrientation.Horizontal,
    Size = BootstrapButtonSize.Medium,
    BorderRadius = 8,
    Spacing = 0
};

group.AddButton("Left");
group.AddButton("Middle");
group.AddButton("Right");


Hiển thị:

┌──────────┬──────────┬──────────┐
│   Left   │  Middle  │  Right   │
└──────────┴──────────┴──────────┘


và:

group.Orientation =
    ButtonGroupOrientation.Vertical;


sẽ thành:

┌──────────────┐
│     Left     │
├──────────────┤
│    Middle    │
├──────────────┤
│    Right     │
└──────────────┘

1. Enum
namespace YourApp.Controls
{
    public enum ButtonGroupOrientation
    {
        Horizontal,
        Vertical
    }

    public enum BootstrapButtonGroupSelection
    {
        None,
        Single,
        Multiple
    }
}


Single tương đương kiểu radio, Multiple tương đương checkbox.

2. BootstrapButtonGroup.cs

Mình giả định BootstrapButton của bạn đã có các property kiểu:

Text
Variant
Size


Nếu tên API của BootstrapButton hiện tại khác thì chỉ cần map lại vài dòng.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapButtonGroup : Panel
    {
        private readonly List<BootstrapButton> _buttons = new();

        private ButtonGroupOrientation _orientation =
            ButtonGroupOrientation.Horizontal;

        private BootstrapButtonGroupSelection _selectionMode =
            BootstrapButtonGroupSelection.None;

        public IReadOnlyList<BootstrapButton> Buttons =>
            _buttons;

        public ButtonGroupOrientation Orientation
        {
            get => _orientation;

            set
            {
                if (_orientation == value)
                    return;

                _orientation = value;

                LayoutButtons();
            }
        }

        public BootstrapButtonGroupSelection SelectionMode
        {
            get => _selectionMode;

            set
            {
                _selectionMode = value;

                UpdateSelectionMode();
            }
        }

        public int Spacing { get; set; } = 0;

        public int BorderRadius { get; set; } =
            AppTheme.BorderRadius;

        public bool EqualWidth { get; set; } = false;

        public BootstrapButtonSize Size { get; set; } =
            BootstrapButtonSize.Medium;

        public BootstrapButtonGroup()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            AutoSize = true;

            BackColor =
                Color.Transparent;

            Padding =
                new Padding(0);

            Margin =
                new Padding(0);
        }

        public BootstrapButton AddButton(
            string text)
        {
            var button =
                new BootstrapButton
                {
                    Text = text,
                    Size = Size
                };

            AddButton(button);

            return button;
        }

        public void AddButton(
            BootstrapButton button)
        {
            if (button == null)
                throw new ArgumentNullException(
                    nameof(button));

            if (_buttons.Contains(button))
                return;

            _buttons.Add(button);

            button.Click +=
                Button_Click;

            Controls.Add(button);

            ApplyButtonStyle(button);

            LayoutButtons();
        }

        public void RemoveButton(
            BootstrapButton button)
        {
            if (!_buttons.Remove(button))
                return;

            button.Click -=
                Button_Click;

            Controls.Remove(button);

            LayoutButtons();
        }

        public void ClearButtons()
        {
            foreach (var button in _buttons)
            {
                button.Click -=
                    Button_Click;
            }

            _buttons.Clear();

            Controls.Clear();

            LayoutButtons();
        }

        private void Button_Click(
            object? sender,
            EventArgs e)
        {
            if (sender is not BootstrapButton button)
                return;

            switch (SelectionMode)
            {
                case BootstrapButtonGroupSelection.Single:
                    SelectSingle(button);
                    break;

                case BootstrapButtonGroupSelection.Multiple:
                    ToggleButton(button);
                    break;
            }
        }

        private void SelectSingle(
            BootstrapButton selected)
        {
            foreach (var button in _buttons)
            {
                SetButtonSelected(
                    button,
                    button == selected);
            }
        }

        private void ToggleButton(
            BootstrapButton button)
        {
            SetButtonSelected(
                button,
                !IsButtonSelected(button));
        }

        private void UpdateSelectionMode()
        {
            if (SelectionMode ==
                BootstrapButtonGroupSelection.None)
            {
                foreach (var button in _buttons)
                {
                    SetButtonSelected(
                        button,
                        false);
                }
            }
        }

        private static bool IsButtonSelected(
            BootstrapButton button)
        {
            return button.Tag is
                BootstrapButtonGroupState state &&
                state.Selected;
        }

        private static void SetButtonSelected(
            BootstrapButton button,
            bool selected)
        {
            if (button.Tag is not
                BootstrapButtonGroupState state)
            {
                state =
                    new BootstrapButtonGroupState();

                button.Tag = state;
            }

            state.Selected = selected;

            ApplySelectedVisual(
                button,
                selected);
        }

        private static void ApplySelectedVisual(
            BootstrapButton button,
            bool selected)
        {
            // Nếu BootstrapButton của bạn đã có
            // property Selected thì dùng trực tiếp:
            //
            // button.Selected = selected;
            //
            // Tạm thời lưu state trong Tag.
        }

        private void ApplyButtonStyle(
            BootstrapButton button)
        {
            button.Size =
                Size;

            // ButtonGroup tự xử lý border-radius.
            //
            // Button ở giữa:
            // radius = 0
            //
            // Button đầu/cuối:
            // radius tương ứng.

            ApplyButtonCorners();
        }

        private void ApplyButtonCorners()
        {
            for (int i = 0;
                 i < _buttons.Count;
                 i++)
            {
                var button =
                    _buttons[i];

                bool first =
                    i == 0;

                bool last =
                    i == _buttons.Count - 1;

                // Các property này sẽ được thêm
                // vào BootstrapButton.
                button.GroupFirst =
                    first;

                button.GroupLast =
                    last;

                button.GroupOrientation =
                    Orientation;

                button.GroupBorderRadius =
                    BorderRadius;
            }
        }

        private void LayoutButtons()
        {
            if (_buttons.Count == 0)
                return;

            ApplyButtonCorners();

            int x = 0;
            int y = 0;

            int maxWidth = 0;
            int maxHeight = 0;

            foreach (var button in _buttons)
            {
                maxWidth =
                    Math.Max(
                        maxWidth,
                        button.PreferredSize.Width);

                maxHeight =
                    Math.Max(
                        maxHeight,
                        button.PreferredSize.Height);
            }

            foreach (var button in _buttons)
            {
                int width =
                    EqualWidth
                        ? maxWidth
                        : button.PreferredSize.Width;

                int height =
                    EqualWidth
                        ? maxHeight
                        : button.PreferredSize.Height;

                button.Size =
                    new Size(
                        Math.Max(1, width),
                        Math.Max(1, height));

                if (Orientation ==
                    ButtonGroupOrientation.Horizontal)
                {
                    button.Location =
                        new Point(x, 0);

                    x +=
                        button.Width +
                        Spacing;
                }
                else
                {
                    button.Location =
                        new Point(0, y);

                    y +=
                        button.Height +
                        Spacing;
                }
            }

            if (Orientation ==
                ButtonGroupOrientation.Horizontal)
            {
                Size =
                    new Size(
                        Math.Max(
                            0,
                            x - Spacing),
                        maxHeight);
            }
            else
            {
                Size =
                    new Size(
                        maxWidth,
                        Math.Max(
                            0,
                            y - Spacing));
            }
        }

        public void Select(
            int index)
        {
            if (index < 0 ||
                index >= _buttons.Count)
                return;

            if (SelectionMode !=
                BootstrapButtonGroupSelection.Single)
                return;

            SelectSingle(
                _buttons[index]);
        }

        public void SelectAll()
        {
            if (SelectionMode !=
                BootstrapButtonGroupSelection.Multiple)
                return;

            foreach (var button in _buttons)
            {
                SetButtonSelected(
                    button,
                    true);
            }
        }

        public void ClearSelection()
        {
            foreach (var button in _buttons)
            {
                SetButtonSelected(
                    button,
                    false);
            }
        }

        public bool IsSelected(
            int index)
        {
            if (index < 0 ||
                index >= _buttons.Count)
                return false;

            return IsButtonSelected(
                _buttons[index]);
        }

        public void RefreshTheme()
        {
            foreach (var button in _buttons)
            {
                ApplyButtonStyle(button);
            }

            Invalidate();
        }

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            LayoutButtons();
        }

        private sealed class
            BootstrapButtonGroupState
        {
            public bool Selected;
        }
    }
}

3. Cần nâng cấp BootstrapButton một chút

Để Button Group thật sự đẹp, mình khuyên không để mỗi button tự vẽ rounded rectangle độc lập.

Thay vào đó BootstrapButton cần biết mình đang nằm trong ButtonGroup:

public bool GroupFirst { get; internal set; }

public bool GroupLast { get; internal set; }

public ButtonGroupOrientation GroupOrientation
{
    get;
    internal set;
}

public int GroupBorderRadius
{
    get;
    internal set;
}


Khi đó:

Horizontal
first                  middle                 last

╭────────────╮┌────────────┐┌────────────╮
│    One     ││    Two     ││   Three    │
╰────────────╯└────────────┘╰────────────╯


Thực tế Bootstrap làm các border liền nhau, nên sẽ không có khoảng trắng giữa các button. Tài liệu Bootstrap cũng minh họa .btn-group với các button nằm sát nhau và có chung đường viền. 
B
Bootstrap

Vertical
╭──────────────╮
│     One      │
├──────────────┤
│     Two      │
├──────────────┤
│    Three     │
╰──────────────╯

4. Mình sẽ sửa phần radius của BootstrapButton như sau

Trong BootstrapButton, thay vì:

DrawRoundedRectangle(...)


hãy cho phép radius từng góc:

public int RadiusTopLeft { get; set; }

public int RadiusTopRight { get; set; }

public int RadiusBottomRight { get; set; }

public int RadiusBottomLeft { get; set; }


Sau đó ButtonGroup áp dụng:

private void ApplyButtonCorners()
{
    for (int i = 0;
         i < _buttons.Count;
         i++)
    {
        var button =
            _buttons[i];

        bool first =
            i == 0;

        bool last =
            i == _buttons.Count - 1;

        if (Orientation ==
            ButtonGroupOrientation.Horizontal)
        {
            button.RadiusTopLeft =
                first ? BorderRadius : 0;

            button.RadiusBottomLeft =
                first ? BorderRadius : 0;

            button.RadiusTopRight =
                last ? BorderRadius : 0;

            button.RadiusBottomRight =
                last ? BorderRadius : 0;
        }
        else
        {
            button.RadiusTopLeft =
                first ? BorderRadius : 0;

            button.RadiusTopRight =
                first ? BorderRadius : 0;

            button.RadiusBottomLeft =
                last ? BorderRadius : 0;

            button.RadiusBottomRight =
                last ? BorderRadius : 0;
        }
    }
}


Đây là cách tốt hơn nhiều so với cố gắng dùng một Region chung.

5. API sẽ khá đẹp
Basic
var group =
    new BootstrapButtonGroup();

group.AddButton("Left");
group.AddButton("Middle");
group.AddButton("Right");

Controls.Add(group);

Primary

Nếu BootstrapButton có Variant:

var group =
    new BootstrapButtonGroup();

var save =
    group.AddButton("Save");

save.Variant =
    BootstrapButtonVariant.Primary;

var cancel =
    group.AddButton("Cancel");

cancel.Variant =
    BootstrapButtonVariant.Secondary;


Bootstrap hỗ trợ việc trộn các style button trong cùng group, ví dụ primary/danger/warning/success. 
B
Bootstrap

6. Outline Button Group

Ví dụ:

var group =
    new BootstrapButtonGroup();

var b1 =
    group.AddButton("Left");

b1.Variant =
    BootstrapButtonVariant.OutlinePrimary;

var b2 =
    group.AddButton("Middle");

b2.Variant =
    BootstrapButtonVariant.OutlinePrimary;

var b3 =
    group.AddButton("Right");

b3.Variant =
    BootstrapButtonVariant.OutlinePrimary;


Kết quả:

┌──────────┬──────────┬──────────┐
│   Left   │  Middle  │  Right   │
└──────────┴──────────┴──────────┘


với background transparent và border #0d6efd.

Bootstrap 5.3 cũng có riêng phần outlined styles cho Button Group. 
B
Bootstrap

7. Small / Medium / Large

Bootstrap có .btn-group-lg, default và .btn-group-sm. 
B
Bootstrap

Mình sẽ cho API:

group.Size =
    BootstrapButtonSize.Small;


hoặc:

group.Size =
    BootstrapButtonSize.Medium;


hoặc:

group.Size =
    BootstrapButtonSize.Large;


Nếu BootstrapButton đã hỗ trợ Size, ButtonGroup chỉ cần propagate:

foreach (var button in _buttons)
{
    button.Size =
        Size;
}

8. Equal width

Đây là extension mình rất khuyên có cho WinForms:

var group =
    new BootstrapButtonGroup
    {
        EqualWidth = true
    };

group.AddButton("Day");
group.AddButton("Month");
group.AddButton("Year");


Kết quả:

┌────────────┬────────────┬────────────┐
│    Day     │   Month    │    Year    │
└────────────┴────────────┴────────────┘


Rất hữu ích cho toolbar/filter.

9. Radio-style Button Group

Đây là phần rất đáng làm.

var group =
    new BootstrapButtonGroup
    {
        SelectionMode =
            BootstrapButtonGroupSelection.Single
    };

group.AddButton("List");
group.AddButton("Grid");
group.AddButton("Kanban");


Khi click:

┌──────────┬──────────┬──────────┐
│  List    │  Grid ✓  │  Kanban  │
└──────────┴──────────┴──────────┘


Bạn có thể lấy:

group.Select(1);


hoặc:

if (group.IsSelected(1))
{
    // Grid
}


Bootstrap cũng hỗ trợ radio toggle button trong button groups. 
B
Bootstrap

10. Multiple selection
var group =
    new BootstrapButtonGroup
    {
        SelectionMode =
            BootstrapButtonGroupSelection.Multiple
    };

group.AddButton("Bold");
group.AddButton("Italic");
group.AddButton("Underline");


Kết quả:

┌──────────┬──────────┬──────────┐
│ Bold  ✓  │ Italic   │ Under ✓  │
└──────────┴──────────┴──────────┘


Tương tự checkbox button group của Bootstrap. 
B
Bootstrap

11. Vertical
var group =
    new BootstrapButtonGroup
    {
        Orientation =
            ButtonGroupOrientation.Vertical
    };

group.AddButton("Dashboard");
group.AddButton("Users");
group.AddButton("Products");
group.AddButton("Reports");


Bootstrap cũng có .btn-group-vertical cho trường hợp này. 
B
Bootstrap

12. Mình còn muốn thêm BootstrapButtonToolbar

Vì Bootstrap Button Group thực tế có luôn khái niệm Button Toolbar, tức nhiều Button Group nằm cạnh nhau. 
B
Bootstrap

API có thể là:

var toolbar =
    new BootstrapButtonToolbar();

var fileGroup =
    toolbar.AddGroup();

fileGroup.AddButton("New");
fileGroup.AddButton("Open");
fileGroup.AddButton("Save");

var editGroup =
    toolbar.AddGroup();

editGroup.AddButton("Cut");
editGroup.AddButton("Copy");
editGroup.AddButton("Paste");

Controls.Add(toolbar);


Hiển thị:

┌──────┬──────┬──────┐     ┌──────┬──────┬──────┐
│ New  │ Open │ Save │     │ Cut  │ Copy │Paste │
└──────┴──────┴──────┘     └──────┴──────┴──────┘


Đây sẽ là control rất hữu ích cho ứng dụng WinForms kiểu Admin/ERP.

Kiến trúc mình đề xuất
BootstrapButton
      │
      ├── BootstrapButtonGroup
      │       ├── Button
      │       ├── Button
      │       └── Button
      │
      └── BootstrapButtonToolbar
              │
              ├── ButtonGroup
              ├── ButtonGroup
              └── ButtonGroup


Như vậy sau Accordion, bộ component của bạn bắt đầu có kiến trúc khá giống Bootstrap nhưng vẫn native WinForms, không cần nhúng WebView hay HTML/CSS.

----

> Tạo BootstrapButtonToolbar

Được. Mình khuyên BootstrapButtonToolbar nên là container điều phối nhiều BootstrapButtonGroup, tương tự Bootstrap 5.3 .btn-toolbar: hỗ trợ horizontal/vertical, spacing giữa các group, căn giữa theo chiều dọc, AutoSize, dark/light theme và có thể dùng trực tiếp với BootstrapButtonGroup vừa xây dựng.

Dưới đây là bản hoàn chỉnh.

1. BootstrapButtonToolbar.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    /// <summary>
    /// Bootstrap-like Button Toolbar.
    ///
    /// Example:
    ///
    /// var toolbar = new BootstrapButtonToolbar();
    ///
    /// var file = toolbar.AddGroup();
    /// file.AddButton("New");
    /// file.AddButton("Open");
    /// file.AddButton("Save");
    ///
    /// var edit = toolbar.AddGroup();
    /// edit.AddButton("Cut");
    /// edit.AddButton("Copy");
    /// edit.AddButton("Paste");
    /// </summary>
    public class BootstrapButtonToolbar : Panel
    {
        private readonly List<BootstrapButtonGroup> _groups = new();

        private ButtonGroupOrientation _orientation =
            ButtonGroupOrientation.Horizontal;

        private int _groupSpacing = 8;

        public IReadOnlyList<BootstrapButtonGroup> Groups =>
            _groups;

        public ButtonGroupOrientation Orientation
        {
            get => _orientation;

            set
            {
                if (_orientation == value)
                    return;

                _orientation = value;

                ApplyOrientation();
                PerformLayout();
            }
        }

        /// <summary>
        /// Spacing between ButtonGroups.
        /// </summary>
        public int GroupSpacing
        {
            get => _groupSpacing;

            set
            {
                _groupSpacing =
                    Math.Max(0, value);

                PerformLayout();
            }
        }

        /// <summary>
        /// Automatically calculate toolbar size.
        /// </summary>
        public bool AutoSizeToolbar { get; set; } = true;

        /// <summary>
        /// Vertically centers horizontal groups.
        /// </summary>
        public bool CenterVertically { get; set; } = true;

        /// <summary>
        /// Horizontally centers vertical groups.
        /// </summary>
        public bool CenterHorizontally { get; set; } = true;

        public BootstrapButtonToolbar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            AutoSize = true;

            BackColor =
                Color.Transparent;

            Padding =
                new Padding(0);

            Margin =
                new Padding(0);
        }

        #region Add / Remove

        public BootstrapButtonGroup AddGroup()
        {
            var group =
                new BootstrapButtonGroup();

            AddGroup(group);

            return group;
        }

        public BootstrapButtonGroup AddGroup(
            params BootstrapButton[] buttons)
        {
            var group =
                new BootstrapButtonGroup();

            foreach (var button in buttons)
            {
                group.AddButton(button);
            }

            AddGroup(group);

            return group;
        }

        public void AddGroup(
            BootstrapButtonGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(
                    nameof(group));

            if (_groups.Contains(group))
                return;

            _groups.Add(group);

            Controls.Add(group);

            ApplyGroupStyle(group);

            PerformLayout();
        }

        public void RemoveGroup(
            BootstrapButtonGroup group)
        {
            if (!_groups.Remove(group))
                return;

            Controls.Remove(group);

            PerformLayout();
        }

        public void ClearGroups()
        {
            _groups.Clear();

            Controls.Clear();

            PerformLayout();
        }

        #endregion

        #region Group Style

        private void ApplyGroupStyle(
            BootstrapButtonGroup group)
        {
            group.Orientation =
                Orientation;

            group.Margin =
                new Padding(0);

            group.Anchor =
                AnchorStyles.None;
        }

        private void ApplyOrientation()
        {
            foreach (var group in _groups)
            {
                group.Orientation =
                    Orientation;
            }
        }

        #endregion

        #region Layout

        protected override void OnLayout(
            LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            LayoutGroups();
        }

        private void LayoutGroups()
        {
            if (_groups.Count == 0)
            {
                if (AutoSizeToolbar)
                {
                    Size =
                        Padding.Size;
                }

                return;
            }

            if (Orientation ==
                ButtonGroupOrientation.Horizontal)
            {
                LayoutHorizontal();
            }
            else
            {
                LayoutVertical();
            }
        }

        private void LayoutHorizontal()
        {
            int availableHeight =
                ClientSize.Height -
                Padding.Vertical;

            int maxHeight = 0;

            foreach (var group in _groups)
            {
                maxHeight =
                    Math.Max(
                        maxHeight,
                        group.Height);
            }

            int x =
                Padding.Left;

            foreach (var group in _groups)
            {
                int y;

                if (CenterVertically)
                {
                    y =
                        Padding.Top +
                        Math.Max(
                            0,
                            (availableHeight -
                             group.Height) / 2);
                }
                else
                {
                    y =
                        Padding.Top;
                }

                group.Location =
                    new Point(
                        x,
                        y);

                x +=
                    group.Width +
                    GroupSpacing;
            }

            int width =
                Math.Max(
                    0,
                    x -
                    GroupSpacing +
                    Padding.Right);

            int height =
                maxHeight +
                Padding.Vertical;

            if (AutoSizeToolbar)
            {
                Size =
                    new Size(
                        width,
                        height);
            }
        }

        private void LayoutVertical()
        {
            int availableWidth =
                ClientSize.Width -
                Padding.Horizontal;

            int maxWidth = 0;

            foreach (var group in _groups)
            {
                maxWidth =
                    Math.Max(
                        maxWidth,
                        group.Width);
            }

            int y =
                Padding.Top;

            foreach (var group in _groups)
            {
                int x;

                if (CenterHorizontally)
                {
                    x =
                        Padding.Left +
                        Math.Max(
                            0,
                            (availableWidth -
                             group.Width) / 2);
                }
                else
                {
                    x =
                        Padding.Left;
                }

                group.Location =
                    new Point(
                        x,
                        y);

                y +=
                    group.Height +
                    GroupSpacing;
            }

            int width =
                maxWidth +
                Padding.Horizontal;

            int height =
                Math.Max(
                    0,
                    y -
                    GroupSpacing +
                    Padding.Bottom);

            if (AutoSizeToolbar)
            {
                Size =
                    new Size(
                        width,
                        height);
            }
        }

        #endregion

        #region Helpers

        public BootstrapButtonGroup? GetGroup(
            int index)
        {
            if (index < 0 ||
                index >= _groups.Count)
            {
                return null;
            }

            return _groups[index];
        }

        public BootstrapButton? GetButton(
            int groupIndex,
            int buttonIndex)
        {
            var group =
                GetGroup(groupIndex);

            if (group == null)
                return null;

            if (buttonIndex < 0 ||
                buttonIndex >= group.Buttons.Count)
            {
                return null;
            }

            return group.Buttons[buttonIndex];
        }

        public void RefreshTheme()
        {
            BackColor =
                Color.Transparent;

            foreach (var group in _groups)
            {
                group.RefreshTheme();
            }

            Invalidate();
        }

        #endregion

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            PerformLayout();
        }
    }
}

2. Cách sử dụng cơ bản
var toolbar =
    new BootstrapButtonToolbar();

var fileGroup =
    toolbar.AddGroup();

fileGroup.AddButton("New");
fileGroup.AddButton("Open");
fileGroup.AddButton("Save");

var editGroup =
    toolbar.AddGroup();

editGroup.AddButton("Cut");
editGroup.AddButton("Copy");
editGroup.AddButton("Paste");

Controls.Add(toolbar);


Giao diện:

┌────────┬────────┬────────┐    ┌────────┬────────┬────────┐
│  New   │  Open  │  Save  │    │  Cut   │  Copy  │ Paste  │
└────────┴────────┴────────┘    └────────┴────────┴────────┘


Khoảng cách giữa button trong cùng group là 0, còn khoảng cách giữa các group là 8px.

Đây chính là sự khác biệt quan trọng giữa:

ButtonGroup
    Button + Button + Button


và:

ButtonToolbar
    ButtonGroup     ButtonGroup     ButtonGroup

3. Chỉnh khoảng cách
var toolbar =
    new BootstrapButtonToolbar
    {
        GroupSpacing = 12
    };


Ví dụ:

┌────────┬────────┐    ┌────────┬────────┐    ┌────────┬────────┐
│  New   │  Open  │    │  Cut   │  Copy  │    │  Undo  │  Redo  │
└────────┴────────┘    └────────┴────────┘    └────────┴────────┘
          12px                   12px

4. Kết hợp với BootstrapButton có Variant

Ví dụ toolbar của một màn hình CRUD:

var toolbar =
    new BootstrapButtonToolbar
    {
        GroupSpacing = 8
    };

var crud =
    toolbar.AddGroup();

var add =
    crud.AddButton("Thêm");

add.Variant =
    BootstrapButtonVariant.Success;

var edit =
    crud.AddButton("Sửa");

edit.Variant =
    BootstrapButtonVariant.Primary;

var delete =
    crud.AddButton("Xóa");

delete.Variant =
    BootstrapButtonVariant.Danger;


Group thứ hai:

var navigation =
    toolbar.AddGroup();

navigation.AddButton("Refresh");
navigation.AddButton("Export");


Thành:

┌────────┬────────┬────────┐    ┌───────────┬────────┐
│  Thêm   │  Sửa   │  Xóa   │    │  Refresh  │ Export │
└────────┴────────┴────────┘    └───────────┴────────┘

5. Toolbar dọc
var toolbar =
    new BootstrapButtonToolbar
    {
        Orientation =
            ButtonGroupOrientation.Vertical,

        GroupSpacing = 10
    };


Kết quả:

┌────────┬────────┐
│  New   │  Open  │
└────────┴────────┘
        │
        10px
        │
┌────────┬────────┐
│  Cut   │  Copy  │
└────────┴────────┘
        │
        10px
        │
┌────────┬────────┐
│  Undo  │  Redo  │
└────────┴────────┘

6. Toolbar cho màn hình CRUD

Đây là cách mình nghĩ bạn sẽ dùng nhiều nhất:

private BootstrapButtonToolbar CreateCrudToolbar()
{
    var toolbar =
        new BootstrapButtonToolbar
        {
            GroupSpacing = 8,
            Padding = new Padding(0, 0, 0, 12)
        };

    // CRUD
    var crud =
        toolbar.AddGroup();

    var add =
        crud.AddButton("Thêm");

    add.Variant =
        BootstrapButtonVariant.Success;

    var edit =
        crud.AddButton("Sửa");

    edit.Variant =
        BootstrapButtonVariant.Primary;

    var delete =
        crud.AddButton("Xóa");

    delete.Variant =
        BootstrapButtonVariant.Danger;

    // Data
    var data =
        toolbar.AddGroup();

    data.AddButton("Refresh");
    data.AddButton("Export");

    // Search
    var search =
        toolbar.AddGroup();

    search.AddButton("Tìm kiếm");

    return toolbar;
}

7. Kết hợp với Selection Mode của ButtonGroup

Điểm này rất hữu ích cho toolbar.

Ví dụ chọn kiểu hiển thị:

var viewGroup =
    toolbar.AddGroup();

viewGroup.SelectionMode =
    BootstrapButtonGroupSelection.Single;

viewGroup.AddButton("List");
viewGroup.AddButton("Grid");
viewGroup.AddButton("Card");

viewGroup.Select(0);


Kết quả:

┌────────┬────────┬────────┐
│ List ✓ │  Grid  │  Card  │
└────────┴────────┴────────┘


Toolbar hoàn toàn không cần biết logic selection. Nó chỉ quản lý layout; BootstrapButtonGroup quản lý selection.

Đây là separation mình muốn giữ:

BootstrapButton
       │
       ▼
BootstrapButtonGroup
       │
       │ selection
       │ border
       │ radius
       │ orientation
       ▼
BootstrapButtonToolbar
       │
       │ layout
       │ spacing
       │ alignment
       ▼
      Form

8. Một nâng cấp mình khuyên làm ngay

Với bộ control này, mình sẽ không dùng Tag để lưu Selected lâu dài như bản ButtonGroup trước. Nên nâng BootstrapButton lên có state chính thức:

public bool Selected
{
    get => _selected;
    set
    {
        if (_selected == value)
            return;

        _selected = value;

        Invalidate();

        SelectedChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}

private bool _selected;

public event EventHandler?
    SelectedChanged;


Sau đó BootstrapButtonGroup chỉ cần:

button.Selected = true;


thay vì:

button.Tag = ...


Điều này sẽ làm API sạch hơn rất nhiều.

9. Và nên bổ sung Alignment

Một toolbar thực tế thường cần:

[New][Edit][Delete]                         [Refresh][Export]


Do đó phiên bản tiếp theo mình khuyên thêm:

public enum BootstrapToolbarAlignment
{
    Left,
    Center,
    Right,
    SpaceBetween
}


Đặc biệt SpaceBetween sẽ rất hữu ích:

┌──────────────────────────────────────────────────────────────┐
│ [New][Edit][Delete]                         [Refresh][Export] │
└──────────────────────────────────────────────────────────────┘


Đây là pattern cực kỳ phổ biến trong màn hình quản trị.

Nếu xây cả bộ framework của bạn, mình sẽ để kiến trúc cuối cùng là:

BootstrapButton
    │
    ├── BootstrapButtonGroup
    │       └── BootstrapButton
    │
    └── BootstrapButtonToolbar
            ├── BootstrapButtonGroup
            │       ├── Button
            │       └── Button
            │
            ├── BootstrapButtonGroup
            │       ├── Button
            │       └── Button
            │
            └── BootstrapButtonGroup
                    ├── Button
                    └── Button


và Button/Group/Toolbar dùng chung AppTheme, nên khi chuyển Light ↔ Dark chỉ cần refresh theme ở root container thay vì phải xử lý từng button thủ công.

----

> Thêm spinners

Được. Với BootstrapSpinner, mình đề xuất làm native WinForms custom control, không dùng GIF. Như vậy spinner sẽ nét ở DPI cao, đổi màu theo AppTheme, có Size, Variant, Type và animation duration/frame riêng.

Bootstrap 5.3 có hai nhóm spinner chính: border spinner và growing spinner, cùng các màu contextual và kích thước nhỏ (spinner-border-sm, spinner-grow-sm).

1. API

Mình đề xuất API như sau:

var spinner = new BootstrapSpinner
{
    Type = BootstrapSpinnerType.Border,
    Variant = BootstrapSpinnerVariant.Primary,
    Size = BootstrapSpinnerSize.Medium,

    AnimationDuration = 750,

    Visible = true
};

Controls.Add(spinner);


Hoặc:

var spinner = new BootstrapSpinner
{
    Type = BootstrapSpinnerType.Grow,
    Variant = BootstrapSpinnerVariant.Success,
    Size = BootstrapSpinnerSize.Small
};

2. Enum

Tạo BootstrapSpinnerEnums.cs:

namespace YourApp.Controls
{
    public enum BootstrapSpinnerType
    {
        Border,
        Grow
    }

    public enum BootstrapSpinnerSize
    {
        Small,
        Medium,
        Large
    }

    public enum BootstrapSpinnerVariant
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        Light,
        Dark,
        Custom
    }
}

3. BootstrapSpinner.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapSpinner : Control
    {
        private readonly Timer _animationTimer;

        private float _animationProgress;

        private BootstrapSpinnerType _type =
            BootstrapSpinnerType.Border;

        private BootstrapSpinnerSize _size =
            BootstrapSpinnerSize.Medium;

        private BootstrapSpinnerVariant _variant =
            BootstrapSpinnerVariant.Primary;

        private int _animationDuration = 750;

        private Color _customColor =
            AppTheme.Colors.Primary;

        private bool _spinning = true;

        public BootstrapSpinner()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor =
                Color.Transparent;

            Size =
                new Size(32, 32);

            TabStop = false;

            _animationTimer =
                new Timer
                {
                    Interval = 16
                };

            _animationTimer.Tick +=
                AnimationTimer_Tick;

            UpdateTimer();
        }

        #region Properties

        public BootstrapSpinnerType Type
        {
            get => _type;

            set
            {
                if (_type == value)
                    return;

                _type = value;

                UpdateDefaultSize();

                Invalidate();
            }
        }

        public BootstrapSpinnerSize SizeMode
        {
            get => _size;

            set
            {
                if (_size == value)
                    return;

                _size = value;

                UpdateDefaultSize();

                Invalidate();
            }
        }

        public BootstrapSpinnerVariant Variant
        {
            get => _variant;

            set
            {
                if (_variant == value)
                    return;

                _variant = value;

                Invalidate();
            }
        }

        public Color CustomColor
        {
            get => _customColor;

            set
            {
                _customColor = value;

                Invalidate();
            }
        }

        /// <summary>
        /// Animation duration in milliseconds.
        /// Bootstrap default is approximately 750ms.
        /// </summary>
        public int AnimationDuration
        {
            get => _animationDuration;

            set
            {
                _animationDuration =
                    Math.Max(50, value);

                Invalidate();
            }
        }

        public bool Spinning
        {
            get => _spinning;

            set
            {
                if (_spinning == value)
                    return;

                _spinning = value;

                UpdateTimer();
            }
        }

        #endregion

        #region Compatibility aliases

        /// <summary>
        /// More natural API:
        /// spinner.Size = ...
        ///
        /// Because Control.Size already exists,
        /// SizeMode is used for spinner size.
        /// </summary>
        public BootstrapSpinnerSize SpinnerSize
        {
            get => SizeMode;
            set => SizeMode = value;
        }

        #endregion

        #region Animation

        private void AnimationTimer_Tick(
            object? sender,
            EventArgs e)
        {
            float delta =
                16f /
                Math.Max(
                    50,
                    AnimationDuration);

            _animationProgress +=
                delta;

            if (_animationProgress >= 1f)
            {
                _animationProgress -=
                    1f;
            }

            Invalidate();
        }

        private void UpdateTimer()
        {
            if (_spinning &&
                Visible &&
                IsHandleCreated)
            {
                _animationTimer.Start();
            }
            else
            {
                _animationTimer.Stop();
            }
        }

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateTimer();
        }

        protected override void OnHandleCreated(
            EventArgs e)
        {
            base.OnHandleCreated(e);

            UpdateTimer();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Stop();

                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Paint

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            g.PixelOffsetMode =
                PixelOffsetMode.HighQuality;

            RectangleF bounds =
                GetSpinnerBounds();

            Color color =
                GetSpinnerColor();

            if (Type ==
                BootstrapSpinnerType.Border)
            {
                DrawBorderSpinner(
                    g,
                    bounds,
                    color);
            }
            else
            {
                DrawGrowSpinner(
                    g,
                    bounds,
                    color);
            }
        }

        private RectangleF GetSpinnerBounds()
        {
            int size =
                Math.Min(
                    ClientSize.Width,
                    ClientSize.Height);

            float x =
                (ClientSize.Width - size) / 2f;

            float y =
                (ClientSize.Height - size) / 2f;

            return new RectangleF(
                x,
                y,
                size,
                size);
        }

        private void DrawBorderSpinner(
            Graphics g,
            RectangleF bounds,
            Color color)
        {
            float thickness =
                Math.Max(
                    2f,
                    bounds.Width / 8f);

            RectangleF arcBounds =
                new RectangleF(
                    bounds.X +
                    thickness / 2f,

                    bounds.Y +
                    thickness / 2f,

                    bounds.Width -
                    thickness,

                    bounds.Height -
                    thickness);

            using var pen =
                new Pen(
                    color,
                    thickness);

            pen.StartCap =
                LineCap.Round;

            pen.EndCap =
                LineCap.Round;

            float startAngle =
                -90f +
                _animationProgress * 360f;

            // Bootstrap-like rotating ring.
            g.DrawArc(
                pen,
                arcBounds,
                startAngle,
                300f);
        }

        private void DrawGrowSpinner(
            Graphics g,
            RectangleF bounds,
            Color color)
        {
            float progress =
                _animationProgress;

            // Two-phase pulse:
            //
            // 0 -> 0.5 : grow
            // 0.5 -> 1 : shrink

            float scale;

            if (progress < 0.5f)
            {
                float t =
                    progress / 0.5f;

                scale =
                    0.25f +
                    0.75f *
                    EaseOutCubic(t);
            }
            else
            {
                float t =
                    (progress - 0.5f) / 0.5f;

                scale =
                    1f -
                    0.75f *
                    EaseInCubic(t);
            }

            float diameter =
                bounds.Width *
                scale;

            float x =
                bounds.X +
                (bounds.Width -
                 diameter) / 2f;

            float y =
                bounds.Y +
                (bounds.Height -
                 diameter) / 2f;

            Color drawColor =
                Color.FromArgb(
                    (int)(
                        255 *
                        GetGrowOpacity(progress)),
                    color);

            using var brush =
                new SolidBrush(
                    drawColor);

            g.FillEllipse(
                brush,
                x,
                y,
                diameter,
                diameter);
        }

        private static float EaseOutCubic(
            float t)
        {
            float inv =
                1f - t;

            return 1f -
                   inv * inv * inv;
        }

        private static float EaseInCubic(
            float t)
        {
            return t * t * t;
        }

        private static float
            GetGrowOpacity(
                float progress)
        {
            if (progress < 0.5f)
            {
                return
                    0.65f +
                    0.35f *
                    (progress / 0.5f);
            }

            return
                1f -
                0.35f *
                ((progress - 0.5f) / 0.5f);
        }

        #endregion

        #region Theme

        private Color GetSpinnerColor()
        {
            if (Variant ==
                BootstrapSpinnerVariant.Custom)
            {
                return CustomColor;
            }

            var colors =
                AppTheme.Colors;

            return Variant switch
            {
                BootstrapSpinnerVariant.Primary =>
                    colors.Primary,

                BootstrapSpinnerVariant.Secondary =>
                    colors.Secondary,

                BootstrapSpinnerVariant.Success =>
                    colors.Success,

                BootstrapSpinnerVariant.Danger =>
                    colors.Danger,

                BootstrapSpinnerVariant.Warning =>
                    colors.Warning,

                BootstrapSpinnerVariant.Info =>
                    colors.Info,

                BootstrapSpinnerVariant.Light =>
                    colors.Light,

                BootstrapSpinnerVariant.Dark =>
                    colors.Dark,

                _ =>
                    colors.Primary
            };
        }

        public void RefreshTheme()
        {
            Invalidate();
        }

        #endregion

        #region Size

        private void UpdateDefaultSize()
        {
            int size =
                SizeMode switch
                {
                    BootstrapSpinnerSize.Small =>
                        20,

                    BootstrapSpinnerSize.Medium =>
                        32,

                    BootstrapSpinnerSize.Large =>
                        48,

                    _ =>
                        32
                };

            // Chỉ tự đổi size nếu control
            // đang dùng kích thước mặc định.
            if (Width <= 48 &&
                Height <= 48)
            {
                Size =
                    new Size(
                        size,
                        size);
            }
        }

        #endregion

        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            Invalidate();
        }
    }
}

4. Cách dùng
Border spinner
var spinner =
    new BootstrapSpinner
    {
        SpinnerSize =
            BootstrapSpinnerSize.Medium,

        Variant =
            BootstrapSpinnerVariant.Primary,

        Type =
            BootstrapSpinnerType.Border
    };

Small
var spinner =
    new BootstrapSpinner
    {
        SpinnerSize =
            BootstrapSpinnerSize.Small
    };


Bootstrap cũng cung cấp kích thước nhỏ cho cả border và grow spinner.

Grow
var spinner =
    new BootstrapSpinner
    {
        Type =
            BootstrapSpinnerType.Grow,

        Variant =
            BootstrapSpinnerVariant.Success
    };

5. Các màu Bootstrap

Bạn có thể tạo nhanh:

new BootstrapSpinner
{
    Variant =
        BootstrapSpinnerVariant.Primary
};


Các variant:

Primary
Secondary
Success
Danger
Warning
Info
Light
Dark
Custom


Ví dụ:

var dangerSpinner =
    new BootstrapSpinner
    {
        Variant =
            BootstrapSpinnerVariant.Danger
    };

6. Custom color

Không bị giới hạn bởi palette:

var spinner =
    new BootstrapSpinner
    {
        Variant =
            BootstrapSpinnerVariant.Custom,

        CustomColor =
            Color.FromArgb(
                139,
                92,
                246)
    };


Hoặc nếu bạn có palette riêng:

spinner.CustomColor =
    ColorTranslator.FromHtml("#8b5cf6");

7. Điều khiển animation

Bạn có thể thay đổi tốc độ:

spinner.AnimationDuration = 500;


Nhanh hơn:

AnimationDuration = 500
       ↓
    nhanh


Chậm:

spinner.AnimationDuration = 1500;


Bootstrap mặc định sử dụng animation khoảng 0.75s, nên 750ms là giá trị hợp lý để làm default.

Dừng:

spinner.Spinning = false;


Chạy lại:

spinner.Spinning = true;

8. Overlay loading

Một pattern rất hữu ích với WinForms là đặt spinner vào BootstrapCard hoặc một panel overlay:

var overlay =
    new Panel
    {
        Dock = DockStyle.Fill,
        BackColor =
            Color.FromArgb(
                120,
                255,
                255,
                255)
    };

var spinner =
    new BootstrapSpinner
    {
        SpinnerSize =
            BootstrapSpinnerSize.Large,

        Variant =
            BootstrapSpinnerVariant.Primary,

        Anchor =
            AnchorStyles.None
    };

overlay.Controls.Add(spinner);


Sau đó khi loading:

overlay.Visible = true;


và hoàn thành:

overlay.Visible = false;

9. Loading trong BootstrapButton

Control này còn rất hợp để tích hợp vào BootstrapButton.

Ví dụ API:

button.Loading = true;


Button tự chuyển thành:

┌─────────────────────────┐
│       ◌  Saving...      │
└─────────────────────────┘


và:

button.Loading = false;


trở lại:

┌─────────────────────────┐
│          Save           │
└─────────────────────────┘


Mình đặc biệt khuyên làm theo hướng này thay vì mỗi Button tự vẽ spinner, tức BootstrapButton compose BootstrapSpinner.

10. Một điểm nên sửa trong BootstrapButton

Để spinner dùng được tốt trong Button, thêm:

public bool Loading { get; set; }

public string LoadingText { get; set; } = "Loading...";

public BootstrapSpinner? LoadingSpinner { get; }


Khi:

button.Loading = true;


thì:

BootstrapButton
┌─────────────────────────────┐
│  BootstrapSpinner  Loading  │
└─────────────────────────────┘


Button cũng nên tự:

disable click khi loading
giữ nguyên kích thước
căn spinner + text
hỗ trợ Light/Dark
lấy màu spinner theo button variant
hỗ trợ LoadingText = null để chỉ hiện spinner

Ví dụ:

var button =
    new BootstrapButton
    {
        Text = "Save",
        Variant =
            BootstrapButtonVariant.Primary,
        LoadingText = "Saving..."
    };

button.Loading = true;


Đây sẽ làm bộ control của bạn nhất quán hơn rất nhiều.

Kiến trúc hiện tại

Sau khi thêm spinner, bộ UI của bạn bắt đầu có một cấu trúc khá đẹp:

AppTheme
   │
   ├── BootstrapButton
   │       └── BootstrapSpinner
   │
   ├── BootstrapButtonGroup
   │       └── BootstrapButton
   │
   ├── BootstrapButtonToolbar
   │       └── BootstrapButtonGroup
   │
   ├── BootstrapCollapse
   │       └── BootstrapAccordion
   │
   └── BootstrapSpinner


Điểm mình sẽ ưu tiên ở bước tiếp theo: nâng BootstrapSpinner thành một animation primitive dùng chung, sau đó tích hợp Loading vào BootstrapButton và tạo thêm BootstrapProgressBar. Khi đó các màn hình CRUD của bạn có thể có loading UX khá giống Bootstrap nhưng vẫn hoàn toàn native WinForms.

----

> Tiếp tục giúp tôi phần: "nâng BootstrapSpinner thành một animation primitive dùng chung, sau đó tích hợp Loading vào BootstrapButton và tạo thêm BootstrapProgressBar"

Được. Ở bước này mình sẽ chỉnh kiến trúc một chút để animation không còn nằm riêng trong BootstrapSpinner.

Mục tiêu:

BootstrapAnimation
       │
       ├── BootstrapSpinner
       │
       ├── BootstrapButton.Loading
       │
       └── BootstrapProgressBar
              ├── Determinate
              └── Indeterminate


Như vậy sau này bạn có thể dùng cùng animation engine cho Collapse, Accordion, toast, skeleton loading... mà không phải tạo Timer riêng cho từng control.

Bootstrap 5.3 cũng có progress bar với màu contextual, label, striped/animated và nhiều progress bar trong cùng một container.

1. BootstrapAnimation.cs

Đây là animation primitive dùng chung.

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace YourApp.Animation
{
    public enum BootstrapEasing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut
    }

    /// <summary>
    /// Lightweight UI animation primitive for WinForms.
    /// Runs entirely on the UI thread.
    /// </summary>
    public sealed class BootstrapAnimation : IDisposable
    {
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch;

        private bool _disposed;
        private bool _running;

        private double _duration;
        private double _from;
        private double _to;

        public event EventHandler<
            BootstrapAnimationEventArgs>? ProgressChanged;

        public event EventHandler? Completed;

        public double Progress { get; private set; }

        public bool IsRunning =>
            _running;

        public BootstrapEasing Easing { get; set; } =
            BootstrapEasing.Linear;

        public BootstrapAnimation(
            int interval = 16)
        {
            _timer =
                new Timer
                {
                    Interval =
                        Math.Max(
                            1,
                            interval)
                };

            _timer.Tick +=
                Timer_Tick;

            _stopwatch =
                new Stopwatch();
        }

        public void Start(
            double from,
            double to,
            int duration)
        {
            if (_disposed)
                return;

            _from = from;
            _to = to;

            _duration =
                Math.Max(
                    1,
                    duration);

            Progress =
                from;

            _stopwatch.Restart();

            _running = true;

            _timer.Start();

            RaiseProgress();
        }

        public void Stop()
        {
            if (!_running)
                return;

            _running = false;

            _timer.Stop();
            _stopwatch.Stop();
        }

        public void Cancel()
        {
            Stop();
        }

        public void Restart(
            double from,
            double to,
            int duration)
        {
            Stop();

            Start(
                from,
                to,
                duration);
        }

        private void Timer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!_running)
                return;

            double elapsed =
                _stopwatch.Elapsed.TotalMilliseconds;

            double t =
                Math.Clamp(
                    elapsed / _duration,
                    0.0,
                    1.0);

            double eased =
                ApplyEasing(
                    t,
                    Easing);

            Progress =
                _from +
                (_to - _from) *
                eased;

            RaiseProgress();

            if (t >= 1.0)
            {
                Progress =
                    _to;

                _running = false;

                _timer.Stop();
                _stopwatch.Stop();

                RaiseProgress();

                Completed?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private void RaiseProgress()
        {
            ProgressChanged?.Invoke(
                this,
                new BootstrapAnimationEventArgs(
                    Progress));
        }

        public static double ApplyEasing(
            double t,
            BootstrapEasing easing)
        {
            t =
                Math.Clamp(
                    t,
                    0.0,
                    1.0);

            return easing switch
            {
                BootstrapEasing.Linear =>
                    t,

                BootstrapEasing.EaseIn =>
                    t * t * t,

                BootstrapEasing.EaseOut =>
                    1.0 -
                    Math.Pow(
                        1.0 - t,
                        3),

                BootstrapEasing.EaseInOut =>
                    t < 0.5
                        ? 4 * t * t * t
                        : 1 -
                          Math.Pow(
                              -2 * t + 2,
                              3) / 2,

                _ => t
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            _timer.Dispose();
            _stopwatch.Dispose();
        }
    }

    public sealed class BootstrapAnimationEventArgs
        : EventArgs
    {
        public double Progress { get; }

        public BootstrapAnimationEventArgs(
            double progress)
        {
            Progress = progress;
        }
    }
}


Điểm quan trọng là animation này không biết gì về UI. Nó chỉ phát:

0 → 1


theo duration/easing.

2. Tạo BootstrapLoopAnimation

Spinner và indeterminate progress không phải animation 0 → 1 một lần. Chúng chạy vòng lặp.

Ta tạo primitive thứ hai:

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace YourApp.Animation
{
    public sealed class BootstrapLoopAnimation : IDisposable
    {
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch;

        private bool _running;
        private bool _disposed;

        public double Progress { get; private set; }

        public int Duration { get; set; } = 750;

        public event EventHandler<
            BootstrapAnimationEventArgs>? ProgressChanged;

        public BootstrapLoopAnimation(
            int interval = 16)
        {
            _timer =
                new Timer
                {
                    Interval =
                        Math.Max(1, interval)
                };

            _timer.Tick +=
                Timer_Tick;

            _stopwatch =
                new Stopwatch();
        }

        public void Start()
        {
            if (_disposed ||
                _running)
                return;

            _running = true;

            _stopwatch.Restart();

            _timer.Start();
        }

        public void Stop()
        {
            if (!_running)
                return;

            _running = false;

            _timer.Stop();
            _stopwatch.Stop();
        }

        private void Timer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!_running)
                return;

            double duration =
                Math.Max(
                    1,
                    Duration);

            double progress =
                (_stopwatch.Elapsed.TotalMilliseconds %
                 duration) /
                duration;

            Progress =
                progress;

            ProgressChanged?.Invoke(
                this,
                new BootstrapAnimationEventArgs(
                    progress));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            _timer.Dispose();
            _stopwatch.Dispose();
        }
    }
}


Bây giờ:

BootstrapAnimation
    0 ───────────────► 1

BootstrapLoopAnimation
    0 → 1 → 0 → 1 → 0 → ...

3. Nâng cấp BootstrapSpinner

Spinner bây giờ không cần tự quản lý Timer nữa.

Phần field:

private readonly BootstrapLoopAnimation _animation;


constructor:

public BootstrapSpinner()
{
    SetStyle(
        ControlStyles.UserPaint |
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.OptimizedDoubleBuffer |
        ControlStyles.ResizeRedraw |
        ControlStyles.SupportsTransparentBackColor,
        true);

    BackColor =
        Color.Transparent;

    Size =
        new Size(32, 32);

    _animation =
        new BootstrapLoopAnimation();

    _animation.ProgressChanged +=
        Animation_ProgressChanged;

    UpdateAnimation();
}


Animation:

private void Animation_ProgressChanged(
    object? sender,
    BootstrapAnimationEventArgs e)
{
    Invalidate();
}


Property:

public int AnimationDuration
{
    get => _animation.Duration;

    set
    {
        _animation.Duration =
            Math.Max(
                50,
                value);
    }
}


Và:

public bool Spinning
{
    get => _spinning;

    set
    {
        if (_spinning == value)
            return;

        _spinning = value;

        UpdateAnimation();
    }
}

private bool _spinning = true;

private void UpdateAnimation()
{
    if (_spinning &&
        Visible &&
        IsHandleCreated)
    {
        _animation.Start();
    }
    else
    {
        _animation.Stop();
    }
}


Trong OnPaint:

double progress =
    _animation.Progress;


Thay cho:

_animationProgress


Cuối cùng:

protected override void Dispose(
    bool disposing)
{
    if (disposing)
    {
        _animation.Dispose();
    }

    base.Dispose(disposing);
}


Như vậy BootstrapSpinner đã trở thành consumer của animation primitive thay vì tự implement animation.

4. Tích hợp Loading vào BootstrapButton

Đây là phần mình nghĩ sẽ rất hữu ích cho bộ UI của bạn.

API:

var button =
    new BootstrapButton
    {
        Text = "Save",
        Variant =
            BootstrapButtonVariant.Primary,

        LoadingText = "Saving..."
    };

button.Loading = true;


Kết quả:

┌──────────────────────────────┐
│      ◌  Saving...            │
└──────────────────────────────┘

Fields

Trong BootstrapButton:

private BootstrapSpinner? _loadingSpinner;

private bool _loading;

private string _loadingText =
    "Loading...";

private bool _originalEnabled;

Properties
public bool Loading
{
    get => _loading;

    set
    {
        if (_loading == value)
            return;

        _loading = value;

        UpdateLoadingState();
    }
}

public string LoadingText
{
    get => _loadingText;

    set
    {
        _loadingText =
            value ?? string.Empty;

        Invalidate();
    }
}

Tạo spinner
private BootstrapSpinner
    CreateLoadingSpinner()
{
    return new BootstrapSpinner
    {
        SpinnerSize =
            BootstrapSpinnerSize.Small,

        Variant =
            BootstrapSpinnerVariant.Custom,

        CustomColor =
            GetLoadingSpinnerColor(),

        Size =
            new Size(18, 18),

        Margin =
            new Padding(0)
    };
}


Màu spinner nên lấy theo variant của button:

private Color GetLoadingSpinnerColor()
{
    return Variant switch
    {
        BootstrapButtonVariant.Light =>
            AppTheme.Colors.Dark,

        BootstrapButtonVariant.Warning =>
            AppTheme.Colors.Dark,

        _ =>
            Color.White
    };
}

Update loading
private void UpdateLoadingState()
{
    if (_loading)
    {
        _originalEnabled =
            Enabled;

        Enabled = false;

        if (_loadingSpinner == null)
        {
            _loadingSpinner =
                CreateLoadingSpinner();

            Controls.Add(
                _loadingSpinner);
        }

        _loadingSpinner.Visible =
            true;

        _loadingSpinner.Spinning =
            true;

        LayoutLoadingContent();
    }
    else
    {
        Enabled =
            _originalEnabled;

        if (_loadingSpinner != null)
        {
            _loadingSpinner.Spinning =
                false;

            _loadingSpinner.Visible =
                false;
        }

        Invalidate();
    }
}

5. Layout Loading của Button

Trong OnPaint của BootstrapButton, nếu loading:

if (Loading)
{
    DrawLoadingContent(g);
}
else
{
    DrawNormalContent(g);
}


Ví dụ:

private void DrawLoadingContent(
    Graphics g)
{
    string text =
        LoadingText;

    SizeF textSize =
        g.MeasureString(
            text,
            Font);

    int spinnerSize =
        18;

    int spacing = 7;

    float totalWidth =
        spinnerSize +
        spacing +
        textSize.Width;

    float x =
        (Width - totalWidth) / 2f;

    float y =
        (Height - spinnerSize) / 2f;

    // Spinner được Control con vẽ.
    // Chỉ cần căn spinner.

    if (_loadingSpinner != null)
    {
        _loadingSpinner.Location =
            new Point(
                (int)x,
                (Height - spinnerSize) / 2);

        _loadingSpinner.BringToFront();
    }

    using var brush =
        new SolidBrush(
            GetForegroundColor());

    g.DrawString(
        text,
        Font,
        brush,
        x +
        spinnerSize +
        spacing,
        (Height - textSize.Height) / 2f);
}


Tuy nhiên, đẹp hơn nữa là để spinner và text cùng nằm trong một internal layout panel thay vì tự tính bằng DrawString. Với BootstrapButton custom-painted hiện tại, cách trên vẫn nhẹ và hiệu quả.

6. Giữ nguyên kích thước Button khi Loading

Điểm này rất quan trọng.

Không nên:

Save
  ↓
Loading...


làm button thay đổi kích thước.

Nên:

┌──────────────────────┐
│        Save          │
└──────────────────────┘

          ↓

┌──────────────────────┐
│   ◌  Saving...       │
└──────────────────────┘


Vì vậy nên tính:

private Size GetLoadingPreferredSize()
{
    Size normal =
        GetNormalPreferredSize();

    Size loading =
        GetLoadingPreferredSizeCore();

    return new Size(
        Math.Max(
            normal.Width,
            loading.Width),

        Math.Max(
            normal.Height,
            loading.Height));
}


Như vậy khi Loading = true, form không bị layout nhảy.

7. BootstrapProgressBar

Bây giờ đến control thứ ba.

API:

var progress =
    new BootstrapProgressBar
    {
        Value = 65,
        Variant =
            BootstrapProgressVariant.Primary
    };


Hiển thị:

┌────────────────────────────────────────────┐
│██████████████████████████░░░░░░░░░░░░░░░░│
└────────────────────────────────────────────┘
                         65%


Bootstrap progress hỗ trợ progress bar với label, nhiều màu, striped và animated stripes.

8. Enum Progress
namespace YourApp.Controls
{
    public enum BootstrapProgressVariant
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        Light,
        Dark,
        Custom
    }
}

9. BootstrapProgressBar.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YourApp.Animation;
using YourApp.Theme;

namespace YourApp.Controls
{
    public class BootstrapProgressBar : Control
    {
        private double _value;

        private BootstrapProgressVariant _variant =
            BootstrapProgressVariant.Primary;

        private Color _customColor =
            AppTheme.Colors.Primary;

        private int _borderRadius =
            6;

        private bool _showText;

        private bool _striped;

        private bool _animated;

        private int _animationDuration =
            1000;

        private readonly BootstrapLoopAnimation
            _stripeAnimation;

        public BootstrapProgressBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Height = 8;

            BackColor =
                Color.FromArgb(
                    233,
                    236,
                    239);

            _stripeAnimation =
                new BootstrapLoopAnimation();

            _stripeAnimation.Duration =
                _animationDuration;

            _stripeAnimation.ProgressChanged +=
                (_, _) => Invalidate();
        }

        #region Value

        public double Value
        {
            get => _value;

            set
            {
                double newValue =
                    Math.Clamp(
                        value,
                        Minimum,
                        Maximum);

                if (Math.Abs(
                        _value -
                        newValue) < 0.001)
                {
                    return;
                }

                _value =
                    newValue;

                Invalidate();
            }
        }

        public double Minimum { get; set; } = 0;

        public double Maximum { get; set; } = 100;

        public double Percentage
        {
            get
            {
                if (Maximum <= Minimum)
                    return 0;

                return
                    (Value - Minimum) /
                    (Maximum - Minimum) *
                    100;
            }

            set
            {
                Value =
                    Minimum +
                    (Maximum - Minimum) *
                    Math.Clamp(
                        value / 100.0,
                        0,
                        1);
            }
        }

        #endregion

        #region Appearance

        public BootstrapProgressVariant Variant
        {
            get => _variant;

            set
            {
                _variant = value;

                Invalidate();
            }
        }

        public Color CustomColor
        {
            get => _customColor;

            set
            {
                _customColor = value;

                Invalidate();
            }
        }

        public int BorderRadius
        {
            get => _borderRadius;

            set
            {
                _borderRadius =
                    Math.Max(
                        0,
                        value);

                Invalidate();
            }
        }

        public bool ShowText
        {
            get => _showText;

            set
            {
                _showText = value;

                Invalidate();
            }
        }

        public string TextFormat { get; set; } =
            "{0:0}%";

        #endregion

        #region Stripes

        public bool Striped
        {
            get => _striped;

            set
            {
                _striped = value;

                UpdateStripeAnimation();
                Invalidate();
            }
        }

        public bool Animated
        {
            get => _animated;

            set
            {
                _animated = value;

                UpdateStripeAnimation();
                Invalidate();
            }
        }

        public int AnimationDuration
        {
            get => _animationDuration;

            set
            {
                _animationDuration =
                    Math.Max(
                        100,
                        value);

                _stripeAnimation.Duration =
                    _animationDuration;
            }
        }

        private void UpdateStripeAnimation()
        {
            if (Striped &&
                Animated &&
                Visible)
            {
                _stripeAnimation.Start();
            }
            else
            {
                _stripeAnimation.Stop();
            }
        }

        #endregion

        #region Paint

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            Rectangle bounds =
                new Rectangle(
                    0,
                    0,
                    Width - 1,
                    Height - 1);

            using var backgroundBrush =
                new SolidBrush(
                    BackColor);

            DrawRoundedRectangle(
                g,
                backgroundBrush,
                bounds,
                BorderRadius);

            double ratio =
                Maximum <= Minimum
                    ? 0
                    : (Value - Minimum) /
                      (Maximum - Minimum);

            ratio =
                Math.Clamp(
                    ratio,
                    0,
                    1);

            int progressWidth =
                (int)(
                    bounds.Width *
                    ratio);

            if (progressWidth <= 0)
                return;

            Rectangle progressBounds =
                new Rectangle(
                    bounds.X,
                    bounds.Y,
                    progressWidth,
                    bounds.Height);

            Color color =
                GetProgressColor();

            using var brush =
                new SolidBrush(
                    color);

            DrawRoundedRectangle(
                g,
                brush,
                progressBounds,
                BorderRadius);

            if (Striped)
            {
                DrawStripes(
                    g,
                    progressBounds,
                    color);
            }

            if (ShowText)
            {
                DrawText(
                    g,
                    bounds);
            }
        }

        private void DrawStripes(
            Graphics g,
            Rectangle bounds,
            Color color)
        {
            using var clip =
                new Region(bounds);

            Region? oldClip =
                g.Clip;

            g.Clip =
                clip;

            using var brush =
                new SolidBrush(
                    Color.FromArgb(
                        45,
                        Color.White));

            const int stripeWidth = 10;
            const int stripeHeight = 30;

            double offset =
                Animated
                    ? _stripeAnimation.Progress *
                      stripeWidth
                    : 0;

            for (int x =
                     bounds.Left -
                     stripeWidth * 2 -
                     (int)offset;
                 x < bounds.Right;
                 x += stripeWidth * 2)
            {
                Point[] points =
                {
                    new Point(
                        x,
                        bounds.Bottom),

                    new Point(
                        x + stripeWidth,
                        bounds.Bottom),

                    new Point(
                        x + stripeWidth +
                        stripeHeight,
                        bounds.Top),

                    new Point(
                        x + stripeHeight,
                        bounds.Top)
                };

                g.FillPolygon(
                    brush,
                    points);
            }

            g.Clip =
                oldClip;
        }

        private void DrawText(
            Graphics g,
            Rectangle bounds)
        {
            string text =
                string.Format(
                    TextFormat,
                    Percentage);

            using var brush =
                new SolidBrush(
                    GetTextColor());

            using var format =
                new StringFormat
                {
                    Alignment =
                        StringAlignment.Center,

                    LineAlignment =
                        StringAlignment.Center
                };

            g.DrawString(
                text,
                Font,
                brush,
                bounds,
                format);
        }

        #endregion

        #region Colors

        private Color GetProgressColor()
        {
            var colors =
                AppTheme.Colors;

            return Variant switch
            {
                BootstrapProgressVariant.Primary =>
                    colors.Primary,

                BootstrapProgressVariant.Secondary =>
                    colors.Secondary,

                BootstrapProgressVariant.Success =>
                    colors.Success,

                BootstrapProgressVariant.Danger =>
                    colors.Danger,

                BootstrapProgressVariant.Warning =>
                    colors.Warning,

                BootstrapProgressVariant.Info =>
                    colors.Info,

                BootstrapProgressVariant.Light =>
                    colors.Light,

                BootstrapProgressVariant.Dark =>
                    colors.Dark,

                BootstrapProgressVariant.Custom =>
                    CustomColor,

                _ =>
                    colors.Primary
            };
        }

        private Color GetTextColor()
        {
            return Variant switch
            {
                BootstrapProgressVariant.Warning =>
                    AppTheme.Colors.Dark,

                BootstrapProgressVariant.Light =>
                    AppTheme.Colors.Dark,

                _ =>
                    Color.White
            };
        }

        #endregion

        #region Helpers

        private static void DrawRoundedRectangle(
            Graphics g,
            Brush brush,
            Rectangle bounds,
            int radius)
        {
            radius =
                Math.Min(
                    radius,
                    Math.Min(
                        bounds.Width,
                        bounds.Height) / 2);

            if (radius <= 0)
            {
                g.FillRectangle(
                    brush,
                    bounds);

                return;
            }

            using var path =
                new GraphicsPath();

            float d =
                radius * 2f;

            path.AddArc(
                bounds.X,
                bounds.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                bounds.Right - d,
                bounds.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                bounds.Right - d,
                bounds.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                bounds.X,
                bounds.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            g.FillPath(
                brush,
                path);
        }

        #endregion

        protected override void OnVisibleChanged(
            EventArgs e)
        {
            base.OnVisibleChanged(e);

            UpdateStripeAnimation();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _stripeAnimation.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

10. Sử dụng ProgressBar
Cơ bản
var progress =
    new BootstrapProgressBar
    {
        Value = 65,
        Variant =
            BootstrapProgressVariant.Primary
    };

Controls.Add(progress);

Success
progress.Variant =
    BootstrapProgressVariant.Success;

progress.Value = 100;

Hiển thị %
var progress =
    new BootstrapProgressBar
    {
        Value = 72,
        ShowText = true
    };


Kết quả:

┌─────────────────────────────────────┐
│███████████████████████████  72%     │
└─────────────────────────────────────┘

11. Striped
var progress =
    new BootstrapProgressBar
    {
        Value = 65,
        Striped = true
    };

12. Animated striped
var progress =
    new BootstrapProgressBar
    {
        Value = 65,

        Striped = true,

        Animated = true,

        AnimationDuration = 1000
    };


Sẽ tạo hiệu ứng:

////\\\\////\\\\////\\\\
   → chuyển động →


Đây tương ứng với ý tưởng .progress-bar-striped.progress-bar-animated của Bootstrap.

13. Smooth transition cho Value

Hiện tại:

progress.Value = 80;


sẽ nhảy trực tiếp:

20% ───────────────────────► 80%


Mình khuyên bổ sung:

progress.AnimateTo(80);


với animation primitive ở trên.

Thêm vào BootstrapProgressBar:

private BootstrapAnimation?
    _valueAnimation;

public int ValueAnimationDuration
{
    get;
    set;
} = 300;

public void AnimateTo(
    double target)
{
    target =
        Math.Clamp(
            target,
            Minimum,
            Maximum);

    _valueAnimation?.Dispose();

    _valueAnimation =
        new BootstrapAnimation();

    _valueAnimation.Easing =
        BootstrapEasing.EaseOut;

    _valueAnimation.ProgressChanged +=
        (_, e) =>
        {
            Value =
                e.Progress;
        };

    _valueAnimation.Completed +=
        (_, _) =>
        {
            Value =
                target;
        };

    _valueAnimation.Start(
        Value,
        target,
        ValueAnimationDuration);
}


Dùng:

progress.AnimateTo(75);


Rất phù hợp khi download/import/export.

14. Indeterminate ProgressBar

Một thứ nữa mình rất khuyên bổ sung.

Khi chưa biết phần trăm:

┌────────────────────────────────────────┐
│        █████████                       │
└────────────────────────────────────────┘
              → → →


API:

progress.Indeterminate = true;


Thêm property:

private bool _indeterminate;

public bool Indeterminate
{
    get => _indeterminate;

    set
    {
        _indeterminate = value;

        UpdateIndeterminateAnimation();

        Invalidate();
    }
}


Và dùng BootstrapLoopAnimation.

Trong OnPaint:

if (Indeterminate)
{
    DrawIndeterminate(g, bounds);
    return;
}


Phần vẽ:

private void DrawIndeterminate(
    Graphics g,
    Rectangle bounds)
{
    Color color =
        GetProgressColor();

    int width =
        Math.Max(
            40,
            bounds.Width / 3);

    int travel =
        bounds.Width + width;

    int x =
        (int)(
            _indeterminateAnimation.Progress *
            travel) -
        width;

    Rectangle bar =
        new Rectangle(
            x,
            bounds.Y,
            width,
            bounds.Height);

    using var brush =
        new SolidBrush(color);

    DrawRoundedRectangle(
        g,
        brush,
        bar,
        BorderRadius);
}


Như vậy:

var progress =
    new BootstrapProgressBar
    {
        Indeterminate = true,

        Variant =
            BootstrapProgressVariant.Primary
    };


sẽ rất phù hợp cho:

load dữ liệu
gọi API
query database
khởi động module
refresh DataGridView
15. Một API rất đẹp cho async

Sau khi có 3 control này, màn hình WinForms có thể viết rất tự nhiên:

private async Task SaveAsync()
{
    saveButton.Loading = true;

    try
    {
        await service.SaveAsync();
    }
    finally
    {
        saveButton.Loading = false;
    }
}


Download:

private async Task DownloadAsync()
{
    progressBar.Value = 0;

    await downloader.DownloadAsync(
        progress =>
        {
            progressBar.Value =
                progress;
        });
}


Hoặc:

progressBar.AnimateTo(100);

16. Và kết hợp Spinner + Button + Progress

Ví dụ một màn hình import:

┌──────────────────────────────────────────────────────┐
│ Import dữ liệu                                       │
│                                                      │
│ File: customers.xlsx                                │
│                                                      │
│ ███████████████████████████░░░░░░░░░░░░  68%       │
│                                                      │
│                         [ Importing... ◌ ]           │
└──────────────────────────────────────────────────────┘


Code:

private async Task ImportAsync()
{
    importButton.Loading = true;

    progressBar.Visible = true;

    progressBar.Value = 0;

    try
    {
        await importer.ImportAsync(
            progress =>
            {
                progressBar.Value =
                    progress;
            });
    }
    finally
    {
        importButton.Loading = false;
    }
}

17. Kiến trúc sau khi nâng cấp

Mình sẽ chốt kiến trúc animation theo hướng này:

                    ┌──────────────────────┐
                    │ BootstrapAnimation   │
                    │ 0 ──────────────► 1  │
                    └──────────┬───────────┘
                               │
             ┌─────────────────┼─────────────────┐
             │                 │                 │
             ▼                 ▼                 ▼
      BootstrapCollapse   ProgressBar       future...
             │                 │
             │                 └── AnimateTo()
             │
             ▼
       Accordion


                    ┌──────────────────────┐
                    │ BootstrapLoopAnimation│
                    │ 0 → 1 → 0 → 1 → ...  │
                    └──────────┬───────────┘
                               │
                  ┌────────────┼────────────┐
                  ▼            ▼            ▼
             Spinner       ProgressBar    future...


Đây là thay đổi mình đánh giá quan trọng hơn bản thân ProgressBar: từ giờ Collapse, Accordion, Spinner, Button.Loading, ProgressBar đều có thể dùng cùng một animation infrastructure.

Một lưu ý nhỏ: code trên dùng C# hiện đại (Math.Clamp, nullable reference types); nếu project WinForms của bạn đang ở .NET Framework 4.x hoặc C# cũ, mình sẽ đổi phần đó sang implementation tương thích.

