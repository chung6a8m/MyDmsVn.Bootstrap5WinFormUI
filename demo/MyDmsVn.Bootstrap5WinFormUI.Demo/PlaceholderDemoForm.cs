using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class PlaceholderDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Panel _swapSkeleton = new Panel();
    private readonly Panel _loadedContent = new Panel();
    private readonly Label _status = new Label();
    private readonly Button _toggle = new Button();
    private bool _loaded;

    public PlaceholderDemoForm()
    {
        Text = "BootstrapPlaceholder / Skeleton Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 760);
        MinimumSize = new Size(700, 560);

        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(12);
        Controls.Add(_content);

        AddSizesSection();
        AddColorsSection();
        AddAnimationsSection();
        AddSkeletonCardSection();
        AddApplicationSwapSection();

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        SetLoaded(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void AddSizesSection()
    {
        var stack = CreateSection("Sizes", "PlaceholderSize owns intrinsic height only while AutoSize is enabled.");
        var row = CreateHorizontalRow();
        AddLabeledPlaceholder(row, "XS", new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.ExtraSmall,
            Tag = "Natural size ExtraSmall"
        });
        AddLabeledPlaceholder(row, "SM", new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.Small,
            Tag = "Natural size Small"
        });
        AddLabeledPlaceholder(row, "Default", new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.Default,
            Tag = "Natural size Default"
        });
        AddLabeledPlaceholder(row, "LG", new BootstrapPlaceholder
        {
            PlaceholderSize = BootstrapPlaceholderSize.Large,
            Tag = "Natural size Large"
        });
        stack.Controls.Add(row);
    }

    private void AddColorsSection()
    {
        var stack = CreateSection("Colors and radius", "Semantic variants, opaque CustomColor, and square/theme/oversized radii.");
        var row = CreateHorizontalRow();
        AddLabeledPlaceholder(row, "Primary / square", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Primary, 0));
        AddLabeledPlaceholder(row, "Secondary / theme", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Secondary, -1));
        AddLabeledPlaceholder(row, "Success / rounded", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Success, 999));
        AddLabeledPlaceholder(row, "Danger", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Danger, 6));
        AddLabeledPlaceholder(row, "Warning", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Warning, 6));
        AddLabeledPlaceholder(row, "Info", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Info, 6));
        AddLabeledPlaceholder(row, "Light", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Light, 6));
        AddLabeledPlaceholder(row, "Dark", CreateExplicitPlaceholder(130, 24, BootstrapVariant.Dark, 6));
        var custom = CreateExplicitPlaceholder(130, 24, BootstrapVariant.Secondary, 6);
        custom.CustomColor = Color.FromArgb(111, 66, 193);
        AddLabeledPlaceholder(row, "Custom color", custom);
        stack.Controls.Add(row);
    }

    private void AddAnimationsSection()
    {
        var stack = CreateSection("Animations", "None, Glow, and Wave share the global Reduced motion preference.");
        var row = CreateHorizontalRow();
        AddLabeledPlaceholder(row, "Static", CreateAnimatedPlaceholder(BootstrapPlaceholderAnimation.None));
        AddLabeledPlaceholder(row, "Glow", CreateAnimatedPlaceholder(BootstrapPlaceholderAnimation.Glow));
        AddLabeledPlaceholder(row, "Wave", CreateAnimatedPlaceholder(BootstrapPlaceholderAnimation.Wave));
        stack.Controls.Add(row);
    }

    private void AddSkeletonCardSection()
    {
        var stack = CreateSection("Skeleton card", "Ordinary controls and percent columns compose an avatar, title, body lines, and button shape.");
        var card = new Panel
        {
            AccessibleName = "Composed skeleton card",
            AutoSize = false,
            Size = new Size(650, 235),
            Padding = new Padding(16),
            BorderStyle = BorderStyle.FixedSingle
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(4)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var avatar = new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(48, 48),
            BorderRadius = 999,
            Animation = BootstrapPlaceholderAnimation.Wave,
            Tag = "Skeleton avatar"
        };
        layout.Controls.Add(avatar, 0, 0);

        var lines = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 0, 0, 0)
        };
        lines.Controls.Add(CreatePercentBar("Skeleton explicit bar title", 18, 4));
        lines.Controls.Add(CreatePercentBar("Skeleton explicit bar body 1", 14, 4));
        lines.Controls.Add(CreatePercentBar("Skeleton explicit bar body 2", 14, 3));
        lines.Controls.Add(CreatePercentBar("Skeleton explicit bar body 3", 14, 2));
        lines.Controls.Add(CreatePercentBar("Skeleton explicit bar button", 32, 1, borderRadius: -1));
        layout.Controls.Add(lines, 1, 0);
        card.Controls.Add(layout);
        stack.Controls.Add(card);
    }

    private void AddApplicationSwapSection()
    {
        var stack = CreateSection("Application-owned swap", "The application toggles skeleton/content visibility and exposes status through native text.");

        _status.AutoSize = true;
        _status.AccessibleName = "Loading status";
        _status.Margin = new Padding(3, 4, 3, 8);

        _toggle.AutoSize = true;
        _toggle.AccessibleName = "Toggle loaded content";
        _toggle.Text = "Toggle loaded state";
        _toggle.UseVisualStyleBackColor = false;
        _toggle.Click += (_, _) => SetLoaded(!_loaded);

        _swapSkeleton.AccessibleName = "Swap skeleton panel";
        _swapSkeleton.Size = new Size(560, 72);
        _swapSkeleton.Controls.Add(new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(420, 18),
            Location = new Point(0, 4),
            Animation = BootstrapPlaceholderAnimation.Glow
        });
        _swapSkeleton.Controls.Add(new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(320, 14),
            Location = new Point(0, 34),
            Animation = BootstrapPlaceholderAnimation.Glow
        });

        _loadedContent.AccessibleName = "Loaded content panel";
        _loadedContent.Size = new Size(560, 72);
        _loadedContent.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Loaded content is now the meaningful application surface.",
            Location = new Point(0, 10)
        });

        stack.Controls.Add(_status);
        stack.Controls.Add(_toggle);
        stack.Controls.Add(_swapSkeleton);
        stack.Controls.Add(_loadedContent);
    }

    private FlowLayoutPanel CreateSection(string title, string description)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 840,
            MinimumSize = new Size(840, 0),
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12)
        };
        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
        };
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = description,
            Margin = new Padding(3, 2, 3, 8)
        });
        group.Controls.Add(stack);
        _content.Controls.Add(group);
        return stack;
    }

    private static FlowLayoutPanel CreateHorizontalRow()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private static void AddLabeledPlaceholder(FlowLayoutPanel row, string caption, BootstrapPlaceholder placeholder)
    {
        var cell = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            MinimumSize = new Size(145, 58),
            Margin = new Padding(0, 0, 10, 0)
        };
        placeholder.Margin = new Padding(4, 6, 4, 4);
        cell.Controls.Add(placeholder);
        cell.Controls.Add(new Label { AutoSize = true, Text = caption, Margin = new Padding(4, 2, 4, 2) });
        row.Controls.Add(cell);
    }

    private static BootstrapPlaceholder CreateExplicitPlaceholder(int width, int height, BootstrapVariant variant, int borderRadius)
    {
        return new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(width, height),
            Variant = variant,
            BorderRadius = borderRadius
        };
    }

    private static BootstrapPlaceholder CreateAnimatedPlaceholder(BootstrapPlaceholderAnimation animation)
    {
        return new BootstrapPlaceholder
        {
            AutoSize = false,
            Size = new Size(180, 24),
            Animation = animation,
            BorderRadius = -1
        };
    }

    private static TableLayoutPanel CreatePercentBar(string tag, int height, int columnSpan, int borderRadius = 0)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = false,
            Size = new Size(500, height + 8),
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4)
        };
        for (var index = 0; index < 4; index++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        var placeholder = new BootstrapPlaceholder
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Height = height,
            Animation = BootstrapPlaceholderAnimation.Wave,
            BorderRadius = borderRadius,
            Tag = tag,
            Margin = new Padding(0, 2, 4, 2)
        };
        row.Controls.Add(placeholder, 0, 0);
        row.SetColumnSpan(placeholder, columnSpan);
        return row;
    }

    private void SetLoaded(bool loaded)
    {
        _loaded = loaded;
        _swapSkeleton.Visible = !loaded;
        _loadedContent.Visible = loaded;
        _status.Text = loaded ? "Content loaded." : "Loading content…";
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        ApplyThemeToChildren(_content, theme);
        _toggle.BackColor = theme.Colors.Surface;
        _toggle.ForeColor = theme.Colors.Text;
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is not BootstrapPlaceholder)
            {
                child.ForeColor = theme.Colors.Text;
                if (child is Panel || child is FlowLayoutPanel || child is TableLayoutPanel || child is GroupBox || child is Label)
                {
                    child.BackColor = theme.Colors.Body;
                }
            }

            if (child.HasChildren)
            {
                ApplyThemeToChildren(child, theme);
            }
        }
    }
}
