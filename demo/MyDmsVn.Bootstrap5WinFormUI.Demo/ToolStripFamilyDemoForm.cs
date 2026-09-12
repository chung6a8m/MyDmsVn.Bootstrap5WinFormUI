using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ToolStripFamilyDemoForm : DemoFormBase
{
    private readonly BootstrapMenuStrip _menuStrip = new BootstrapMenuStrip { Variant = BootstrapVariant.Primary };
    private readonly BootstrapToolStrip _toolStrip = new BootstrapToolStrip { Variant = BootstrapVariant.Success };
    private readonly BootstrapContextMenuStrip _contextMenu = new BootstrapContextMenuStrip { Variant = BootstrapVariant.Warning };
    private readonly BootstrapStatusStrip _statusStrip = new BootstrapStatusStrip { Variant = BootstrapVariant.Info, SizingGrip = true };
    private readonly BootstrapToolStrip _verticalToolStrip = new BootstrapToolStrip { Dock = DockStyle.Right, LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow, Variant = BootstrapVariant.Secondary };
    private readonly Panel _content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
    private readonly ListBox _eventLog = new ListBox { Dock = DockStyle.Fill };
    private readonly Image _nativeImage;
    private readonly Font _headingFont;

    public ToolStripFamilyDemoForm()
    {
        Text = "Menus / ToolStrips";
        ClientSize = new Size(980, 620);
        _nativeImage = SystemIcons.Information.ToBitmap();
        _headingFont = new Font(Font, FontStyle.Bold);
        ConfigureMenu();
        ConfigureToolStrips();
        ConfigureContextMenu();
        ConfigureStatus();
        ConfigureContent();
        Controls.Add(_content);
        Controls.Add(_verticalToolStrip);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);
        MainMenuStrip = _menuStrip;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _contextMenu.Dispose();
            _nativeImage.Dispose();
            _headingFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ConfigureMenu()
    {
        var file = new ToolStripMenuItem("&File");
        var save = new ToolStripMenuItem("&Save") { ShortcutKeys = Keys.Control | Keys.S };
        save.Click += (_, _) => Log("File / Save clicked through native shortcut routing");
        var checkedItem = new ToolStripMenuItem("Checked option") { CheckOnClick = true, Checked = true };
        checkedItem.CheckedChanged += (_, _) => Log("Checked option = " + checkedItem.Checked);
        var nested = new ToolStripMenuItem("Nested");
        nested.DropDownItems.Add("Child command", null, (_, _) => Log("Nested child clicked"));
        file.DropDownItems.AddRange(new ToolStripItem[]
        {
            save,
            checkedItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Disabled") { Enabled = false },
            nested
        });
        file.DropDownOpening += (_, _) => Log("File menu opening");
        file.DropDownClosed += (_, _) => Log("File menu closed");
        _menuStrip.Items.Add(file);
    }

    private void ConfigureToolStrips()
    {
        _toolStrip.Dock = DockStyle.Top;
        _toolStrip.ShowItemToolTips = true;
        _toolStrip.Items.Add(new ToolStripButton("New", _nativeImage, (_, _) => Log("New clicked")) { ToolTipText = "Native image and tooltip" });
        _toolStrip.Items.Add(new ToolStripButton("Pinned") { CheckOnClick = true, Checked = true });
        _toolStrip.Items.Add(new ToolStripSeparator());
        var dropdown = new ToolStripDropDownButton("Actions");
        dropdown.DropDownItems.Add("Refresh", null, (_, _) => Log("Refresh clicked"));
        _toolStrip.Items.Add(dropdown);
        var split = new ToolStripSplitButton("Run");
        split.ButtonClick += (_, _) => Log("Split main action");
        split.DropDownItems.Add("Run safe", null, (_, _) => Log("Split drop-down action"));
        _toolStrip.Items.Add(split);
        for (var index = 1; index <= 10; index++)
        {
            _toolStrip.Items.Add(new ToolStripButton("Overflow " + index) { Overflow = ToolStripItemOverflow.AsNeeded });
        }

        _verticalToolStrip.Items.Add(new ToolStripButton("Up"));
        _verticalToolStrip.Items.Add(new ToolStripSeparator());
        _verticalToolStrip.Items.Add(new ToolStripButton("Down"));
    }

    private void ConfigureContextMenu()
    {
        var checkedItem = new ToolStripMenuItem("Checked") { Checked = true, CheckOnClick = true };
        var nested = new ToolStripMenuItem("More");
        nested.DropDownItems.Add("Nested action", null, (_, _) => Log("Context nested action"));
        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            checkedItem,
            new ToolStripMenuItem("Disabled") { Enabled = false },
            new ToolStripSeparator(),
            nested
        });
        _contextMenu.Opening += (_, _) => Log("Context opening for " + (_contextMenu.SourceControl?.Name ?? "unknown"));
        _contextMenu.Closed += (_, _) => Log("Context closed");
        _content.Name = "ToolStrip demo content";
        _content.ContextMenuStrip = _contextMenu;
    }

    private void ConfigureStatus()
    {
        var left = new ToolStripStatusLabel("Ready");
        var spring = new ToolStripStatusLabel("Right-click the workspace") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        var bordered = new ToolStripStatusLabel("Bordered") { BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right, BorderStyle = Border3DStyle.RaisedOuter };
        var commands = new ToolStripDropDownButton("Status menu");
        commands.DropDownItems.Add("Command", null, (_, _) => Log("Status command"));
        var split = new ToolStripSplitButton("Sync");
        split.ButtonClick += (_, _) => Log("Status sync");
        split.DropDownItems.Add("Sync options");
        var progress = new ToolStripProgressBar { Minimum = 0, Maximum = 100, Value = 65, Style = ProgressBarStyle.Continuous };
        _statusStrip.Items.AddRange(new ToolStripItem[] { left, spring, bordered, commands, split, progress });
    }

    private void ConfigureContent()
    {
        var heading = new Label { Dock = DockStyle.Top, Height = 48, Text = "Native WinForms behavior, Bootstrap presentation", Font = _headingFont };
        var instructions = new Label { Dock = DockStyle.Top, Height = 50, Text = "Try Ctrl+S, checked commands, nested menus, toolbar overflow, the vertical separator, and the context menu." };
        var logGroup = new GroupBox { Dock = DockStyle.Fill, Text = "Native event log", Padding = new Padding(12) };
        logGroup.Controls.Add(_eventLog);
        _content.Controls.Add(logGroup);
        _content.Controls.Add(instructions);
        _content.Controls.Add(heading);
    }

    private void Log(string message)
    {
        _eventLog.Items.Insert(0, DateTime.Now.ToLongTimeString() + "  " + message);
    }
}
