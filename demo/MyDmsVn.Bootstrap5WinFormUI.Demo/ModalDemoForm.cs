using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ModalDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _result = new Label();

    public ModalDemoForm()
    {
        Text = "BootstrapModal Demo";
        ClientSize = new Size(940, 700);
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(18);

        _content.Controls.Add(CreateNote("Native ShowDialog, DialogResult, AcceptButton, CancelButton, focus, and FormClosing remain authoritative. Test Tab/Shift+Tab, Enter, Escape, Alt+Tab, owner move/resize, Light/Dark, reduced motion, and 100/125/150/175/200% scaling."));
        _content.Controls.Add(CreateRow(
            CreateScenarioButton("Small", BootstrapModalSize.Small),
            CreateScenarioButton("Default", BootstrapModalSize.Default),
            CreateScenarioButton("Large", BootstrapModalSize.Large),
            CreateScenarioButton("Extra large", BootstrapModalSize.ExtraLarge),
            CreateScenarioButton("Custom", BootstrapModalSize.Custom)));
        _content.Controls.Add(CreateRow(
            CreateScenarioButton("Static backdrop", BootstrapModalSize.Default, BootstrapModalBackdropMode.Static),
            CreateScenarioButton("No backdrop", BootstrapModalSize.Default, BootstrapModalBackdropMode.None),
            CreateScenarioButton("Framework dismiss", BootstrapModalSize.Default, useCancelButton: false),
            CreateScenarioButton("Escape disabled", BootstrapModalSize.Default, closeOnEscape: false, useCancelButton: false),
            CreateScenarioButton("RTL", BootstrapModalSize.Default, BootstrapModalBackdropMode.Dismissible, rtl: true)));
        _content.Controls.Add(CreateRow(
            CreateScenarioButton("Caller Close diagnostic", BootstrapModalSize.Default, callerClose: true),
            CreateScenarioButton("Dynamic content", BootstrapModalSize.Default, dynamicContent: true),
            CreateScenarioButton("Parameterless owner", BootstrapModalSize.Default, parameterlessOwner: true),
            CreateScenarioButton("Monitor edge", BootstrapModalSize.Large, monitorEdgeOwner: true)));
        _result.AutoSize = true;
        _result.Text = "Last DialogResult: (none)";
        _result.AccessibleName = "Last modal dialog result";
        _content.Controls.Add(_result);
        Controls.Add(_content);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private Button CreateScenarioButton(
        string text,
        BootstrapModalSize size,
        BootstrapModalBackdropMode backdrop = BootstrapModalBackdropMode.Dismissible,
        bool closeOnEscape = true,
        bool rtl = false,
        bool useCancelButton = true,
        bool callerClose = false,
        bool dynamicContent = false,
        bool parameterlessOwner = false,
        bool monitorEdgeOwner = false)
    {
        var button = new Button { AutoSize = true, Text = text };
        button.Click += (_, _) => ShowScenario(
            text,
            size,
            backdrop,
            closeOnEscape,
            rtl,
            useCancelButton,
            callerClose,
            dynamicContent,
            parameterlessOwner,
            monitorEdgeOwner);
        return button;
    }

    private void ShowScenario(
        string title,
        BootstrapModalSize size,
        BootstrapModalBackdropMode backdrop,
        bool closeOnEscape,
        bool rtl,
        bool useCancelButton,
        bool callerClose,
        bool dynamicContent,
        bool parameterlessOwner,
        bool monitorEdgeOwner)
    {
        using var modal = new BootstrapModal
        {
            Text = title,
            ModalSize = size,
            BackdropMode = backdrop,
            CloseOnEscape = closeOnEscape,
            RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = rtl
        };
        if (size == BootstrapModalSize.Custom) modal.ClientSize = new Size(640, 440);

        var editor = new TextBox { Dock = DockStyle.Top, Text = "Initial focus (type here)", TabIndex = 0 };
        var longBody = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Text = string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                "This modal uses a native WinForms modal loop.",
                "The body scrolls independently when content exceeds the working area.",
                "Change the global theme or reduced-motion setting before opening another scenario.",
                "Move or resize the owner, switch applications with Alt+Tab, and verify the backdrop follows without becoming globally topmost."
            }),
            TabIndex = 1
        };
        modal.BodyPanel.Controls.Add(longBody);
        modal.BodyPanel.Controls.Add(editor);
        modal.InitialFocusControl = editor;

        var toggleTheme = new Button { Text = "Toggle Light/Dark while open", AutoSize = true };
        toggleTheme.Click += (_, _) =>
        {
            var current = BootstrapThemeManager.CurrentTheme;
            var nextMode = current.Mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light;
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(nextMode, current.ReducedMotion);
        };
        var toggleReducedMotion = new Button { Text = "Toggle reduced motion", AutoSize = true };
        toggleReducedMotion.Click += (_, _) =>
        {
            var current = BootstrapThemeManager.CurrentTheme;
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(current.Mode, !current.ReducedMotion);
        };
        modal.BodyPanel.Controls.Add(toggleReducedMotion);
        modal.BodyPanel.Controls.Add(toggleTheme);

        var dynamicDetails = new Label
        {
            AutoSize = true,
            Text = "This content and the extra footer action were added at runtime.",
            Visible = false
        };
        var toggleDynamic = new Button { Text = "Toggle dynamic content", AutoSize = true, Visible = dynamicContent };
        modal.BodyPanel.Controls.Add(dynamicDetails);
        modal.BodyPanel.Controls.Add(toggleDynamic);

        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var ok = new Button { Text = "OK", AutoSize = true, DialogResult = DialogResult.OK };
        var extraAction = new Button { Text = "Extra action", AutoSize = true, Visible = false };
        var callerCloseButton = new Button { Text = "Caller Close", AutoSize = true, Visible = callerClose };
        callerCloseButton.Click += (_, _) => modal.Close();
        toggleDynamic.Click += (_, _) =>
        {
            dynamicDetails.Visible = !dynamicDetails.Visible;
            extraAction.Visible = dynamicDetails.Visible;
            modal.PerformLayout();
        };
        modal.FooterPanel.Controls.Add(callerCloseButton);
        modal.FooterPanel.Controls.Add(extraAction);
        modal.FooterPanel.Controls.Add(cancel);
        modal.FooterPanel.Controls.Add(ok);
        modal.AcceptButton = ok;
        if (useCancelButton) modal.CancelButton = cancel;

        DialogResult result;
        var ownerDescription = "managed owner";
        if (monitorEdgeOwner)
        {
            var working = Screen.FromControl(this).WorkingArea;
            using var edgeOwner = new Form
            {
                Text = "Modal edge diagnostic owner",
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(working.Right - 340, working.Bottom - 240, 320, 220)
            };
            edgeOwner.Show();
            result = modal.ShowDialog(edgeOwner);
            ownerDescription = "edge owner";
        }
        else if (parameterlessOwner)
        {
            result = modal.ShowDialog();
            ownerDescription = "parameterless ShowDialog";
        }
        else
        {
            var owner = TopLevelControl as IWin32Window;
            result = owner is null ? modal.ShowDialog() : modal.ShowDialog(owner);
        }

        _result.Text = $"Last DialogResult: {result} ({ownerDescription})";
    }

    private static FlowLayoutPanel CreateRow(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = new Padding(0, 10, 0, 4) };
        row.Controls.AddRange(controls);
        return row;
    }

    private static Label CreateNote(string text) => new Label { AutoSize = true, MaximumSize = new Size(850, 0), Text = text };
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);
    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
    }
}
