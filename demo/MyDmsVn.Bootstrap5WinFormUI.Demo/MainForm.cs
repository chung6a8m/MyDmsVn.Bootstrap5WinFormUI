using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class MainForm : Form
{
    private readonly ComboBox _themeMode = new ComboBox();
    private readonly CheckBox _reducedMotion = new CheckBox();
    private readonly Button _renderingDemo = new Button();
    private readonly Button _iconDemo = new Button();
    private readonly Button _animationDemo = new Button();
    private readonly FlowLayoutPanel _commandBar = new FlowLayoutPanel();
    private readonly TableLayoutPanel _palette = new TableLayoutPanel();
    private readonly Label _summary = new Label();
    private bool _updatingSelection;

    public MainForm()
    {
        Text = "MyDmsVn.Bootstrap5WinFormUI — Foundation Demo";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 680);
        MinimumSize = new Size(720, 520);

        ConfigureCommandBar();
        ConfigurePalette();
        ConfigureSummary();

        Controls.Add(_palette);
        Controls.Add(_summary);
        Controls.Add(_commandBar);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        SyncSelection(BootstrapThemeManager.CurrentTheme);
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureCommandBar()
    {
        _commandBar.Dock = DockStyle.Top;
        _commandBar.AutoSize = true;
        _commandBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _commandBar.FlowDirection = FlowDirection.LeftToRight;
        _commandBar.WrapContents = false;
        _commandBar.Padding = new Padding(12, 10, 12, 10);

        var modeLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 6, 6, 0),
            Text = "Theme"
        };

        _themeMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeMode.Width = 120;
        _themeMode.Items.Add("Light");
        _themeMode.Items.Add("Dark");
        _themeMode.SelectedIndexChanged += (_, _) => PublishSelectedTheme();

        _reducedMotion.AutoSize = true;
        _reducedMotion.Margin = new Padding(18, 6, 0, 0);
        _reducedMotion.Text = "Reduced motion";
        _reducedMotion.CheckedChanged += (_, _) => PublishSelectedTheme();

        _renderingDemo.AutoSize = true;
        _renderingDemo.Margin = new Padding(18, 1, 0, 0);
        _renderingDemo.Text = "Rendering / DPI";
        _renderingDemo.UseVisualStyleBackColor = false;
        _renderingDemo.Click += (_, _) =>
        {
            var demo = new RenderingDemoForm();
            demo.Show(this);
        };

        _iconDemo.AutoSize = true;
        _iconDemo.Margin = new Padding(8, 1, 0, 0);
        _iconDemo.Text = "Icons";
        _iconDemo.UseVisualStyleBackColor = false;
        _iconDemo.Click += (_, _) =>
        {
            var demo = new IconDemoForm();
            demo.Show(this);
        };

        _animationDemo.AutoSize = true;
        _animationDemo.Margin = new Padding(8, 1, 0, 0);
        _animationDemo.Text = "Animation";
        _animationDemo.UseVisualStyleBackColor = false;
        _animationDemo.Click += (_, _) =>
        {
            var demo = new AnimationDemoForm();
            demo.Show(this);
        };

        _commandBar.Controls.Add(modeLabel);
        _commandBar.Controls.Add(_themeMode);
        _commandBar.Controls.Add(_reducedMotion);
        _commandBar.Controls.Add(_renderingDemo);
        _commandBar.Controls.Add(_iconDemo);
        _commandBar.Controls.Add(_animationDemo);
    }

    private void ConfigurePalette()
    {
        _palette.Dock = DockStyle.Fill;
        _palette.AutoScroll = true;
        _palette.Padding = new Padding(12);
        _palette.ColumnCount = 3;
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
    }

    private void ConfigureSummary()
    {
        _summary.Dock = DockStyle.Bottom;
        _summary.AutoSize = false;
        _summary.Height = 72;
        _summary.Padding = new Padding(12, 8, 12, 8);
        _summary.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void PublishSelectedTheme()
    {
        if (_updatingSelection || _themeMode.SelectedIndex < 0)
        {
            return;
        }

        var mode = _themeMode.SelectedIndex == 1
            ? BootstrapThemeMode.Dark
            : BootstrapThemeMode.Light;

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode, _reducedMotion.Checked);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        SyncSelection(e.NewTheme);
        ApplyTheme(e.NewTheme);
    }

    private void SyncSelection(BootstrapTheme theme)
    {
        _updatingSelection = true;
        try
        {
            _themeMode.SelectedIndex = theme.Mode == BootstrapThemeMode.Dark ? 1 : 0;
            _reducedMotion.Checked = theme.ReducedMotion;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _commandBar.BackColor = theme.Colors.SurfaceSecondary;
        _commandBar.ForeColor = theme.Colors.Text;
        _themeMode.BackColor = theme.Colors.Surface;
        _themeMode.ForeColor = theme.Colors.Text;
        _reducedMotion.BackColor = theme.Colors.SurfaceSecondary;
        _reducedMotion.ForeColor = theme.Colors.Text;
        _renderingDemo.BackColor = theme.Colors.Surface;
        _renderingDemo.ForeColor = theme.Colors.Text;
        _iconDemo.BackColor = theme.Colors.Surface;
        _iconDemo.ForeColor = theme.Colors.Text;
        _animationDemo.BackColor = theme.Colors.Surface;
        _animationDemo.ForeColor = theme.Colors.Text;
        _palette.BackColor = theme.Colors.Body;
        _summary.BackColor = theme.Colors.SurfaceSecondary;
        _summary.ForeColor = theme.Colors.Text;

        RebuildPalette(theme);

        var metrics = theme.Metrics;
        var typography = theme.Typography;
        _summary.Text =
            $"{theme.Mode} · Reduced motion: {theme.ReducedMotion} · " +
            $"Control heights: {metrics.ControlHeightSmall}/{metrics.ControlHeight}/{metrics.ControlHeightLarge}px · " +
            $"Radius: {metrics.RadiusSmall}/{metrics.Radius}/{metrics.RadiusLarge}px · " +
            $"Body: {typography.Body.FontFamilyName} {typography.Body.SizeInPoints:0.##}pt";
    }

    private void RebuildPalette(BootstrapTheme theme)
    {
        _palette.SuspendLayout();
        try
        {
            while (_palette.Controls.Count > 0)
            {
                var control = _palette.Controls[0];
                _palette.Controls.RemoveAt(0);
                control.Dispose();
            }

            _palette.RowStyles.Clear();
            _palette.RowCount = 0;

            AddHeader(theme);

            var colors = theme.Colors;
            var tokens = new[]
            {
                new KeyValuePair<string, Color>("Primary", colors.Primary),
                new KeyValuePair<string, Color>("Secondary", colors.Secondary),
                new KeyValuePair<string, Color>("Success", colors.Success),
                new KeyValuePair<string, Color>("Danger", colors.Danger),
                new KeyValuePair<string, Color>("Warning", colors.Warning),
                new KeyValuePair<string, Color>("Info", colors.Info),
                new KeyValuePair<string, Color>("Light", colors.Light),
                new KeyValuePair<string, Color>("Dark", colors.Dark),
                new KeyValuePair<string, Color>("Body", colors.Body),
                new KeyValuePair<string, Color>("Surface", colors.Surface),
                new KeyValuePair<string, Color>("SurfaceSecondary", colors.SurfaceSecondary),
                new KeyValuePair<string, Color>("Border", colors.Border),
                new KeyValuePair<string, Color>("Text", colors.Text),
                new KeyValuePair<string, Color>("MutedText", colors.MutedText),
                new KeyValuePair<string, Color>("Disabled", colors.Disabled),
                new KeyValuePair<string, Color>("Focus", colors.Focus),
                new KeyValuePair<string, Color>("Hover", colors.Hover),
                new KeyValuePair<string, Color>("Active", colors.Active)
            };

            foreach (var token in tokens)
            {
                AddColorRow(theme, token.Key, token.Value);
            }
        }
        finally
        {
            _palette.ResumeLayout(true);
        }
    }

    private void AddHeader(BootstrapTheme theme)
    {
        var row = _palette.RowCount++;
        _palette.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _palette.Controls.Add(CreateLabel("Token", theme, FontStyle.Bold), 0, row);
        _palette.Controls.Add(CreateLabel("Preview", theme, FontStyle.Bold), 1, row);
        _palette.Controls.Add(CreateLabel("Value", theme, FontStyle.Bold), 2, row);
    }

    private void AddColorRow(BootstrapTheme theme, string name, Color color)
    {
        var row = _palette.RowCount++;
        _palette.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _palette.Controls.Add(CreateLabel(name, theme, FontStyle.Regular), 0, row);
        _palette.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 4, 12, 4),
            BackColor = color,
            BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = $"{name} color preview"
        }, 1, row);
        _palette.Controls.Add(CreateLabel(ToHex(color), theme, FontStyle.Regular), 2, row);
    }

    private static Label CreateLabel(string text, BootstrapTheme theme, FontStyle style)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 5, 3, 3),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = theme.Colors.Body,
            ForeColor = theme.Colors.Text,
            Font = new Font(theme.Typography.Body.FontFamilyName, theme.Typography.Body.SizeInPoints, style)
        };
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
