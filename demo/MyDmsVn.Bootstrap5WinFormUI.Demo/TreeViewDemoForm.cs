using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class TreeViewDemoForm : Form
{
    private readonly Panel _topBar = new Panel();
    private readonly TabControl _tabs = new TabControl();
    private readonly TableLayoutPanel _diagnostics = new TableLayoutPanel();
    private readonly ComboBox _variantSelector = new ComboBox();
    private readonly Label _selectedNodeDiagnostic = CreateDiagnosticLabel("TreeView diagnostic selected node");
    private readonly Label _checkedStateDiagnostic = CreateDiagnosticLabel("TreeView diagnostic checked state");
    private readonly Label _nativeEventDiagnostic = CreateDiagnosticLabel("TreeView diagnostic native event");
    private readonly Label _hitTestDiagnostic = CreateDiagnosticLabel("TreeView diagnostic hit test");
    private readonly Label _dragDiagnostic = CreateDiagnosticLabel("TreeView diagnostic drag event");
    private readonly ImageList _nodeImages = new ImageList();
    private readonly ImageList _stateImages = new ImageList();
    private readonly Font _largeNodeFont = new Font(FontFamily.GenericSansSerif, 15f, FontStyle.Bold);
    private readonly Font _accentNodeFont = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Italic);

    private BootstrapTreeView? _defaultTree;
    private BootstrapTreeView? _variantTree;
    private BootstrapTreeView? _labelEditTree;
    private BootstrapTreeView? _dragTree;
    private BootstrapTreeView? _deepTree;
    private BootstrapTreeView? _stressTree;
    private Button? _allowDropToggle;
    private bool _initialStateApplied;
    private bool _deepStateApplied;
    private int _nativeEventSequence;
    private int _dragEventSequence;

    public TreeViewDemoForm()
    {
        Text = "TreeView Demo";
        ClientSize = new Size(1100, 780);
        MinimumSize = new Size(780, 560);

        ConfigureImageLists();
        ConfigureChrome();
        BuildHierarchyTab();
        BuildPresentationTab();
        BuildInteractionTab();
        BuildLayoutAndStressTab();
        ConfigureDiagnostics();

        Controls.Add(_tabs);
        Controls.Add(_diagnostics);
        Controls.Add(_topBar);

        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (_tabs.SelectedTab?.Name == "layoutTab")
            {
                ApplyDeepInitialState();
            }
        };

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyInitialTreeState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);

        if (disposing)
        {
            _nodeImages.Dispose();
            _stateImages.Dispose();
            _largeNodeFont.Dispose();
            _accentNodeFont.Dispose();
        }
    }

    private void ConfigureChrome()
    {
        _topBar.Dock = DockStyle.Top;
        _topBar.Height = 48;
        _topBar.Padding = new Padding(12, 9, 12, 7);

        var variantLabel = new Label
        {
            AutoSize = true,
            Text = "Selected-row Variant:",
            Location = new Point(12, 15)
        };

        _variantSelector.AccessibleName = "TreeView variant selector";
        _variantSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _variantSelector.Location = new Point(145, 10);
        _variantSelector.Width = 150;
        foreach (BootstrapVariant variant in Enum.GetValues(typeof(BootstrapVariant)))
        {
            _variantSelector.Items.Add(variant);
        }
        _variantSelector.SelectedIndexChanged += (_, _) =>
        {
            if (_variantTree is not null && _variantSelector.SelectedItem is BootstrapVariant variant)
            {
                _variantTree.Variant = variant;
            }
        };
        _variantSelector.SelectedItem = BootstrapVariant.Primary;

        var themeHint = new Label
        {
            AutoSize = true,
            AccessibleName = "TreeView integrated theme instructions",
            Text = "Use the integrated shell Theme selector for Light/Dark runtime switching.",
            Location = new Point(320, 15)
        };

        _topBar.Controls.Add(variantLabel);
        _topBar.Controls.Add(_variantSelector);
        _topBar.Controls.Add(themeHint);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(14, 5);
    }

    private void ConfigureDiagnostics()
    {
        _diagnostics.Dock = DockStyle.Bottom;
        _diagnostics.Height = 126;
        _diagnostics.Padding = new Padding(10, 6, 10, 6);
        _diagnostics.ColumnCount = 2;
        _diagnostics.RowCount = 5;
        _diagnostics.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        _diagnostics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddDiagnosticRow(0, "SelectedNode", _selectedNodeDiagnostic, "No selection event yet.");
        AddDiagnosticRow(1, "Checked state", _checkedStateDiagnostic, "No check event yet.");
        AddDiagnosticRow(2, "Native event", _nativeEventDiagnostic, "No native event yet.");
        AddDiagnosticRow(3, "Hit test", _hitTestDiagnostic, "Move over a TreeView to inspect TreeView.HitTest regions.");
        AddDiagnosticRow(4, "Drag event", _dragDiagnostic, "No drag event yet.");
    }

    private void BuildHierarchyTab()
    {
        var tab = new TabPage("Hierarchy / variants") { Name = "hierarchyTab" };
        var flow = CreateScenarioFlow();
        tab.Controls.Add(flow);

        _defaultTree = CreateOrganizationTree("TreeView default organization hierarchy");
        flow.Controls.Add(CreateTreeCard("Organization hierarchy + native selection", _defaultTree, 500, 285));

        _variantTree = CreateOrganizationTree("TreeView semantic variant preview");
        flow.Controls.Add(CreateTreeCard("Semantic Variant preview", _variantTree, 500, 285));

        var checkboxes = CreateTree("TreeView checkbox permissions");
        checkboxes.CheckBoxes = true;
        var permissions = new TreeNode("Order management");
        permissions.Nodes.Add(new TreeNode("View orders") { Checked = true });
        permissions.Nodes.Add(new TreeNode("Approve discounts"));
        permissions.Nodes.Add(new TreeNode("Release shipments") { Checked = true });
        checkboxes.Nodes.Add(permissions);
        flow.Controls.Add(CreateTreeCard("CheckBoxes without StateImageList", checkboxes, 335, 235));

        _tabs.TabPages.Add(tab);
    }

    private void BuildPresentationTab()
    {
        var tab = new TabPage("Lines / images / overrides") { Name = "presentationTab" };
        var flow = CreateScenarioFlow();
        tab.Controls.Add(flow);

        var lines = CreateProductTree("TreeView lines and root lines");
        lines.ShowLines = true;
        lines.ShowRootLines = true;
        lines.ShowPlusMinus = true;
        flow.Controls.Add(CreateTreeCard("Lines + root lines + +/-", lines));

        var noRootLines = CreateProductTree("TreeView no root lines");
        noRootLines.ShowLines = true;
        noRootLines.ShowRootLines = false;
        noRootLines.ShowPlusMinus = true;
        flow.Controls.Add(CreateTreeCard("Lines, no root lines", noRootLines));

        var noPlusMinus = CreateProductTree("TreeView no plus minus");
        noPlusMinus.ShowLines = true;
        noPlusMinus.ShowRootLines = true;
        noPlusMinus.ShowPlusMinus = false;
        flow.Controls.Add(CreateTreeCard("Lines, no +/-", noPlusMinus));

        var fullRow = CreateProductTree("TreeView full row without lines");
        fullRow.ShowLines = false;
        fullRow.FullRowSelect = true;
        flow.Controls.Add(CreateTreeCard("FullRowSelect without lines", fullRow));

        var fullRowWithLines = CreateProductTree("TreeView full row with lines");
        fullRowWithLines.ShowLines = true;
        fullRowWithLines.FullRowSelect = true;
        flow.Controls.Add(CreateTreeCard("FullRowSelect with lines", fullRowWithLines));

        var stateImages = CreateTree("TreeView custom state images");
        stateImages.StateImageList = _stateImages;
        var workflow = new TreeNode("Invoice workflow") { StateImageIndex = 0 };
        workflow.Nodes.Add(new TreeNode("Draft invoice") { StateImageIndex = 0 });
        workflow.Nodes.Add(new TreeNode("Approved invoice") { StateImageIndex = 1 });
        workflow.Nodes.Add(new TreeNode("Blocked invoice") { StateImageIndex = 2 });
        stateImages.Nodes.Add(workflow);
        flow.Controls.Add(CreateTreeCard("StateImageList ImageSize = 28×20", stateImages));

        var nodeImages = CreateTree("TreeView node images");
        nodeImages.ImageList = _nodeImages;
        nodeImages.ImageKey = "folder";
        nodeImages.SelectedImageKey = "folder-open";
        var catalog = new TreeNode("Product catalog") { ImageKey = "folder", SelectedImageKey = "folder-open" };
        catalog.Nodes.Add(new TreeNode("Industrial pumps") { ImageKey = "document", SelectedImageKey = "document" });
        catalog.Nodes.Add(new TreeNode("Flow meters") { ImageKey = "document", SelectedImageKey = "document" });
        nodeImages.Nodes.Add(catalog);
        flow.Controls.Add(CreateTreeCard("Normal / selected ImageList", nodeImages));

        var overrides = CreateTree("TreeView node overrides");
        overrides.ItemHeight = 36;
        var overrideRoot = new TreeNode("Customer account overrides");
        overrideRoot.Nodes.Add(new TreeNode("Large VIP account")
        {
            NodeFont = _largeNodeFont,
            ForeColor = Color.DarkSlateBlue,
            BackColor = Color.LemonChiffon
        });
        overrideRoot.Nodes.Add(new TreeNode("Italic caller-owned note")
        {
            NodeFont = _accentNodeFont,
            ForeColor = Color.DarkGreen
        });
        overrides.Nodes.Add(overrideRoot);
        flow.Controls.Add(CreateTreeCard("Node colors/fonts + ItemHeight=36", overrides));

        var disabled = CreateProductTree("TreeView disabled");
        disabled.Enabled = false;
        flow.Controls.Add(CreateTreeCard("Disabled control", disabled));

        _tabs.TabPages.Add(tab);
    }

    private void BuildInteractionTab()
    {
        var tab = new TabPage("Interaction") { Name = "interactionTab" };
        var flow = CreateScenarioFlow();
        tab.Controls.Add(flow);

        var hotTracking = CreateProductTree("TreeView hot tracking enabled");
        hotTracking.HotTracking = true;
        flow.Controls.Add(CreateTreeCard("HotTracking = true", hotTracking));

        var noHotTracking = CreateProductTree("TreeView hot tracking disabled");
        noHotTracking.HotTracking = false;
        flow.Controls.Add(CreateTreeCard("HotTracking = false", noHotTracking));

        _labelEditTree = CreateTree("TreeView label editing");
        _labelEditTree.LabelEdit = true;
        _labelEditTree.Nodes.Add(new TreeNode("Editable cost center"));
        _labelEditTree.Nodes.Add(new TreeNode("Editable project folder"));
        var editPanel = CreateTreeWithAction(
            _labelEditTree,
            "BeginEdit selected node",
            "TreeView begin label edit",
            (_, _) =>
            {
                if (_labelEditTree?.SelectedNode is TreeNode node)
                {
                    _labelEditTree.Focus();
                    node.BeginEdit();
                }
            });
        flow.Controls.Add(CreateControlCard("Native label editing", editPanel, 360, 270));

        _dragTree = CreateTree("TreeView drag drop smoke");
        _dragTree.Nodes.Add(new TreeNode("Drag this sales region"));
        _dragTree.Nodes.Add(new TreeNode("Drop target — diagnostics only"));
        _dragTree.ItemDrag += (_, e) =>
        {
            UpdateDragEvent(_dragTree, "ItemDrag: " + (e.Item as TreeNode)?.Text);
            if (e.Item is TreeNode node)
            {
                _dragTree.DoDragDrop(node, DragDropEffects.Copy);
            }
        };
        _dragTree.DragEnter += (_, e) =>
        {
            e.Effect = HasTreeNodeData(e) ? DragDropEffects.Copy : DragDropEffects.None;
            UpdateDragEvent(_dragTree, "DragEnter " + e.Effect);
        };
        _dragTree.DragOver += (_, e) =>
        {
            e.Effect = HasTreeNodeData(e) ? DragDropEffects.Copy : DragDropEffects.None;
            UpdateDragEvent(_dragTree, "DragOver " + e.Effect);
        };
        _dragTree.DragDrop += (_, _) => UpdateDragEvent(_dragTree, "DragDrop — no reorder policy");

        var dragPanel = new Panel { Dock = DockStyle.Fill };
        _dragTree.Dock = DockStyle.Fill;
        _allowDropToggle = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            AccessibleName = "TreeView AllowDrop smoke toggle",
            Text = "Enable AllowDrop smoke",
            UseVisualStyleBackColor = false
        };
        _allowDropToggle.Click += (_, _) => ToggleAllowDrop();
        dragPanel.Controls.Add(_dragTree);
        dragPanel.Controls.Add(_allowDropToggle);
        flow.Controls.Add(CreateControlCard("ItemDrag + opt-in AllowDrop smoke", dragPanel, 360, 270));

        _tabs.TabPages.Add(tab);
    }

    private void BuildLayoutAndStressTab()
    {
        var tab = new TabPage("Scrolling / RTL / stress") { Name = "layoutTab" };
        var flow = CreateScenarioFlow();
        tab.Controls.Add(flow);

        _deepTree = CreateTree("TreeView deep scrolling hierarchy");
        var company = new TreeNode("Contoso regional operations");
        for (var regionIndex = 1; regionIndex <= 5; regionIndex++)
        {
            var region = new TreeNode("Region " + regionIndex);
            for (var branchIndex = 1; branchIndex <= 4; branchIndex++)
            {
                var branch = new TreeNode("Branch " + regionIndex + "." + branchIndex);
                var department = new TreeNode("Warehouse operations");
                department.Nodes.Add(new TreeNode("Receiving team"));
                department.Nodes.Add(new TreeNode("Dispatch team"));
                branch.Nodes.Add(department);
                branch.Nodes.Add(new TreeNode("Sales office"));
                region.Nodes.Add(branch);
            }
            company.Nodes.Add(region);
        }
        _deepTree.Nodes.Add(company);
        flow.Controls.Add(CreateTreeCard("Deep hierarchy + vertical scrolling", _deepTree, 360, 275));

        var longText = CreateTree("TreeView long text constrained width");
        longText.Nodes.Add(new TreeNode(
            "A deliberately long customer-segmentation hierarchy node used to verify constrained width, native horizontal scrolling, and right-of-label hit testing"));
        longText.Nodes.Add(new TreeNode("Short sibling"));
        flow.Controls.Add(CreateTreeCard("Long text / constrained width", longText, 330, 235));

        var horizontal = CreateTree("TreeView horizontal scroll hit testing");
        horizontal.ImageList = _nodeImages;
        horizontal.StateImageList = _stateImages;
        horizontal.ImageKey = "folder";
        horizontal.SelectedImageKey = "folder-open";
        var horizontalRoot = new TreeNode(
            "Very wide inventory hierarchy with expander, state image, node image, and enough text to force native horizontal scrolling while HitTest remains the interaction oracle")
        {
            ImageKey = "folder",
            SelectedImageKey = "folder-open",
            StateImageIndex = 1
        };
        horizontalRoot.Nodes.Add(new TreeNode("Wide child item for hit-test verification")
        {
            ImageKey = "document",
            SelectedImageKey = "document",
            StateImageIndex = 0
        });
        horizontal.Nodes.Add(horizontalRoot);
        flow.Controls.Add(CreateTreeCard("Horizontal scroll + all glyph slots", horizontal, 360, 250));

        var rtl = CreateProductTree("TreeView RTL hierarchy");
        rtl.RightToLeft = RightToLeft.Yes;
        rtl.RightToLeftLayout = true;
        rtl.ImageList = _nodeImages;
        rtl.ImageKey = "folder";
        rtl.SelectedImageKey = "folder-open";
        flow.Controls.Add(CreateTreeCard("RTL / mirrored native layout", rtl));

        _stressTree = CreateOrganizationTree("TreeView rapid stress hierarchy");
        var stressPanel = CreateTreeWithAction(
            _stressTree,
            "Rapid expand/collapse + theme cycle",
            "TreeView rapid stress",
            (_, _) => RunRapidStress());
        flow.Controls.Add(CreateControlCard("Rapid lifecycle stress", stressPanel, 500, 300));

        _tabs.TabPages.Add(tab);
    }

    private BootstrapTreeView CreateTree(string accessibleName)
    {
        var tree = new BootstrapTreeView
        {
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            HideSelection = false,
            Scrollable = true
        };
        WireDiagnostics(tree);
        return tree;
    }

    private BootstrapTreeView CreateOrganizationTree(string accessibleName)
    {
        var tree = CreateTree(accessibleName);
        tree.PathSeparator = " / ";

        var company = new TreeNode("MyDmsVn Holdings");
        var operations = new TreeNode("Operations");
        var northRegion = new TreeNode("Northern region");
        northRegion.Nodes.Add(new TreeNode("Hà Nội distribution center"));
        northRegion.Nodes.Add(new TreeNode("Hải Phòng service hub"));
        operations.Nodes.Add(northRegion);

        var southRegion = new TreeNode("Southern region");
        southRegion.Nodes.Add(new TreeNode("Hồ Chí Minh distribution center"));
        southRegion.Nodes.Add(new TreeNode("Cần Thơ service hub"));
        operations.Nodes.Add(southRegion);

        var commercial = new TreeNode("Commercial");
        var enterprise = new TreeNode("Enterprise sales");
        enterprise.Nodes.Add(new TreeNode("Key accounts"));
        enterprise.Nodes.Add(new TreeNode("Channel partners"));
        commercial.Nodes.Add(enterprise);
        commercial.Nodes.Add(new TreeNode("Customer success"));

        company.Nodes.Add(operations);
        company.Nodes.Add(commercial);

        var shared = new TreeNode("Shared services");
        shared.Nodes.Add(new TreeNode("Finance"));
        shared.Nodes.Add(new TreeNode("People operations"));
        shared.Nodes.Add(new TreeNode("Information technology"));

        tree.Nodes.Add(company);
        tree.Nodes.Add(shared);
        return tree;
    }

    private BootstrapTreeView CreateProductTree(string accessibleName)
    {
        var tree = CreateTree(accessibleName);
        var root = new TreeNode("Product families");
        var pumps = new TreeNode("Pumps");
        pumps.Nodes.Add(new TreeNode("Centrifugal"));
        pumps.Nodes.Add(new TreeNode("Positive displacement"));
        var valves = new TreeNode("Valves");
        valves.Nodes.Add(new TreeNode("Control valves"));
        valves.Nodes.Add(new TreeNode("Isolation valves"));
        root.Nodes.Add(pumps);
        root.Nodes.Add(valves);
        tree.Nodes.Add(root);
        return tree;
    }

    private static FlowLayoutPanel CreateScenarioFlow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
    }

    private static GroupBox CreateTreeCard(string title, BootstrapTreeView tree, int width = 330, int height = 235)
    {
        tree.Dock = DockStyle.Fill;
        return CreateControlCard(title, tree, width, height);
    }

    private static GroupBox CreateControlCard(string title, Control content, int width, int height)
    {
        var group = new GroupBox
        {
            Text = title,
            Size = new Size(width, height),
            Margin = new Padding(6),
            Padding = new Padding(8)
        };
        content.Dock = DockStyle.Fill;
        group.Controls.Add(content);
        return group;
    }

    private static Panel CreateTreeWithAction(
        BootstrapTreeView tree,
        string buttonText,
        string buttonAccessibleName,
        EventHandler click)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        tree.Dock = DockStyle.Fill;
        var button = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            Text = buttonText,
            AccessibleName = buttonAccessibleName,
            UseVisualStyleBackColor = false
        };
        button.Click += click;
        panel.Controls.Add(tree);
        panel.Controls.Add(button);
        return panel;
    }

    private void ApplyInitialTreeState()
    {
        if (_initialStateApplied || IsDisposed || Disposing)
        {
            return;
        }

        _initialStateApplied = true;
        ExpandAndSelectOrganizationTree(_defaultTree);
        ExpandAndSelectOrganizationTree(_variantTree);

        if (_labelEditTree is not null && _labelEditTree.Nodes.Count > 0)
        {
            _labelEditTree.SelectedNode = _labelEditTree.Nodes[0];
        }

        if (_dragTree is not null && _dragTree.Nodes.Count > 0)
        {
            _dragTree.SelectedNode = _dragTree.Nodes[0];
        }
    }

    private static void ExpandAndSelectOrganizationTree(BootstrapTreeView? tree)
    {
        if (tree is null || tree.Nodes.Count == 0)
        {
            return;
        }

        var company = tree.Nodes[0];
        company.Expand();
        if (company.Nodes.Count == 0)
        {
            tree.SelectedNode = company;
            return;
        }

        var operations = company.Nodes[0];
        operations.Expand();
        if (operations.Nodes.Count == 0)
        {
            tree.SelectedNode = operations;
            return;
        }

        var northRegion = operations.Nodes[0];
        northRegion.Expand();
        tree.SelectedNode = northRegion.Nodes.Count > 0 ? northRegion.Nodes[0] : northRegion;
    }

    private void ApplyDeepInitialState()
    {
        if (_deepStateApplied || _deepTree is null || _deepTree.Nodes.Count == 0)
        {
            return;
        }

        _deepStateApplied = true;
        var root = _deepTree.Nodes[0];
        root.Expand();
        if (root.Nodes.Count > 0)
        {
            root.Nodes[0].Expand();
            if (root.Nodes[0].Nodes.Count > 0)
            {
                root.Nodes[0].Nodes[0].Expand();
            }
        }
    }

    private void ToggleAllowDrop()
    {
        if (_dragTree is null || _allowDropToggle is null)
        {
            return;
        }

        _dragTree.AllowDrop = !_dragTree.AllowDrop;
        _allowDropToggle.Text = _dragTree.AllowDrop
            ? "Disable AllowDrop smoke"
            : "Enable AllowDrop smoke";
        UpdateDragEvent(_dragTree, "AllowDrop=" + _dragTree.AllowDrop);
    }

    private void WireDiagnostics(BootstrapTreeView tree)
    {
        tree.BeforeSelect += (_, e) => UpdateNativeEvent(tree, "BeforeSelect: " + NodeText(e.Node));
        tree.AfterSelect += (_, e) =>
        {
            _selectedNodeDiagnostic.Text = tree.AccessibleName + ": " + (e.Node?.FullPath ?? "<none>");
            UpdateNativeEvent(tree, "AfterSelect: " + NodeText(e.Node));
        };
        tree.BeforeExpand += (_, e) => UpdateNativeEvent(tree, "BeforeExpand: " + NodeText(e.Node));
        tree.AfterExpand += (_, e) => UpdateNativeEvent(tree, "AfterExpand: " + NodeText(e.Node));
        tree.BeforeCollapse += (_, e) => UpdateNativeEvent(tree, "BeforeCollapse: " + NodeText(e.Node));
        tree.AfterCollapse += (_, e) => UpdateNativeEvent(tree, "AfterCollapse: " + NodeText(e.Node));
        tree.BeforeCheck += (_, e) => UpdateNativeEvent(tree, "BeforeCheck: " + NodeText(e.Node));
        tree.AfterCheck += (_, e) =>
        {
            _checkedStateDiagnostic.Text = tree.AccessibleName + ": " + NodeText(e.Node) + " checked=" + (e.Node?.Checked ?? false);
            UpdateNativeEvent(tree, "AfterCheck: " + NodeText(e.Node));
        };
        tree.BeforeLabelEdit += (_, e) => UpdateNativeEvent(tree, "BeforeLabelEdit: " + NodeText(e.Node));
        tree.AfterLabelEdit += (_, e) => UpdateNativeEvent(tree, "AfterLabelEdit: " + NodeText(e.Node) + " label=" + (e.Label ?? "<cancel>"));
        tree.NodeMouseClick += (_, e) =>
        {
            UpdateHitTest(tree, e.Location);
            UpdateNativeEvent(tree, "NodeMouseClick: " + NodeText(e.Node) + " / " + e.Button);
        };
        tree.MouseMove += (_, e) => UpdateHitTest(tree, e.Location);
        tree.ItemDrag += (_, e) => UpdateDragEvent(tree, "ItemDrag: " + (e.Item as TreeNode)?.Text);
    }

    private void UpdateNativeEvent(BootstrapTreeView tree, string eventText)
    {
        _nativeEventSequence++;
        _nativeEventDiagnostic.Text = "#" + _nativeEventSequence + " " + tree.AccessibleName + " — " + eventText;
    }

    private void UpdateHitTest(BootstrapTreeView tree, Point location)
    {
        if (!tree.IsHandleCreated || tree.IsDisposed)
        {
            return;
        }

        var hit = tree.HitTest(location);
        _hitTestDiagnostic.Text = tree.AccessibleName + ": " + hit.Location + " / " + (hit.Node?.Text ?? "<none>");
    }

    private void UpdateDragEvent(BootstrapTreeView tree, string eventText)
    {
        _dragEventSequence++;
        _dragDiagnostic.Text = "#" + _dragEventSequence + " " + tree.AccessibleName + " — " + eventText;
    }

    private void RunRapidStress()
    {
        if (_stressTree is null || !_stressTree.IsHandleCreated || _stressTree.Nodes.Count == 0)
        {
            return;
        }

        var root = _stressTree.Nodes[0];
        for (var index = 0; index < 8; index++)
        {
            root.Expand();
            _stressTree.Refresh();
            root.Collapse();
            _stressTree.Refresh();
        }
        root.Expand();
        _stressTree.Refresh();

        var original = BootstrapThemeManager.CurrentTheme;
        var alternateMode = original.Mode == BootstrapThemeMode.Light
            ? BootstrapThemeMode.Dark
            : BootstrapThemeMode.Light;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(alternateMode, original.ReducedMotion);
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(original.Mode, original.ReducedMotion);
    }

    private void AddDiagnosticRow(int row, string title, Label value, string initialText)
    {
        value.Text = initialText;
        _diagnostics.Controls.Add(new Label
        {
            AutoSize = true,
            Text = title + ":",
            Margin = new Padding(0, 2, 8, 2)
        }, 0, row);
        _diagnostics.Controls.Add(value, 1, row);
    }

    private static Label CreateDiagnosticLabel(string accessibleName)
    {
        return new Label
        {
            AutoSize = true,
            AutoEllipsis = true,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill
        };
    }

    private static string NodeText(TreeNode? node)
    {
        return node?.Text ?? "<none>";
    }

    private static bool HasTreeNodeData(DragEventArgs e)
    {
        return e.Data is not null && e.Data.GetDataPresent(typeof(TreeNode));
    }

    private void ConfigureImageLists()
    {
        _nodeImages.ImageSize = new Size(16, 16);
        _nodeImages.TransparentColor = Color.Transparent;
        AddImage(_nodeImages, "folder", CreateGlyph(Color.SteelBlue, false));
        AddImage(_nodeImages, "folder-open", CreateGlyph(Color.SeaGreen, true));
        AddImage(_nodeImages, "document", CreateGlyph(Color.DarkGoldenrod, false));

        _stateImages.ImageSize = new Size(28, 20);
        _stateImages.TransparentColor = Color.Transparent;
        AddImage(_stateImages, "pending", CreateStateGlyph(Color.Goldenrod, false));
        AddImage(_stateImages, "approved", CreateStateGlyph(Color.SeaGreen, true));
        AddImage(_stateImages, "blocked", CreateStateGlyph(Color.IndianRed, false));
    }

    private static void AddImage(ImageList list, string key, Bitmap bitmap)
    {
        using (bitmap)
        {
            list.Images.Add(key, bitmap);
        }
    }

    private static Bitmap CreateGlyph(Color color, bool open)
    {
        var bitmap = new Bitmap(16, 16);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(color))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillRectangle(brush, 2, open ? 5 : 4, 12, open ? 8 : 9);
            graphics.FillRectangle(brush, 3, 2, 6, 4);
        }
        return bitmap;
    }

    private static Bitmap CreateStateGlyph(Color color, bool checkedState)
    {
        var bitmap = new Bitmap(28, 20);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(color))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillEllipse(brush, 5, 2, 16, 16);
            if (checkedState)
            {
                using (var pen = new Pen(Color.White, 2f))
                {
                    graphics.DrawLines(pen, new[]
                    {
                        new Point(9, 10),
                        new Point(12, 13),
                        new Point(18, 7)
                    });
                }
            }
        }
        return bitmap;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        ApplyThemeRecursive(_topBar, theme);
        ApplyThemeRecursive(_tabs, theme);
        ApplyThemeRecursive(_diagnostics, theme);
    }

    private static void ApplyThemeRecursive(Control control, BootstrapTheme theme)
    {
        if (control is BootstrapTreeView)
        {
            return;
        }

        control.ForeColor = theme.Colors.Text;
        if (control is TabPage || control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel || control is GroupBox)
        {
            control.BackColor = theme.Colors.Surface;
        }
        else if (control is Label)
        {
            control.BackColor = Color.Transparent;
        }
        else if (control is ComboBox)
        {
            control.BackColor = theme.Colors.Surface;
        }
        else if (control is Button button)
        {
            button.UseVisualStyleBackColor = false;
            button.BackColor = theme.Colors.SurfaceSecondary;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child, theme);
        }
    }
}