using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class FeedbackDemoForm : Form
{
    private static readonly BootstrapVariant[] Variants =
    {
        BootstrapVariant.Primary,
        BootstrapVariant.Secondary,
        BootstrapVariant.Success,
        BootstrapVariant.Danger,
        BootstrapVariant.Warning,
        BootstrapVariant.Info,
        BootstrapVariant.Light,
        BootstrapVariant.Dark
    };

    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly List<BootstrapAlert> _dismissibleAlerts = new List<BootstrapAlert>();
    private readonly Label _dismissStatus = new Label();
    private readonly Button _restoreAlertsButton = new Button();
    private readonly BootstrapToastContainer _toastContainer = new BootstrapToastContainer();
    private readonly BootstrapToastService _toastService = new BootstrapToastService();
    private readonly Label _globalToastUnreadStatus = new Label();
    private readonly IContainer _components;
    private readonly BootstrapTooltip _defaultTooltip;
    private readonly BootstrapTooltip _semanticTooltip;
    private readonly BootstrapTooltip _customTooltip;
    private readonly BootstrapTooltip _managedTopTooltip;
    private readonly BootstrapTooltip _managedBottomEndTooltip;
    private readonly BootstrapTooltip _managedAutoTooltip;
    private readonly BootstrapPopover _interactivePopover;
    private readonly FlowLayoutPanel _interactivePopoverContent;
    private readonly Label _popoverStatus = new Label();
    private bool _ownedResourcesDisposed;
    private int _toastSequence;

    public FeedbackDemoForm()
    {
        _components = new Container();
        _defaultTooltip = new BootstrapTooltip(_components);
        _semanticTooltip = new BootstrapTooltip(_components)
        {
            Variant = BootstrapVariant.Info
        };
        _customTooltip = new BootstrapTooltip(_components)
        {
            CustomColor = Color.FromArgb(111, 66, 193)
        };
        _managedTopTooltip = new BootstrapTooltip(_components)
        {
            Positioning = BootstrapTooltipPositioning.Managed,
            Placement = BootstrapOverlayPlacement.Top,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
        };
        _managedBottomEndTooltip = new BootstrapTooltip(_components)
        {
            Positioning = BootstrapTooltipPositioning.Managed,
            Placement = BootstrapOverlayPlacement.BottomEnd,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
        };
        _managedAutoTooltip = new BootstrapTooltip(_components)
        {
            Positioning = BootstrapTooltipPositioning.Managed,
            Placement = BootstrapOverlayPlacement.Auto,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
        };
        _interactivePopover = new BootstrapPopover(_components);
        _interactivePopoverContent = CreateInteractivePopoverContent();
        _interactivePopover.Content = _interactivePopoverContent;

        Text = "Feedback Components Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 720);
        MinimumSize = new Size(640, 420);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        Controls.Add(_content);

        AddSemanticVariantsSection();
        AddShapeAndStateSection();
        AddAlertsSection();
        AddTooltipsSection();
        AddToastsSection();
        AddGlobalToastServiceSection();
        AddDpiGuidanceSection();

        _toastService.HistoryChanged += OnGlobalToastHistoryChanged;
        UpdateGlobalToastUnreadStatus();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_ownedResourcesDisposed)
        {
            _ownedResourcesDisposed = true;
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _toastService.HistoryChanged -= OnGlobalToastHistoryChanged;
            _toastService.Dispose();
            _components.Dispose();
            if (!_interactivePopoverContent.IsDisposed)
            {
                _interactivePopoverContent.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
    }

    private void AddSemanticVariantsSection()
    {
        var group = CreateGroup("Badge semantic variants");
        var row = CreateBadgeRow();
        foreach (var variant in Variants)
        {
            row.Controls.Add(new BootstrapBadge
            {
                Text = variant.ToString(),
                Variant = variant,
                AccessibleName = $"{variant} badge"
            });
        }

        group.Controls.Add(row);
        _content.Controls.Add(group);
    }

    private void AddShapeAndStateSection()
    {
        var group = CreateGroup("Badge shape, custom color, disabled, and content length");
        var stack = CreateVerticalStack();
        var shapeRow = CreateBadgeRow();
        shapeRow.Controls.Add(new BootstrapBadge { Text = "Default", Variant = BootstrapVariant.Primary, AccessibleName = "Default badge" });
        shapeRow.Controls.Add(new BootstrapBadge { Text = "Pill", Variant = BootstrapVariant.Success, Pill = true, AccessibleName = "Pill badge" });
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Custom color",
            Variant = BootstrapVariant.Danger,
            CustomColor = Color.FromArgb(111, 66, 193),
            AccessibleName = "Custom color badge"
        });
        shapeRow.Controls.Add(new BootstrapBadge { Text = "Disabled", Variant = BootstrapVariant.Secondary, Enabled = false, AccessibleName = "Disabled badge" });
        shapeRow.Controls.Add(new BootstrapBadge { Text = "Square radius", Variant = BootstrapVariant.Info, BorderRadius = 0, AccessibleName = "Square radius badge" });
        var longRow = CreateBadgeRow();
        longRow.Controls.Add(new BootstrapBadge
        {
            Text = "A long badge label verifies auto-size content measurement",
            Variant = BootstrapVariant.Warning,
            AccessibleName = "Long text badge"
        });
        stack.Controls.Add(shapeRow);
        stack.Controls.Add(longRow);
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void AddAlertsSection()
    {
        var group = CreateGroup("Alerts — semantic, icon, dismissal, multiline, and disabled states");
        var stack = CreateVerticalStack();
        stack.Controls.Add(CreateAlert(BootstrapVariant.Primary, "Primary — inline feedback with the default themed radius."));
        stack.Controls.Add(CreateAlert(BootstrapVariant.Secondary, "Secondary — lower-emphasis application feedback."));
        stack.Controls.Add(CreateAlert(BootstrapVariant.Success, "Success — changes saved successfully.", IconDescriptor.Framework(FrameworkIconGlyph.Check)));
        var danger = CreateAlert(BootstrapVariant.Danger, "Danger — an operation failed. This example is dismissible.", dismissible: true);
        WireDismissStatus(danger, "Danger");
        stack.Controls.Add(danger);
        stack.Controls.Add(CreateAlert(BootstrapVariant.Warning, "Warning — the upload has not completed.\r\nCheck the connection and try again."));
        var info = CreateAlert(
            BootstrapVariant.Info,
            "Info — keyboard users can Tab to the close affordance and activate it with Enter or Space.",
            IconDescriptor.Framework(FrameworkIconGlyph.Check),
            dismissible: true);
        WireDismissStatus(info, "Info");
        stack.Controls.Add(info);
        stack.Controls.Add(CreateAlert(BootstrapVariant.Light, "Light — contrast fallback regression example."));
        stack.Controls.Add(CreateAlert(BootstrapVariant.Dark, "Dark — contrast fallback regression example."));
        var disabled = CreateAlert(BootstrapVariant.Success, "Disabled — neutral disabled palette and no user dismissal.");
        disabled.Enabled = false;
        disabled.AccessibleName = "Disabled alert";
        stack.Controls.Add(disabled);
        var square = CreateAlert(BootstrapVariant.Info, "Custom radius — BorderRadius = 0 keeps the surface square.");
        square.BorderRadius = 0;
        square.AccessibleName = "Square radius alert";
        stack.Controls.Add(square);

        ConfigureDismissControls();
        var commandRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 4, 0, 0)
        };
        commandRow.Controls.Add(_restoreAlertsButton);
        commandRow.Controls.Add(_dismissStatus);
        stack.Controls.Add(commandRow);
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void AddTooltipsSection()
    {
        var group = CreateGroup("Tooltips — native association, themes, multiple targets, and timing");
        var stack = CreateVerticalStack();
        var targets = CreateBadgeRow();
        var defaultTarget = CreateTooltipTarget("Default dark", "Default tooltip target");
        var secondDefaultTarget = CreateTooltipTarget("Same instance", "Second default tooltip target");
        var semanticTarget = CreateTooltipTarget("Info variant", "Semantic tooltip target");
        var customTarget = CreateTooltipTarget("Custom color", "Custom tooltip target");
        var multilineTarget = CreateTooltipTarget("Multiline", "Multiline tooltip target");
        var longTarget = CreateTooltipTarget("Long text", "Long tooltip target");
        var managedTopTarget = CreateTooltipTarget("Managed Top", "Managed Top tooltip target");
        var managedBottomEndTarget = CreateTooltipTarget("Managed BottomEnd", "Managed BottomEnd tooltip target");
        var managedAutoTarget = CreateTooltipTarget("Managed Auto near edge", "Managed Auto tooltip target");
        var nativeBaselineTarget = CreateTooltipTarget("Native baseline", "Native baseline tooltip target");
        _defaultTooltip.SetToolTip(defaultTarget, "Default BootstrapTooltip using the Dark semantic variant.");
        _defaultTooltip.SetToolTip(secondDefaultTarget, "The same BootstrapTooltip instance serves this second control.");
        _semanticTooltip.SetToolTip(semanticTarget, "Semantic Info tooltip resolved from the current theme.");
        _customTooltip.SetToolTip(customTarget, "Custom purple background with contrast-selected foreground text.");
        _defaultTooltip.SetToolTip(multilineTarget, "First explicit line.\r\nSecond explicit line; BootstrapTooltip does not auto-wrap.");
        _defaultTooltip.SetToolTip(
            longTarget,
            "This deliberately long tooltip caption demonstrates native positioning with owner-drawn presentation while preserving the complete single-line caption without framework auto-wrap policy.");
        _managedTopTooltip.SetToolTip(managedTopTarget, "Managed Top with FlipAndShift collision handling.");
        _managedBottomEndTooltip.SetToolTip(managedBottomEndTarget, "Managed BottomEnd preserves logical alignment and shifts near edges.");
        _managedAutoTooltip.SetToolTip(managedAutoTarget, "Managed Auto chooses the least-overflow side deterministically.");
        _defaultTooltip.SetToolTip(nativeBaselineTarget, "Native positioning baseline remains the backward-compatible default.");
        targets.Controls.Add(defaultTarget);
        targets.Controls.Add(secondDefaultTarget);
        targets.Controls.Add(semanticTarget);
        targets.Controls.Add(customTarget);
        targets.Controls.Add(multilineTarget);
        targets.Controls.Add(longTarget);
        targets.Controls.Add(managedTopTarget);
        targets.Controls.Add(managedBottomEndTarget);
        targets.Controls.Add(managedAutoTarget);
        targets.Controls.Add(nativeBaselineTarget);
        stack.Controls.Add(targets);
        stack.Controls.Add(CreateTooltipCollisionSandbox());
        stack.Controls.Add(CreatePopoverDemoRow());
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Live native timing/state forwarding for the default tooltip:",
            Margin = new Padding(3, 6, 3, 3)
        });
        stack.Controls.Add(CreateTooltipTimingRow());
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private Panel CreateTooltipCollisionSandbox()
    {
        var sandbox = new Panel
        {
            Size = new Size(760, 150),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3, 6, 3, 8),
            AccessibleName = "Tooltip edge collision sandbox"
        };
        var topLeft = CreateTooltipTarget("Top flips", "Top edge tooltip target");
        topLeft.Location = new Point(4, 4);
        var topRight = CreateTooltipTarget("Auto", "Auto edge tooltip target");
        topRight.Location = new Point(680, 4);
        var bottomLeft = CreateTooltipTarget("Bottom flips", "Bottom edge tooltip target");
        bottomLeft.Location = new Point(4, 115);
        var bottomRight = CreateTooltipTarget("Wide shift", "Wide shift tooltip target");
        bottomRight.Location = new Point(670, 115);
        _managedTopTooltip.SetToolTip(topLeft, "Top placement should flip when the monitor working area has no room above.");
        _managedAutoTooltip.SetToolTip(topRight, "Auto near a corner selects the best visible side.");
        _managedBottomEndTooltip.SetToolTip(bottomLeft, "BottomEnd flips to TopEnd when needed.");
        _managedBottomEndTooltip.SetToolTip(bottomRight, "A deliberately wide managed tooltip shifts on its cross axis to remain inside the padded working area.");
        sandbox.Controls.Add(topLeft);
        sandbox.Controls.Add(topRight);
        sandbox.Controls.Add(bottomLeft);
        sandbox.Controls.Add(bottomRight);
        return sandbox;
    }

    private FlowLayoutPanel CreatePopoverDemoRow()
    {
        var row = CreateBadgeRow();
        var target = CreateTooltipTarget("Open interactive Popover", "Interactive Popover target");
        _interactivePopover.Target = target;
        var placement = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 100,
            AccessibleName = "Popover placement"
        };
        placement.Items.AddRange(new object[] { "Auto", "Top", "Bottom", "Left", "Right" });
        placement.SelectedIndex = 0;
        placement.SelectedIndexChanged += (_, _) =>
            _interactivePopover.Placement = (BootstrapOverlayPlacement)Enum.Parse(typeof(BootstrapOverlayPlacement), placement.SelectedItem!.ToString()!);
        var collision = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 110,
            AccessibleName = "Popover collision behavior"
        };
        collision.Items.AddRange(new object[] { "None", "Flip", "Shift", "FlipAndShift" });
        collision.SelectedIndex = 3;
        collision.SelectedIndexChanged += (_, _) =>
            _interactivePopover.CollisionBehavior = (BootstrapOverlayCollisionBehavior)Enum.Parse(typeof(BootstrapOverlayCollisionBehavior), collision.SelectedItem!.ToString()!);
        var outside = new CheckBox { AutoSize = true, Text = "Outside close", Checked = true, AccessibleName = "Popover outside close" };
        outside.CheckedChanged += (_, _) => _interactivePopover.CloseOnClickOutside = outside.Checked;
        var escape = new CheckBox { AutoSize = true, Text = "Escape close", Checked = true, AccessibleName = "Popover escape close" };
        escape.CheckedChanged += (_, _) => _interactivePopover.CloseOnEscape = escape.Checked;
        _popoverStatus.AutoSize = true;
        _popoverStatus.AccessibleName = "Popover interaction status";
        _popoverStatus.Text = "Popover action not used yet.";
        row.Controls.Add(target);
        row.Controls.Add(placement);
        row.Controls.Add(collision);
        row.Controls.Add(outside);
        row.Controls.Add(escape);
        row.Controls.Add(_popoverStatus);
        return row;
    }

    private FlowLayoutPanel CreateInteractivePopoverContent()
    {
        var content = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            MinimumSize = new Size(280, 0),
            AccessibleName = "Interactive Popover content"
        };
        var heading = new Label { AutoSize = true, Text = "Interactive settings", AccessibleName = "Popover heading" };
        var editor = new TextBox { Width = 240, Text = "Draft value", AccessibleName = "Popover text editor" };
        var checkBox = new CheckBox { AutoSize = true, Text = "Enable option", AccessibleName = "Popover option" };
        var commands = CreateBadgeRow();
        var apply = CreateActionButton("Apply", "Popover apply action", (_, _) =>
            _popoverStatus.Text = checkBox.Checked ? "Popover value applied with option." : "Popover value applied.");
        var close = CreateActionButton("Close", "Popover close action", (_, _) => _interactivePopover.Hide());
        commands.Controls.Add(apply);
        commands.Controls.Add(close);
        content.Controls.Add(heading);
        content.Controls.Add(editor);
        content.Controls.Add(checkBox);
        content.Controls.Add(commands);
        return content;
    }

    private void AddToastsSection()
    {
        var group = CreateGroup("Toasts — ownership, queueing, placement, auto-hide, and stress");
        var stack = CreateVerticalStack();
        var commands = CreateBadgeRow();
        commands.Controls.Add(CreateActionButton("Show manual Toast", "Show manual Toast", (_, _) =>
            ShowToast(BootstrapVariant.Success, "Manual", "This Toast remains until dismissed.", autoHide: false)));
        commands.Controls.Add(CreateActionButton("Show auto-hide Toast", "Show auto-hide Toast", (_, _) =>
            ShowToast(BootstrapVariant.Info, "Auto-hide", "The semantic delay starts only after the enter transition completes.", autoHide: true)));
        commands.Controls.Add(CreateActionButton("Icon + multiline", "Show icon multiline Toast", (_, _) =>
            ShowToast(
                BootstrapVariant.Warning,
                "Upload warning",
                "The upload is still pending.\r\nCheck the connection before retrying.",
                autoHide: false,
                IconDescriptor.Framework(FrameworkIconGlyph.Check))));
        commands.Controls.Add(CreateActionButton("Burst 8", "Burst 8 Toasts", (_, _) => ShowToastBurst(8)));
        commands.Controls.Add(CreateActionButton("Dismiss All", "Dismiss all Toasts", (_, _) => _toastContainer.DismissAll()));
        commands.Controls.Add(CreateActionButton("Cycle placement", "Cycle Toast placement", (_, _) => CycleToastPlacement()));
        commands.Controls.Add(CreateActionButton("Rapid show/dismiss", "Rapid show then dismiss Toast", (_, _) => RapidShowDismiss()));
        commands.Controls.Add(CreateActionButton("Disabled Toast", "Show disabled Toast", (_, _) => ShowDisabledToast()));
        commands.Controls.Add(CreateActionButton("Stress 100", "Stress 100 Toasts", (_, _) => ShowToastBurst(100)));

        _toastContainer.AccessibleName = "Toast demo container";
        _toastContainer.Size = new Size(780, 300);
        _toastContainer.Margin = new Padding(0, 4, 0, 8);
        _toastContainer.MaximumVisibleToasts = 3;
        _toastContainer.ToastSpacing = 8;
        _toastContainer.Placement = BootstrapToastPlacement.TopRight;

        stack.Controls.Add(commands);
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(780, 0),
            AccessibleName = "Toast demo guidance",
            Text = "MaximumVisibleToasts = 3 so Burst 8 demonstrates FIFO queueing. Reduced motion makes enter/exit/reflow immediate, but AutoHide still waits its semantic delay. The host must be sized for the desired stack; normal Panel clipping is authoritative.",
            Margin = new Padding(3, 4, 3, 8)
        });
        stack.Controls.Add(_toastContainer);
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(780, 0),
            AccessibleName = "Toast stress guidance",
            Text = "Resource stress: note USER/GDI/process handle counts; run repeated Stress 100 + Dismiss All cycles until several hundred Toasts have been created/disposed; return to idle and switch Light/Dark; verify no continually growing timer activity, handle climb, retained visible children, or post-disposal exceptions. Repeat with reduced motion and AutoHide enabled.",
            Margin = new Padding(3, 4, 3, 8)
        });
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void AddGlobalToastServiceSection()
    {
        var group = CreateGroup("Global Toast service and notification center");
        var stack = CreateVerticalStack();
        var notificationCommands = CreateBadgeRow();
        notificationCommands.Controls.Add(CreateActionButton("Show global Toast", "Show global Toast", (_, _) =>
            ShowGlobalToast("Global notification", "This auto-hide Toast is routed to the monitor containing this Feedback page.", autoHide: true)));
        notificationCommands.Controls.Add(CreateActionButton("Show non-auto-hide", "Show non-auto-hide Toast", (_, _) =>
            ShowGlobalToast("Persistent notification", "Dismiss this Toast explicitly; its history remains available in the center.", autoHide: false)));
        notificationCommands.Controls.Add(CreateActionButton("Burst 7", "Burst 7 notifications", (_, _) => ShowGlobalToastBurst(7)));
        notificationCommands.Controls.Add(CreateActionButton("History disabled", "Show history-disabled Toast", (_, _) =>
            _toastService.Show(new BootstrapToastOptions
            {
                Title = "Transient only",
                Text = "IncludeInHistory=false leaves the unread count unchanged.",
                Variant = BootstrapVariant.Secondary,
                AutoHide = false,
                IncludeInHistory = false
            }, this)));
        notificationCommands.Controls.Add(CreateActionButton("Long content", "Show long global Toast", (_, _) =>
            ShowGlobalToast(
                "Oversized notification",
                "This deliberately long notification exercises screen-height constraints while preserving the full text in notification history.\r\n" +
                "Move the demo between monitors and repeat at each supported DPI scale. The transient host remains bounded to the working area, while the notification center retains all content for later reading.",
                autoHide: false)));

        var centerCommands = CreateBadgeRow();
        centerCommands.Controls.Add(CreateActionButton("Open center", "Open notification center", (_, _) => _toastService.ShowNotificationCenter(this)));
        centerCommands.Controls.Add(CreateActionButton("Hide center", "Hide notification center", (_, _) => _toastService.HideNotificationCenter()));
        centerCommands.Controls.Add(CreateActionButton("Mark all read", "Mark all global notifications read", (_, _) => _toastService.MarkAllAsRead()));
        centerCommands.Controls.Add(CreateActionButton("Clear history", "Clear global notification history", (_, _) => _toastService.ClearHistory()));
        centerCommands.Controls.Add(CreateActionButton("Dismiss live Toasts", "Dismiss all global Toasts", (_, _) => _toastService.DismissAll()));

        var placementCommands = CreateBadgeRow();
        placementCommands.Controls.Add(CreatePlacementButton("Top left", "Set global Toast TopLeft", BootstrapToastPlacement.TopLeft));
        placementCommands.Controls.Add(CreatePlacementButton("Top right", "Set global Toast TopRight", BootstrapToastPlacement.TopRight));
        placementCommands.Controls.Add(CreatePlacementButton("Bottom left", "Set global Toast BottomLeft", BootstrapToastPlacement.BottomLeft));
        placementCommands.Controls.Add(CreatePlacementButton("Bottom right", "Set global Toast BottomRight", BootstrapToastPlacement.BottomRight));
        var topMost = new CheckBox
        {
            AutoSize = true,
            Text = "TopMost",
            Checked = _toastService.TopMost,
            AccessibleName = "Global Toast TopMost",
            Margin = new Padding(12, 7, 3, 3)
        };
        topMost.CheckedChanged += (_, _) => _toastService.TopMost = topMost.Checked;
        placementCommands.Controls.Add(topMost);

        var capacity = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 500,
            Value = _toastService.HistoryCapacity,
            Width = 72,
            AccessibleName = "Global Toast history capacity",
            Margin = new Padding(12, 4, 3, 3)
        };
        capacity.ValueChanged += (_, _) => _toastService.HistoryCapacity = (int)capacity.Value;
        placementCommands.Controls.Add(new Label { AutoSize = true, Text = "History capacity", Margin = new Padding(8, 8, 0, 0) });
        placementCommands.Controls.Add(capacity);

        _globalToastUnreadStatus.AutoSize = true;
        _globalToastUnreadStatus.AccessibleName = "Global Toast unread count";
        _globalToastUnreadStatus.Margin = new Padding(3, 6, 3, 4);

        stack.Controls.Add(notificationCommands);
        stack.Controls.Add(centerCommands);
        stack.Controls.Add(placementCommands);
        stack.Controls.Add(_globalToastUnreadStatus);
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(780, 0),
            AccessibleName = "Global Toast service guidance",
            Text = "All Show/Open actions pass this page as relativeTo, so routing follows the monitor containing the demo. Unread updates come from HistoryChanged without polling. Verify all placements, TopMost off/on, Light/Dark, reduced motion, Alt+F4 hide/reopen, monitor reconfiguration, keyboard focus, and rapid open/hide/show/dismiss cycles.",
            Margin = new Padding(3, 4, 3, 8)
        });
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private FlowLayoutPanel CreateTooltipTimingRow()
    {
        var row = CreateBadgeRow();
        var initialDelay = CreateTimingEditor("Initial", "Tooltip InitialDelay", _defaultTooltip.InitialDelay, 0, 60000);
        var reshowDelay = CreateTimingEditor("Reshow", "Tooltip ReshowDelay", _defaultTooltip.ReshowDelay, 0, 60000);
        var autoPopDelay = CreateTimingEditor("Auto-pop", "Tooltip AutoPopDelay", _defaultTooltip.AutoPopDelay, 1, 60000);
        var active = new CheckBox
        {
            AutoSize = true,
            Text = "Active",
            Checked = _defaultTooltip.Active,
            AccessibleName = "Tooltip Active",
            Margin = new Padding(12, 7, 3, 3)
        };
        var showAlways = new CheckBox
        {
            AutoSize = true,
            Text = "Show always",
            Checked = _defaultTooltip.ShowAlways,
            AccessibleName = "Tooltip ShowAlways",
            Margin = new Padding(8, 7, 3, 3)
        };
        initialDelay.ValueChanged += (_, _) => _defaultTooltip.InitialDelay = (int)initialDelay.Value;
        reshowDelay.ValueChanged += (_, _) => _defaultTooltip.ReshowDelay = (int)reshowDelay.Value;
        autoPopDelay.ValueChanged += (_, _) => _defaultTooltip.AutoPopDelay = (int)autoPopDelay.Value;
        active.CheckedChanged += (_, _) => _defaultTooltip.Active = active.Checked;
        showAlways.CheckedChanged += (_, _) => _defaultTooltip.ShowAlways = showAlways.Checked;
        row.Controls.Add(CreateTimingLabel("Initial (ms)"));
        row.Controls.Add(initialDelay);
        row.Controls.Add(CreateTimingLabel("Reshow (ms)"));
        row.Controls.Add(reshowDelay);
        row.Controls.Add(CreateTimingLabel("Auto-pop (ms)"));
        row.Controls.Add(autoPopDelay);
        row.Controls.Add(active);
        row.Controls.Add(showAlways);
        return row;
    }

    private void ConfigureDismissControls()
    {
        _restoreAlertsButton.AutoSize = true;
        _restoreAlertsButton.Text = "Restore dismissed alerts";
        _restoreAlertsButton.UseVisualStyleBackColor = false;
        _restoreAlertsButton.AccessibleName = "Restore dismissed alerts";
        _restoreAlertsButton.Click += (_, _) =>
        {
            foreach (var alert in _dismissibleAlerts)
            {
                alert.Visible = true;
            }
            _dismissStatus.Text = "Dismissed alerts restored.";
        };
        _dismissStatus.AutoSize = true;
        _dismissStatus.Text = "No alert dismissed yet.";
        _dismissStatus.Margin = new Padding(12, 8, 0, 0);
        _dismissStatus.AccessibleName = "Alert dismissal status";
    }

    private BootstrapAlert CreateAlert(BootstrapVariant variant, string text, IconDescriptor? icon = null, bool dismissible = false)
    {
        var multiline = text.IndexOf('\n') >= 0;
        var alert = new BootstrapAlert
        {
            Size = new Size(780, multiline ? 72 : 52),
            Margin = new Padding(0, 3, 0, 3),
            Text = text,
            Variant = variant,
            Icon = icon,
            Dismissible = dismissible,
            AccessibleName = $"{variant} alert"
        };
        if (dismissible)
        {
            _dismissibleAlerts.Add(alert);
        }
        return alert;
    }

    private void WireDismissStatus(BootstrapAlert alert, string label)
    {
        alert.Dismissed += (_, _) => _dismissStatus.Text = $"Last dismissed: {label}";
    }

    private void AddDpiGuidanceSection()
    {
        var group = CreateGroup("Theme and DPI verification");
        group.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(800, 0),
            AccessibleName = "Feedback DPI guidance",
            Text = "Use the integrated demo's Light/Dark switch while this page is open. Repeat at Windows display scaling 100%, 125%, 150%, 175%, and 200%. Verify Badge padding, Alert borders/text/icons/close focus, Tooltip presentation, and Toast title/body/icon/close glyph/stack spacing. Reduced motion should remove Toast movement while leaving AutoHide timing observable.",
            Margin = new Padding(3, 4, 3, 8)
        });
        _content.Controls.Add(group);
    }

    private void ShowToast(BootstrapVariant variant, string title, string text, bool autoHide, IconDescriptor? icon = null)
    {
        var toast = new BootstrapToast
        {
            Width = 320,
            Title = title,
            Text = text,
            Variant = variant,
            Icon = icon,
            AutoHide = autoHide,
            AutoHideDelay = 3000,
            AccessibleName = $"Demo Toast {++_toastSequence}"
        };
        _toastContainer.ShowToast(toast);
    }

    private void ShowToastBurst(int count)
    {
        var variants = new[]
        {
            BootstrapVariant.Success,
            BootstrapVariant.Warning,
            BootstrapVariant.Danger,
            BootstrapVariant.Info
        };
        for (var index = 0; index < count; index++)
        {
            ShowToast(variants[index % variants.Length], $"Burst {index + 1}", $"FIFO queue demonstration item {index + 1} of {count}.", autoHide: false);
        }
    }

    private void ShowGlobalToast(string title, string text, bool autoHide)
    {
        _toastService.Show(new BootstrapToastOptions
        {
            Title = title,
            Text = text,
            Variant = BootstrapVariant.Info,
            AutoHide = autoHide,
            AutoHideDelay = 3000
        }, this);
    }

    private void ShowGlobalToastBurst(int count)
    {
        var variants = new[]
        {
            BootstrapVariant.Success,
            BootstrapVariant.Warning,
            BootstrapVariant.Danger,
            BootstrapVariant.Info
        };
        for (var index = 0; index < count; index++)
        {
            _toastService.Show(new BootstrapToastOptions
            {
                Title = $"Global burst {index + 1}",
                Text = $"Per-screen FIFO notification {index + 1} of {count}.",
                Variant = variants[index % variants.Length],
                AutoHide = false
            }, this);
        }
    }

    private Button CreatePlacementButton(string text, string accessibleName, BootstrapToastPlacement placement)
    {
        return CreateActionButton(text, accessibleName, (_, _) => _toastService.Placement = placement);
    }

    private void OnGlobalToastHistoryChanged(object? sender, EventArgs e)
    {
        UpdateGlobalToastUnreadStatus();
    }

    private void UpdateGlobalToastUnreadStatus()
    {
        _globalToastUnreadStatus.Text = $"Unread: {_toastService.UnreadCount}";
    }

    private void CycleToastPlacement()
    {
        switch (_toastContainer.Placement)
        {
            case BootstrapToastPlacement.TopLeft:
                _toastContainer.Placement = BootstrapToastPlacement.TopRight;
                break;
            case BootstrapToastPlacement.TopRight:
                _toastContainer.Placement = BootstrapToastPlacement.BottomLeft;
                break;
            case BootstrapToastPlacement.BottomLeft:
                _toastContainer.Placement = BootstrapToastPlacement.BottomRight;
                break;
            default:
                _toastContainer.Placement = BootstrapToastPlacement.TopLeft;
                break;
        }
    }

    private void RapidShowDismiss()
    {
        var toast = new BootstrapToast
        {
            Width = 320,
            Title = "Rapid dismissal",
            Text = "Dismissed immediately after ShowToast to exercise enter interruption.",
            Variant = BootstrapVariant.Danger,
            AutoHide = false,
            AccessibleName = $"Demo Toast {++_toastSequence}"
        };
        _toastContainer.ShowToast(toast);
        toast.Dismiss();
    }

    private void ShowDisabledToast()
    {
        var toast = new BootstrapToast
        {
            Width = 320,
            Title = "Disabled Toast",
            Text = "Uses the shared neutral disabled feedback palette.",
            Variant = BootstrapVariant.Info,
            AutoHide = false,
            Enabled = false,
            AccessibleName = $"Demo Toast {++_toastSequence}"
        };
        _toastContainer.ShowToast(toast);
    }

    private static Button CreateActionButton(string text, string accessibleName, EventHandler onClick)
    {
        var button = new Button
        {
            AutoSize = true,
            Text = text,
            AccessibleName = accessibleName,
            Margin = new Padding(3, 3, 6, 3),
            UseVisualStyleBackColor = true
        };
        button.Click += onClick;
        return button;
    }

    private static Button CreateTooltipTarget(string text, string accessibleName)
    {
        return new Button
        {
            AutoSize = true,
            Text = text,
            AccessibleName = accessibleName,
            Margin = new Padding(3, 3, 6, 3),
            UseVisualStyleBackColor = true
        };
    }

    private static NumericUpDown CreateTimingEditor(string label, string accessibleName, int value, int minimum, int maximum)
    {
        return new NumericUpDown
        {
            AccessibleName = accessibleName,
            Minimum = minimum,
            Maximum = maximum,
            Value = Math.Max(minimum, Math.Min(maximum, value)),
            Width = 72,
            Margin = new Padding(0, 3, 8, 3),
            ThousandsSeparator = false,
            Tag = label
        };
    }

    private static Label CreateTimingLabel(string text)
    {
        return new Label { AutoSize = true, Text = text, Margin = new Padding(3, 7, 4, 3) };
    }

    private static GroupBox CreateGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 830,
            MinimumSize = new Size(830, 0),
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(12)
        };
    }

    private static FlowLayoutPanel CreateBadgeRow()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 2, 0, 8),
            Padding = new Padding(0, 4, 0, 4)
        };
    }

    private static FlowLayoutPanel CreateVerticalStack()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
        };
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!IsDisposed)
        {
            ApplyTheme(e.NewTheme);
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        ApplyThemeToChildren(_content, theme);
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is BootstrapBadge || child is BootstrapAlert || child is BootstrapToast || child is BootstrapToastContainer)
            {
                continue;
            }

            child.ForeColor = theme.Colors.Text;
            if (child is GroupBox || child is FlowLayoutPanel || child is Label)
            {
                child.BackColor = theme.Colors.Body;
            }

            if (child.HasChildren)
            {
                ApplyThemeToChildren(child, theme);
            }
        }
    }
}
