using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class AnimationDemoForm : Form
{
    private readonly FlowLayoutPanel _commandBar = new FlowLayoutPanel();
    private readonly Button _finiteStart = new Button();
    private readonly Button _finiteStop = new Button();
    private readonly Button _finiteRestart = new Button();
    private readonly Button _loopStart = new Button();
    private readonly Button _loopStop = new Button();
    private readonly Button _loopRestart = new Button();
    private readonly Button _togglePreviews = new Button();
    private readonly CheckBox _reducedMotion = new CheckBox();

    private readonly TableLayoutPanel _layout = new TableLayoutPanel();
    private readonly Label _finiteTitle = new Label();
    private readonly Label _finiteProgress = new Label();
    private readonly Panel _finitePreview = new Panel();
    private readonly Panel _finiteBar = new Panel();
    private readonly Label _loopTitle = new Label();
    private readonly Label _loopProgress = new Label();
    private readonly Panel _loopPreview = new Panel();
    private readonly Panel _loopMarker = new Panel();
    private readonly Label _note = new Label();

    private readonly BootstrapAnimation _finiteAnimation;
    private readonly BootstrapLoopAnimation _loopAnimation;
    private bool _syncingReducedMotion;

    public AnimationDemoForm()
    {
        Text = "Animation Infrastructure";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 520);
        MinimumSize = new Size(720, 420);

        ConfigureCommandBar();
        ConfigureContent();

        Controls.Add(_layout);
        Controls.Add(_commandBar);

        _finiteAnimation = new BootstrapAnimation(
            TimeSpan.FromMilliseconds(900),
            BootstrapEasing.EaseInOut,
            _finitePreview);
        _loopAnimation = new BootstrapLoopAnimation(
            TimeSpan.FromMilliseconds(1200),
            BootstrapEasing.Linear,
            _loopPreview);

        _finiteAnimation.ProgressChanged += OnFiniteProgressChanged;
        _loopAnimation.ProgressChanged += OnLoopProgressChanged;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;

        SyncReducedMotion(BootstrapThemeManager.CurrentTheme);
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        UpdateFinitePreview();
        UpdateLoopPreview();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _finiteAnimation.ProgressChanged -= OnFiniteProgressChanged;
            _loopAnimation.ProgressChanged -= OnLoopProgressChanged;
            _finiteAnimation.Dispose();
            _loopAnimation.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ConfigureCommandBar()
    {
        _commandBar.Dock = DockStyle.Top;
        _commandBar.AutoSize = true;
        _commandBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _commandBar.WrapContents = true;
        _commandBar.Padding = new Padding(12, 10, 12, 10);

        ConfigureButton(_finiteStart, "Finite Start", (_, _) => _finiteAnimation.Start());
        ConfigureButton(_finiteStop, "Finite Stop", (_, _) => _finiteAnimation.Stop());
        ConfigureButton(_finiteRestart, "Finite Restart", (_, _) => _finiteAnimation.Restart());
        ConfigureButton(_loopStart, "Loop Start", (_, _) => _loopAnimation.Start());
        ConfigureButton(_loopStop, "Loop Stop", (_, _) => _loopAnimation.Stop());
        ConfigureButton(_loopRestart, "Loop Restart", (_, _) => _loopAnimation.Restart());
        ConfigureButton(_togglePreviews, "Hide previews", TogglePreviews);

        _reducedMotion.AutoSize = true;
        _reducedMotion.Margin = new Padding(12, 7, 0, 0);
        _reducedMotion.Text = "Reduced motion";
        _reducedMotion.CheckedChanged += (_, _) => PublishReducedMotion();

        _commandBar.Controls.Add(_finiteStart);
        _commandBar.Controls.Add(_finiteStop);
        _commandBar.Controls.Add(_finiteRestart);
        _commandBar.Controls.Add(_loopStart);
        _commandBar.Controls.Add(_loopStop);
        _commandBar.Controls.Add(_loopRestart);
        _commandBar.Controls.Add(_togglePreviews);
        _commandBar.Controls.Add(_reducedMotion);
    }

    private static void ConfigureButton(Button button, string text, EventHandler click)
    {
        button.AutoSize = true;
        button.Margin = new Padding(0, 1, 8, 0);
        button.Text = text;
        button.UseVisualStyleBackColor = false;
        button.Click += click;
    }

    private void ConfigureContent()
    {
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(18);
        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        _layout.RowCount = 5;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _finiteTitle.AutoSize = true;
        _finiteTitle.Text = "Finite animation — EaseInOut / 900 ms";
        _finiteTitle.Margin = new Padding(0, 6, 0, 6);

        _finiteProgress.AutoSize = true;
        _finiteProgress.TextAlign = ContentAlignment.MiddleRight;
        _finiteProgress.Dock = DockStyle.Fill;

        _finitePreview.Dock = DockStyle.Fill;
        _finitePreview.Margin = new Padding(0, 0, 0, 18);
        _finitePreview.Padding = new Padding(8);
        _finitePreview.BorderStyle = BorderStyle.FixedSingle;
        _finitePreview.Resize += (_, _) => UpdateFinitePreview();
        _finitePreview.Controls.Add(_finiteBar);

        _finiteBar.Height = 34;
        _finiteBar.Left = 8;
        _finiteBar.Top = 22;

        _loopTitle.AutoSize = true;
        _loopTitle.Text = "Loop animation — Linear / 1200 ms";
        _loopTitle.Margin = new Padding(0, 6, 0, 6);

        _loopProgress.AutoSize = true;
        _loopProgress.TextAlign = ContentAlignment.MiddleRight;
        _loopProgress.Dock = DockStyle.Fill;

        _loopPreview.Dock = DockStyle.Fill;
        _loopPreview.Margin = new Padding(0, 0, 0, 18);
        _loopPreview.Padding = new Padding(8);
        _loopPreview.BorderStyle = BorderStyle.FixedSingle;
        _loopPreview.Resize += (_, _) => UpdateLoopPreview();
        _loopPreview.Controls.Add(_loopMarker);

        _loopMarker.Size = new Size(34, 34);
        _loopMarker.Top = 22;

        _note.AutoSize = false;
        _note.Dock = DockStyle.Fill;
        _note.TextAlign = ContentAlignment.TopLeft;
        _note.Text =
            "The previews are driven only by BootstrapAnimation/BootstrapLoopAnimation. " +
            "Hide both preview owners while they are running to verify scheduling pauses; show them again to verify hidden wall-clock time is excluded. " +
            "Reduced motion is evaluated on the next Start/Restart.";

        _layout.Controls.Add(_finiteTitle, 0, 0);
        _layout.Controls.Add(_finiteProgress, 1, 0);
        _layout.Controls.Add(_finitePreview, 0, 1);
        _layout.SetColumnSpan(_finitePreview, 2);
        _layout.Controls.Add(_loopTitle, 0, 2);
        _layout.Controls.Add(_loopProgress, 1, 2);
        _layout.Controls.Add(_loopPreview, 0, 3);
        _layout.SetColumnSpan(_loopPreview, 2);
        _layout.Controls.Add(_note, 0, 4);
        _layout.SetColumnSpan(_note, 2);
    }

    private void OnFiniteProgressChanged(object? sender, EventArgs e)
    {
        UpdateFinitePreview();
    }

    private void OnLoopProgressChanged(object? sender, EventArgs e)
    {
        UpdateLoopPreview();
    }

    private void UpdateFinitePreview()
    {
        if (_finiteAnimation is null)
        {
            return;
        }

        var availableWidth = Math.Max(0, _finitePreview.ClientSize.Width - 16);
        _finiteBar.Width = (int)Math.Round(availableWidth * _finiteAnimation.Progress);
        _finiteProgress.Text = $"{_finiteAnimation.Progress:0.000} · {(_finiteAnimation.IsRunning ? "running" : "stopped")}";
    }

    private void UpdateLoopPreview()
    {
        if (_loopAnimation is null)
        {
            return;
        }

        var travel = Math.Max(0, _loopPreview.ClientSize.Width - _loopMarker.Width - 16);
        _loopMarker.Left = 8 + (int)Math.Round(travel * _loopAnimation.Progress);
        _loopProgress.Text = $"{_loopAnimation.Progress:0.000} · {(_loopAnimation.IsRunning ? "running" : "stopped")}";
    }

    private void TogglePreviews(object? sender, EventArgs e)
    {
        var makeVisible = !_finitePreview.Visible;
        _finitePreview.Visible = makeVisible;
        _loopPreview.Visible = makeVisible;
        _togglePreviews.Text = makeVisible ? "Hide previews" : "Show previews";
    }

    private void PublishReducedMotion()
    {
        if (_syncingReducedMotion)
        {
            return;
        }

        var current = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(current.Mode, _reducedMotion.Checked);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        SyncReducedMotion(e.NewTheme);
        ApplyTheme(e.NewTheme);
    }

    private void SyncReducedMotion(BootstrapTheme theme)
    {
        _syncingReducedMotion = true;
        try
        {
            _reducedMotion.Checked = theme.ReducedMotion;
        }
        finally
        {
            _syncingReducedMotion = false;
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _commandBar.BackColor = theme.Colors.SurfaceSecondary;
        _layout.BackColor = theme.Colors.Body;
        _finiteTitle.ForeColor = theme.Colors.Text;
        _finiteProgress.ForeColor = theme.Colors.MutedText;
        _loopTitle.ForeColor = theme.Colors.Text;
        _loopProgress.ForeColor = theme.Colors.MutedText;
        _note.ForeColor = theme.Colors.MutedText;
        _finitePreview.BackColor = theme.Colors.Surface;
        _loopPreview.BackColor = theme.Colors.Surface;
        _finiteBar.BackColor = theme.Colors.Primary;
        _loopMarker.BackColor = theme.Colors.Info;
        _reducedMotion.BackColor = theme.Colors.SurfaceSecondary;
        _reducedMotion.ForeColor = theme.Colors.Text;

        var buttons = new[]
        {
            _finiteStart,
            _finiteStop,
            _finiteRestart,
            _loopStart,
            _loopStop,
            _loopRestart,
            _togglePreviews
        };

        foreach (var button in buttons)
        {
            button.BackColor = theme.Colors.Surface;
            button.ForeColor = theme.Colors.Text;
        }
    }
}
