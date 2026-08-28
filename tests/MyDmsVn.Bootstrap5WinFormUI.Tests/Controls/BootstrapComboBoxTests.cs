using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapComboBoxTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void NativeComboBoxCharacterizationPreservesUnboundSelectionAndEvents()
    {
        using var comboBox = new ComboBox();
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        var selectedIndexChanged = 0;
        var selectedValueChanged = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectedIndexChanged++;
        comboBox.SelectedValueChanged += (_, _) => selectedValueChanged++;

        comboBox.SelectedIndex = 1;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.EqualTo("Beta"));
            Assert.That(comboBox.Text, Is.EqualTo("Beta"));
            Assert.That(selectedIndexChanged, Is.EqualTo(1));
            Assert.That(selectedValueChanged, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeComboBoxCharacterizationPreservesBindingAndEditableConfiguration()
    {
        var source = CreateLookupSource();
        using var form = new Form { ShowInTaskbar = false };
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DisplayMember = nameof(LookupItem.Name),
            ValueMember = nameof(LookupItem.Id),
            DataSource = source
        };
        form.Controls.Add(comboBox);
        form.Show();
        Application.DoEvents();

        comboBox.SelectedValue = 20;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DataSource, Is.SameAs(source));
            Assert.That(comboBox.DisplayMember, Is.EqualTo(nameof(LookupItem.Name)));
            Assert.That(comboBox.ValueMember, Is.EqualTo(nameof(LookupItem.Id)));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.SameAs(source[1]));
            Assert.That(comboBox.SelectedValue, Is.EqualTo(20));
            Assert.That(comboBox.GetItemText(source[1]), Is.EqualTo("Twenty"));
            Assert.That(comboBox.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
            Assert.That(comboBox.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.SuggestAppend));
            Assert.That(comboBox.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.ListItems));
        }));
    }

    [Test]
    public void DefaultsUseNativeOwnerDrawContractWithoutChangingNativeState()
    {
        using var comboBox = new BootstrapComboBox();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DrawMode, Is.EqualTo(DrawMode.OwnerDrawFixed));
            Assert.That(comboBox.FlatStyle, Is.EqualTo(FlatStyle.Flat));
            Assert.That(comboBox.IntegralHeight, Is.True);
            Assert.That(comboBox.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(comboBox.BorderRadius, Is.EqualTo(-1));
            Assert.That(comboBox.LeadingIcon, Is.Null);
            Assert.That(comboBox.IconRenderer, Is.Not.Null);
            Assert.That(comboBox.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(-1));
            Assert.That(comboBox.DataSource, Is.Null);
        }));

        var rendererProperty = typeof(BootstrapComboBox).GetProperty(nameof(BootstrapComboBox.IconRenderer));
        Assert.That(rendererProperty, Is.Not.Null);
        Assert.That(rendererProperty!.GetCustomAttribute<BrowsableAttribute>()?.Browsable, Is.False);
        Assert.That(
            rendererProperty.GetCustomAttribute<DesignerSerializationVisibilityAttribute>()?.Visibility,
            Is.EqualTo(DesignerSerializationVisibility.Hidden));
    }

    [Test]
    public void PublicDeclaredSurfaceContainsOnlyPlannedMembers()
    {
        var names = typeof(BootstrapComboBox)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member =>
                member.MemberType is MemberTypes.Constructor or MemberTypes.Event or MemberTypes.Property ||
                member is MethodInfo method && !method.IsSpecialName)
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(names, Is.EqualTo(new[]
        {
            ".ctor",
            "BorderRadius",
            "IconRenderer",
            "LeadingIcon",
            "ValidationState"
        }));
    }

    [Test]
    public void FrameworkPresentationPropertiesRejectInvalidValuesBeforeMutation()
    {
        using var comboBox = new BootstrapComboBox();
        var originalRenderer = comboBox.IconRenderer;

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => comboBox.ValidationState = (BootstrapValidationState)999));
        Assert.That(comboBox.ValidationState, Is.EqualTo(BootstrapValidationState.None));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => comboBox.BorderRadius = -2));
        Assert.That(comboBox.BorderRadius, Is.EqualTo(-1));

        Assert.Throws<ArgumentNullException>((Action)(() => comboBox.IconRenderer = null!));
        Assert.That(comboBox.IconRenderer, Is.SameAs(originalRenderer));
    }

    [Test]
    public void FrameworkPresentationChangesDoNotMutateNativeSelectionOrRaiseSelectionEvents()
    {
        using var comboBox = new BootstrapComboBox();
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        comboBox.SelectedIndex = 1;
        var selectedIndexChanged = 0;
        var selectedValueChanged = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectedIndexChanged++;
        comboBox.SelectedValueChanged += (_, _) => selectedValueChanged++;
        var renderer = new RecordingIconRenderer();

        comboBox.ValidationState = BootstrapValidationState.Valid;
        comboBox.BorderRadius = 8;
        comboBox.LeadingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        comboBox.IconRenderer = renderer;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.EqualTo("Beta"));
            Assert.That(comboBox.Text, Is.EqualTo("Beta"));
            Assert.That(selectedIndexChanged, Is.EqualTo(0));
            Assert.That(selectedValueChanged, Is.EqualTo(0));
        }));
    }

    [Test]
    public void NativeBindingMembersRemainCanonicalAndFunctional()
    {
        var source = CreateLookupSource();
        using var form = new Form { ShowInTaskbar = false };
        var comboBox = new BootstrapComboBox
        {
            DisplayMember = nameof(LookupItem.Name),
            ValueMember = nameof(LookupItem.Id),
            DataSource = source
        };
        form.Controls.Add(comboBox);
        form.Show();
        Application.DoEvents();

        comboBox.SelectedValue = 20;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DataSource, Is.SameAs(source));
            Assert.That(comboBox.DisplayMember, Is.EqualTo(nameof(LookupItem.Name)));
            Assert.That(comboBox.ValueMember, Is.EqualTo(nameof(LookupItem.Id)));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.SameAs(source[1]));
            Assert.That(comboBox.SelectedValue, Is.EqualTo(20));
            Assert.That(comboBox.GetItemText(source[1]), Is.EqualTo("Twenty"));
        }));
    }

    [Test]
    public void HandleRecreationPreservesNativeDataSelectionStyleAndAutoCompleteState()
    {
        var source = CreateLookupSource();
        using var form = new Form { ShowInTaskbar = false };
        var comboBox = new TestBootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DisplayMember = nameof(LookupItem.Name),
            ValueMember = nameof(LookupItem.Id),
            DataSource = source
        };
        form.Controls.Add(comboBox);
        form.Show();
        Application.DoEvents();
        comboBox.SelectedValue = 20;
        Application.DoEvents();

        comboBox.ForceHandleRecreation();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DrawMode, Is.EqualTo(DrawMode.OwnerDrawFixed));
            Assert.That(comboBox.DataSource, Is.SameAs(source));
            Assert.That(comboBox.DisplayMember, Is.EqualTo(nameof(LookupItem.Name)));
            Assert.That(comboBox.ValueMember, Is.EqualTo(nameof(LookupItem.Id)));
            Assert.That(comboBox.SelectedValue, Is.EqualTo(20));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
            Assert.That(comboBox.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.SuggestAppend));
            Assert.That(comboBox.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.ListItems));
        }));
    }

    [Test]
    public void OwnerDrawUsesDisplayMemberAndLeadingIconOnlyForClosedSelectionArea()
    {
        using var comboBox = new TestBootstrapComboBox
        {
            DisplayMember = nameof(ThrowingToStringLookupItem.Name),
            LeadingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
            Size = new Size(220, 32)
        };
        var renderer = new RecordingIconRenderer();
        comboBox.IconRenderer = renderer;
        comboBox.Items.Add(new ThrowingToStringLookupItem("Display member text"));
        comboBox.SelectedIndex = 0;
        var externalDrawCount = 0;
        comboBox.DrawItem += (_, _) => externalDrawCount++;

        using var bitmap = new Bitmap(220, 64);
        using var graphics = Graphics.FromImage(bitmap);

        Assert.DoesNotThrow((Action)(() => comboBox.DrawForTest(
            graphics,
            new Rectangle(0, 0, 220, 32),
            0,
            DrawItemState.Selected)));
        Assert.That(renderer.CallCount, Is.EqualTo(0));

        Assert.DoesNotThrow((Action)(() => comboBox.DrawForTest(
            graphics,
            new Rectangle(0, 32, 220, 32),
            0,
            DrawItemState.ComboBoxEdit)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.CallCount, Is.EqualTo(1));
            Assert.That(renderer.LastDescriptor, Is.SameAs(comboBox.LeadingIcon));
            Assert.That(renderer.LastBounds.Width, Is.GreaterThan(0));
            Assert.That(renderer.LastBounds.Height, Is.GreaterThan(0));
            Assert.That(externalDrawCount, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DrawToBitmapSmokeSupportsValidationDisabledAndExplicitRadiusStates()
    {
        using var comboBox = new BootstrapComboBox { Size = new Size(220, 32) };
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 0;
        using var bitmap = new Bitmap(comboBox.Width, comboBox.Height);

        Assert.DoesNotThrow((Action)(() => comboBox.DrawToBitmap(bitmap, comboBox.ClientRectangle)));

        comboBox.ValidationState = BootstrapValidationState.Valid;
        Assert.DoesNotThrow((Action)(() => comboBox.DrawToBitmap(bitmap, comboBox.ClientRectangle)));

        comboBox.ValidationState = BootstrapValidationState.Invalid;
        comboBox.BorderRadius = 8;
        Assert.DoesNotThrow((Action)(() => comboBox.DrawToBitmap(bitmap, comboBox.ClientRectangle)));

        comboBox.Enabled = false;
        Assert.DoesNotThrow((Action)(() => comboBox.DrawToBitmap(bitmap, comboBox.ClientRectangle)));
    }

    [Test]
    public void RuntimeThemeSwitchUpdatesThemeOwnedFontAndPaletteWithoutChangingSelection()
    {
        using var comboBox = new BootstrapComboBox();
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 1;
        var baseTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var typography = new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
            baseTheme.Typography.BodySmall,
            baseTheme.Typography.Label,
            baseTheme.Typography.HeadingSmall,
            baseTheme.Typography.HeadingMedium);

        BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
            BootstrapThemeMode.Dark,
            baseTheme.Colors,
            baseTheme.Metrics,
            typography);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
            Assert.That(comboBox.Font.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(comboBox.BackColor, Is.EqualTo(baseTheme.Colors.Surface));
            Assert.That(comboBox.ForeColor, Is.EqualTo(baseTheme.Colors.Text));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.EqualTo("Beta"));
        }));
    }

    [Test]
    public void CallerAssignedFontRemainsCallerOwnedAcrossThemeChangesAndDispose()
    {
        using var callerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        var comboBox = new BootstrapComboBox { Font = callerFont };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.That(comboBox.Font, Is.SameAs(callerFont));
        comboBox.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void DisposalReleasesThemeSubscriptionAndThemeOwnedFont()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var comboBox = new BootstrapComboBox();
        var ownedFont = comboBox.Font;

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        comboBox.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)));

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", ownedFont)));
    }

    [Test]
    public void RepeatedLifecycleStressDoesNotLeakStaticThemeHandlers()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();

        for (var index = 0; index < 50; index++)
        {
            using var comboBox = new BootstrapComboBox
            {
                BorderRadius = index % 10,
                ValidationState = (BootstrapValidationState)(index % 3),
                LeadingIcon = index % 2 == 0 ? IconDescriptor.Framework(FrameworkIconGlyph.Check) : null
            };
            comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
            comboBox.SelectedIndex = index % 2;

            if (index % 10 == 0)
            {
                var mode = BootstrapThemeManager.CurrentTheme.Mode == BootstrapThemeMode.Light
                    ? BootstrapThemeMode.Dark
                    : BootstrapThemeMode.Light;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            }
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
    }

    private static List<LookupItem> CreateLookupSource()
    {
        return new List<LookupItem>
        {
            new LookupItem(10, "Ten"),
            new LookupItem(20, "Twenty")
        };
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class TestBootstrapComboBox : BootstrapComboBox
    {
        public void ForceHandleRecreation()
        {
            RecreateHandle();
        }

        public void DrawForTest(Graphics graphics, Rectangle bounds, int index, DrawItemState state)
        {
            var args = new DrawItemEventArgs(graphics, Font, bounds, index, state, ForeColor, BackColor);
            OnDrawItem(args);
        }
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public int CallCount { get; private set; }

        public IconDescriptor? LastDescriptor { get; private set; }

        public Rectangle LastBounds { get; private set; }

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            CallCount++;
            LastDescriptor = descriptor;
            LastBounds = bounds;
            return true;
        }
    }

    private sealed class LookupItem
    {
        public LookupItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    private sealed class ThrowingToStringLookupItem
    {
        public ThrowingToStringLookupItem(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            throw new InvalidOperationException("Owner draw must use GetItemText instead of ToString().");
        }
    }
}
