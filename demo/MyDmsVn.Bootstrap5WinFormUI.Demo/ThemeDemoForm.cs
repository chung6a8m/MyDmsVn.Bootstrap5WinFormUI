using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class ThemeDemoForm : Form
{
    private readonly Label _summary = new Label();
    private readonly TableLayoutPanel _palette = new TableLayoutPanel();

    public ThemeDemoForm()
    {
        Text = "Theme";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        AutoScroll = true;

        _summary.Dock = DockStyle.Top;
        _summary.Height = 72;
        _summary.Padding = new Padding(20, 14, 20, 10);
        _summary.TextAlign = ContentAlignment.MiddleLeft;

        _palette.Dock = DockStyle.Fill;
        _palette.AutoScroll = true;
        _palette.Padding = new Padding(20, 8, 20, 20);
        _palette.ColumnCount = 3;
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _palette.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        Controls.Add(_palette);
        Controls.Add(_summary);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
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

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _summary.BackColor = theme.Colors.SurfaceSecondary;
        _summary.ForeColor = theme.Colors.Text;
        _palette.BackColor = theme.Colors.Body;

        var metrics = theme.Metrics;
        var typography = theme.Typography;
        _summary.Text =
            $"{theme.Mode} theme · Reduced motion: {theme.ReducedMotion} · " +
            $"Control heights {metrics.ControlHeightSmall}/{metrics.ControlHeight}/{metrics.ControlHeightLarge}px · " +
            $"Radius {metrics.RadiusSmall}/{metrics.Radius}/{metrics.RadiusLarge}px · " +
            $"Body {typography.Body.FontFamilyName} {typography.Body.SizeInPoints:0.##}pt";

        RebuildPalette(theme);
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
        _palette.Controls.Add(CreateLabel("Token", theme), 0, row);
        _palette.Controls.Add(CreateLabel("Preview", theme), 1, row);
        _palette.Controls.Add(CreateLabel("Value", theme), 2, row);
    }

    private void AddColorRow(BootstrapTheme theme, string name, Color color)
    {
        var row = _palette.RowCount++;
        _palette.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        _palette.Controls.Add(CreateLabel(name, theme), 0, row);
        _palette.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 4, 12, 4),
            BackColor = color,
            BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = $"{name} color preview"
        }, 1, row);
        _palette.Controls.Add(CreateLabel(ToHex(color), theme), 2, row);
    }

    private static Label CreateLabel(string text, BootstrapTheme theme)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 5, 3, 3),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = theme.Colors.Body,
            ForeColor = theme.Colors.Text
        };
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
