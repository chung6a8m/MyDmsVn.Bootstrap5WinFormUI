using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Release;

[TestFixture]
public sealed class Phase16PublicApiBaselineTests
{
    private const string ApprovedV1Fingerprint = "1da8ac7a60315c596fa34c674bd63444feaeee2c399b8adb199e295825f54f46";

    [Test]
    public void ExportedApiMatchesApprovedV1Baseline()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var api = BuildApiSurface(assembly);
        var fingerprint = ComputeSha256(api);

        Assert.That(
            fingerprint,
            Is.EqualTo(ApprovedV1Fingerprint),
            "Public API baseline changed. Review the exported API deliberately before updating the approved v1 fingerprint.\n" +
            "Actual fingerprint: " + fingerprint + "\n\n" + api);
    }

    [Test]
    public void V1CompatibilityAssemblyVersionIsStable()
    {
        Assert.That(typeof(BootstrapButton).Assembly.GetName().Version, Is.EqualTo(new Version(1, 0, 0, 0)));
    }

    [Test]
    public void BootstrapCheckableApiExportsOnlyTheReviewedContract()
    {
        var assembly = typeof(BootstrapCheckBox).Assembly;
        var types = new[] { typeof(BootstrapCheckBox), typeof(BootstrapRadioButton), typeof(BootstrapSwitch) };
        var expectedCheckBoxProtected = new[]
        {
            "Dispose", "OnAutoSizeChanged", "OnCheckStateChanged", "OnDpiChangedAfterParent", "OnEnabledChanged",
            "OnFontChanged", "OnGotFocus", "OnLostFocus", "OnMouseCaptureChanged", "OnMouseDown", "OnMouseEnter",
            "OnMouseLeave", "OnMouseUp", "OnPaddingChanged", "OnPaint", "OnTextChanged", "OnVisibleChanged"
        };
        var expectedRadioProtected = new[]
        {
            "Dispose", "OnAutoSizeChanged", "OnCheckedChanged", "OnDpiChangedAfterParent", "OnEnabledChanged",
            "OnFontChanged", "OnGotFocus", "OnLostFocus", "OnMouseCaptureChanged", "OnMouseDown", "OnMouseEnter",
            "OnMouseLeave", "OnMouseUp", "OnPaddingChanged", "OnPaint", "OnTextChanged", "OnVisibleChanged"
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapCheckBox).BaseType, Is.EqualTo(typeof(CheckBox)));
            Assert.That(typeof(BootstrapRadioButton).BaseType, Is.EqualTo(typeof(RadioButton)));
            Assert.That(typeof(BootstrapSwitch).BaseType, Is.EqualTo(typeof(CheckBox)));
            foreach (var type in types)
            {
                Assert.That(GetDeclaredPublicPropertyNames(type), Is.EqualTo(new[] { "ValidationState", "Variant" }), type.Name);
                Assert.That(GetDeclaredPublicMethodNames(type), Is.EqualTo(new[] { "GetPreferredSize" }), type.Name);
                Assert.That(GetDeclaredPublicEventNames(type), Is.Empty, type.Name);
            }
            Assert.That(GetDeclaredProtectedMethodNames(typeof(BootstrapCheckBox)), Is.EqualTo(expectedCheckBoxProtected));
            Assert.That(GetDeclaredProtectedMethodNames(typeof(BootstrapRadioButton)), Is.EqualTo(expectedRadioProtected));
            Assert.That(GetDeclaredProtectedMethodNames(typeof(BootstrapSwitch)), Is.EqualTo(expectedCheckBoxProtected));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapCheckableKind"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapCheckableRenderLogic"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapCheckableMetrics"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapCheckablePalette"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapCheckableLayout"));
        }));
    }

    [Test]
    public void BootstrapLookupExportsOnlyReviewedEntryPointsAndKeepsInfrastructureInternal()
    {
        var assembly = typeof(BootstrapLookupBox).Assembly;
        var lookup = typeof(BootstrapLookupBox);
        var requiredProperties = new[] { "ResultsGrid", "SearchTextNormalizer", "TextNormalizer", "TextComparer", "ValidationMessage" };
        var requiredMethods = new[] { "CancelPendingEdit" };
        var internalTypes = new[]
        {
            "BootstrapLookupCell", "BootstrapLookupEditingControl", "BootstrapLookupDropDownAffordance",
            "BootstrapLookupDropDownController", "BootstrapLookupDropDownContent", "BootstrapLookupFooter",
            "BootstrapLookupDataAdapter", "BootstrapLookupSearchEngine", "BootstrapLookupMemberAccessor"
        };
        var accessory = typeof(BootstrapTextBox).GetMethod("SetFrameworkTrailingAccessory", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var validation = typeof(BootstrapTextBox).GetMethod("SetTransientValidationStateOverride", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.GetEvent("ResultsChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Not.Null);
            foreach (var name in requiredProperties) Assert.That(lookup.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Not.Null, name);
            foreach (var name in requiredMethods) Assert.That(lookup.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Not.Null, name);
            foreach (var name in internalTypes)
            {
                var type = assembly.GetTypes().Single(candidate => candidate.Name == name);
                Assert.That(type.IsPublic || type.IsNestedPublic, Is.False, name);
            }
            Assert.That(accessory, Is.Not.Null); Assert.That(accessory!.IsAssembly, Is.True);
            Assert.That(validation, Is.Not.Null); Assert.That(validation!.IsAssembly, Is.True);
        }));
    }

    [Test]
    public void BootstrapSelectCustomResultRenderingApiExportsOnlyTheReviewedContract()
    {
        const BindingFlags declaredInstance = BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;
        var resultRowHeight = typeof(BootstrapSelect).GetProperty(
            nameof(BootstrapSelect.ResultRowHeight),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var visibleDpiMethods = typeof(BootstrapSelect).GetMethods(declaredInstance)
            .Where(method => !method.IsSpecialName
                             && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly)
                             && method.Name.IndexOf("Dpi", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(method => method.Name)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(resultRowHeight, Is.Not.Null);
            Assert.That(resultRowHeight!.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(typeof(BootstrapSelect).GetMember("MeasureResultItem", declaredInstance), Is.Empty);
            Assert.That(typeof(BootstrapSelect).GetProperty("ItemTemplate", declaredInstance), Is.Null);
            Assert.That(visibleDpiMethods, Is.Empty);
        }));
    }

    [Test]
    public void FormattedInputApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapFormattedTextBox).Assembly;
        var formattedExports = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Where(name => name is not null &&
                           (name.IndexOf("FormattedTextBox", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("InputFormatter", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("FormatOptions", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("InputFormatMode", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("NumeralGroupStyle", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("CreditCardType", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("BootstrapTimeFormat", StringComparison.Ordinal) >= 0))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var editor = typeof(BootstrapTextBox).GetProperty(
            "Editor",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var editorTextChanged = typeof(BootstrapTextBox).GetMethod(
            "OnEditorTextChanged",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var editorKeyDown = typeof(BootstrapTextBox).GetMethod(
            "OnEditorKeyDown",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(formattedExports, Is.EqualTo(new[]
            {
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapFormattedTextBox",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapCreditCardFormatOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapCreditCardInputFormatter",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapCreditCardType",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapDateFormatOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapDateInputFormatter",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapGeneralFormatOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapGeneralInputFormatter",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapInputFormatMode",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapNumeralFormatOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapNumeralGroupStyle",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapNumeralInputFormatter",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapTimeFormat",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapTimeFormatOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.BootstrapTimeInputFormatter",
                "MyDmsVn.Bootstrap5WinFormUI.Formatting.IInputFormatter"
            }));
            Assert.That(GetDeclaredPublicPropertyNames(typeof(BootstrapFormattedTextBox)), Is.EqualTo(new[]
            {
                "CreditCardOptions", "CreditCardType", "DateOptions", "FormatMode", "Formatter",
                "GeneralOptions", "NumeralOptions", "RawValue", "Text", "TimeOptions"
            }));
            Assert.That(GetDeclaredPublicEventNames(typeof(BootstrapFormattedTextBox)),
                Is.EqualTo(new[] { "CreditCardTypeChanged", "RawValueChanged" }));
            Assert.That(GetDeclaredPublicMethodNames(typeof(BootstrapFormattedTextBox)), Is.EqualTo(new[] { "Reformat" }));
            Assert.That(editor, Is.Not.Null);
            Assert.That(editor!.GetMethod!.IsFamily, Is.True);
            Assert.That(editorTextChanged, Is.Not.Null);
            Assert.That(editorTextChanged!.IsFamily, Is.True);
            Assert.That(editorKeyDown, Is.Not.Null);
            Assert.That(editorKeyDown!.IsFamily, Is.True);
            Assert.That(typeof(BootstrapTextBox).GetProperty("Editor", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapTextBox).GetMethod("OnEditorTextChanged", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapTextBox).GetMethod("OnEditorKeyDown", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("InputCaretMapper"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("FormattedTextSnapshot"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("FormattedTextHistory"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("InputFormatOptionValidation"));
        }));
    }

    [Test]
    public void InputGroupApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapInputGroup).Assembly;
        var inputGroupExports = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Where(name => name is not null && name.IndexOf("InputGroup", StringComparison.Ordinal) >= 0)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var connectedMemberNames = new[]
        {
            "ConnectedCornerRadius", "ConnectedSizeOverride", "GetConnectedSafeMinimumHeight"
        };
        var connectedControls = new[]
        {
            typeof(BootstrapButton), typeof(BootstrapTextBox), typeof(BootstrapNumericBox),
            typeof(BootstrapSelect), typeof(BootstrapSplitButton), typeof(BootstrapInputGroupText)
        };
        const BindingFlags declaredVisible = BindingFlags.Instance | BindingFlags.Public |
                                             BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Enum.GetValues(typeof(BootstrapInputGroupSize)), Is.EqualTo(new[]
            {
                BootstrapInputGroupSize.Small,
                BootstrapInputGroupSize.Default,
                BootstrapInputGroupSize.Large
            }));
            Assert.That(inputGroupExports, Is.EqualTo(new[]
            {
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroup",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupSize",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupText"
            }));
            Assert.That(GetDeclaredPublicPropertyNames(typeof(BootstrapInputGroup)), Is.EqualTo(new[] { "InputGroupSize" }));
            Assert.That(GetDeclaredPublicMethodNames(typeof(BootstrapInputGroup)), Is.EqualTo(new[] { "GetPreferredSize" }));
            Assert.That(GetDeclaredPublicEventNames(typeof(BootstrapInputGroup)), Is.Empty);
            Assert.That(GetDeclaredProtectedMethodNames(typeof(BootstrapInputGroup)), Is.EqualTo(new[]
            {
                "CreateControlsInstance", "Dispose", "OnDpiChangedAfterParent", "OnLayout", "OnRightToLeftChanged"
            }));
            Assert.That(GetDeclaredPublicPropertyNames(typeof(BootstrapInputGroupText)), Is.EqualTo(new[]
            {
                "BorderRadius", "Icon", "IconRenderer", "Text", "TextAlign"
            }));
            Assert.That(GetDeclaredPublicMethodNames(typeof(BootstrapInputGroupText)), Is.EqualTo(new[] { "GetPreferredSize" }));
            Assert.That(GetDeclaredPublicEventNames(typeof(BootstrapInputGroupText)), Is.Empty);
            Assert.That(GetDeclaredProtectedMethodNames(typeof(BootstrapInputGroupText)), Is.EqualTo(new[]
            {
                "Dispose", "OnFontChanged", "OnPaint", "OnTextChanged"
            }));

            foreach (var type in connectedControls)
            {
                var visibleConnectedMembers = type.GetMembers(declaredVisible)
                    .Where(member => connectedMemberNames.Contains(member.Name) &&
                                     (member is MethodBase method && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly) ||
                                      member is PropertyInfo property &&
                                      ((property.GetMethod?.IsPublic ?? false) || (property.GetMethod?.IsFamily ?? false) ||
                                       (property.GetMethod?.IsFamilyOrAssembly ?? false) || (property.SetMethod?.IsPublic ?? false) ||
                                       (property.SetMethod?.IsFamily ?? false) || (property.SetMethod?.IsFamilyOrAssembly ?? false))))
                    .ToArray();
                Assert.That(visibleConnectedMembers, Is.Empty, type.Name + " must implement connected members explicitly.");
            }

            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("IBootstrapConnectedControl"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapConnectedControlSize"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapConnectedControlLayoutLogic"));
            Assert.That(assembly.GetExportedTypes().Select(type => type.Name), Does.Not.Contain("BootstrapInputGroupLayoutLogic"));
        }));
    }

    [Test]
    public void OverlayApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapPopover).Assembly;
        var exportedNames = assembly.GetExportedTypes().Select(type => type.FullName).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapPopover).GetProperty(nameof(BootstrapPopover.Content))!.PropertyType, Is.EqualTo(typeof(Control)));
            Assert.That(typeof(BootstrapTooltip).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapTooltip).GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementEngine"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Rendering.BootstrapOverlayPlacementRequest"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlayDropDown"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapOverlaySurface"));
        }));
    }

    [Test]
    public void AdvancedDropdownApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapDropdown).Assembly;
        var exportedNames = assembly.GetExportedTypes().Select(type => type.FullName).ToArray();
        var splitProperties = typeof(BootstrapSplitButton)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var splitMethods = typeof(BootstrapSplitButton)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Enum.GetValues(typeof(BootstrapDropdownItemKind)), Is.EqualTo(new[]
            {
                BootstrapDropdownItemKind.Item,
                BootstrapDropdownItemKind.Separator,
                BootstrapDropdownItemKind.HostedControl
            }));
            Assert.That(typeof(BootstrapDropdownItem).GetProperty(nameof(BootstrapDropdownItem.DropDownItems)), Is.Not.Null);
            Assert.That(typeof(BootstrapDropdownItem).GetProperty(nameof(BootstrapDropdownItem.HostedControlFactory)), Is.Not.Null);
            Assert.That(splitProperties, Is.EqualTo(new[]
            {
                "BorderRadius", "ButtonSize", "Icon", "IconPosition", "IconRenderer", "Items",
                "Loading", "LoadingText", "MinimumWidth", "Outline", "Text", "Variant"
            }));
            Assert.That(splitMethods, Is.EqualTo(new[] { "CloseDropDown", "GetPreferredSize", "ShowDropDown" }));
            Assert.That(typeof(BootstrapSplitButton).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(eventInfo => eventInfo.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(new[] { "Closed", "Opened" }));
            Assert.That(typeof(BootstrapSplitButton).GetProperty("Font", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(typeof(BootstrapSplitButton).GetProperty("AccessibleName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(splitProperties.Any(name => name.IndexOf("Button", StringComparison.Ordinal) >= 0 && name != "ButtonSize"), Is.False);
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapConnectedButtonLayoutLogic"));
            Assert.That(exportedNames, Does.Not.Contain("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapDropdownRenderer"));
        }));
    }

    [Test]
    public void CalendarApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapCalendar).Assembly;
        var calendarProperties = GetDeclaredPublicPropertyNames(typeof(BootstrapCalendar));
        var pickerProperties = GetDeclaredPublicPropertyNames(typeof(BootstrapCalendarPicker));
        var calendarMethods = GetDeclaredPublicMethodNames(typeof(BootstrapCalendar));
        var pickerMethods = GetDeclaredPublicMethodNames(typeof(BootstrapCalendarPicker));

        var calendarExports = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Where(name => name is not null && name.IndexOf("BootstrapCalendar", StringComparison.Ordinal) >= 0)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Enum.GetValues(typeof(BootstrapCalendarSelectionMode)), Is.EqualTo(new[]
            {
                BootstrapCalendarSelectionMode.Single,
                BootstrapCalendarSelectionMode.Range,
                BootstrapCalendarSelectionMode.Multiple
            }));
            Assert.That(calendarExports, Is.EqualTo(new[]
            {
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapCalendar",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapCalendarPicker",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapCalendarSelectionMode"
            }));
            Assert.That(calendarProperties, Is.EqualTo(new[]
            {
                "BorderRadius", "DisplayMonth", "MaxDate", "MinDate", "RangeEnd", "RangeStart", "SelectedDate", "SelectedDates", "SelectionMode"
            }));
            Assert.That(pickerProperties, Is.EqualTo(new[]
            {
                "BorderRadius", "DateFormat", "MaxDate", "MinDate", "PlaceholderText", "RangeEnd", "RangeStart", "SelectedDate", "SelectedDates", "SelectionMode", "ValidationState"
            }));
            Assert.That(calendarMethods, Is.EqualTo(new[]
            {
                "ClearSelection", "GetPreferredSize", "SetRange", "SetSelectedDates", "ShowNextMonth", "ShowPreviousMonth"
            }));
            Assert.That(pickerMethods, Is.EqualTo(new[]
            {
                "ClearSelection", "CloseDropDown", "SetRange", "SetSelectedDates", "ShowDropDown"
            }));
            Assert.That(GetDeclaredPublicEventNames(typeof(BootstrapCalendar)), Is.EqualTo(new[] { "DisplayMonthChanged", "SelectionChanged" }));
            Assert.That(GetDeclaredPublicEventNames(typeof(BootstrapCalendarPicker)), Is.EqualTo(new[] { "Closed", "Opened", "SelectionChanged" }));
            Assert.That(typeof(BootstrapCalendar).GetEvent("SelectionActivated", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapCalendar).GetProperty("FocusedDate", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(BootstrapCalendarPicker).GetProperty("ActiveCalendar", BindingFlags.Instance | BindingFlags.Public), Is.Null);
        }));
    }

    [Test]
    public void GlobalToastApiExportsOnlyTheReviewedPublicContract()
    {
        var assembly = typeof(BootstrapToastService).Assembly;
        var toastExports = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Where(name => name is not null &&
                           (name.IndexOf("Toast", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("Notification", StringComparison.Ordinal) >= 0))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toastExports, Is.EqualTo(new[]
            {
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToast",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastContainer",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastHistoryItem",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastOptions",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastPlacement",
                "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapToastService"
            }));
            Assert.That(GetDeclaredPublicPropertyNames(typeof(BootstrapToastOptions)), Is.EqualTo(new[]
            {
                "AnimationDuration", "AutoHide", "AutoHideDelay", "Dismissible", "Icon",
                "IncludeInHistory", "Text", "Title", "Variant"
            }));
            Assert.That(GetDeclaredPublicPropertyNames(typeof(BootstrapToastHistoryItem)), Is.EqualTo(new[]
            {
                "CreatedAtUtc", "Id", "IsRead", "Text", "Title", "Variant"
            }));
            Assert.That(GetDeclaredPublicMethodNames(typeof(BootstrapToastService)), Is.EqualTo(new[]
            {
                "ClearHistory", "DismissAll", "Dispose", "GetHistory", "HideNotificationCenter",
                "MarkAllAsRead", "MarkAsRead", "Show", "Show", "ShowNotificationCenter", "ToggleNotificationCenter"
            }));
            Assert.That(toastExports.Any(name => name!.IndexOf("Host", StringComparison.Ordinal) >= 0), Is.False);
            Assert.That(toastExports.Any(name => name!.IndexOf("Window", StringComparison.Ordinal) >= 0), Is.False);
        }));
    }

    private static string[] GetDeclaredPublicPropertyNames(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetDeclaredPublicMethodNames(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetDeclaredPublicEventNames(Type type)
    {
        return type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(eventInfo => eventInfo.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetDeclaredProtectedMethodNames(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName && (method.IsFamily || method.IsFamilyOrAssembly))
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildApiSurface(Assembly assembly)
    {
        var lines = new List<string>();

        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add(FormatTypeDeclaration(type));

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var constructor in type.GetConstructors(flags).Where(IsVisible).OrderBy(FormatConstructor, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatConstructor(constructor));
            }

            foreach (var field in type.GetFields(flags).Where(IsVisible).OrderBy(FormatField, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatField(field));
            }

            foreach (var property in type.GetProperties(flags).Where(IsVisible).OrderBy(FormatProperty, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatProperty(property));
            }

            foreach (var eventInfo in type.GetEvents(flags).Where(IsVisible).OrderBy(FormatEvent, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatEvent(eventInfo));
            }

            foreach (var method in type.GetMethods(flags)
                         .Where(method => !method.IsSpecialName && IsVisible(method))
                         .OrderBy(FormatMethod, StringComparer.Ordinal))
            {
                lines.Add("  " + FormatMethod(method));
            }
        }

        return string.Join("\n", lines);
    }

    private static string FormatTypeDeclaration(Type type)
    {
        if (type.IsEnum)
        {
            return "enum " + FormatType(type) + " : " + FormatType(Enum.GetUnderlyingType(type));
        }

        if (type.IsInterface)
        {
            return "interface " + FormatType(type);
        }

        var kind = type.IsValueType ? "struct " : "class ";
        var baseType = type.BaseType;
        return kind + FormatType(type) + (baseType is null ? string.Empty : " : " + FormatType(baseType));
    }

    private static string FormatConstructor(ConstructorInfo constructor)
    {
        return Visibility(constructor) + " ctor(" + string.Join(",", constructor.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatField(FieldInfo field)
    {
        var constant = field.IsLiteral ? " = " + Convert.ToString(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
        return Visibility(field) + " field " + FormatType(field.FieldType) + " " + field.Name + constant;
    }

    private static string FormatProperty(PropertyInfo property)
    {
        var accessors = property.GetAccessors(nonPublic: true).Where(IsVisible).Select(Visibility).Distinct().OrderBy(value => value, StringComparer.Ordinal);
        var indexParameters = property.GetIndexParameters();
        var index = indexParameters.Length == 0 ? string.Empty : "[" + string.Join(",", indexParameters.Select(FormatParameter)) + "]";
        return string.Join("/", accessors) + " property " + FormatType(property.PropertyType) + " " + property.Name + index;
    }

    private static string FormatEvent(EventInfo eventInfo)
    {
        var methods = new[] { eventInfo.AddMethod, eventInfo.RemoveMethod }.Where(method => method is not null).Cast<MethodInfo>().Where(IsVisible);
        return string.Join("/", methods.Select(Visibility).Distinct().OrderBy(value => value, StringComparer.Ordinal)) +
               " event " + FormatType(eventInfo.EventHandlerType!) + " " + eventInfo.Name;
    }

    private static string FormatMethod(MethodInfo method)
    {
        var genericArity = method.IsGenericMethodDefinition ? "`" + method.GetGenericArguments().Length : string.Empty;
        return Visibility(method) + " method " + FormatType(method.ReturnType) + " " + method.Name + genericArity +
               "(" + string.Join(",", method.GetParameters().Select(FormatParameter)) + ")";
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var modifier = parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? "ref " : string.Empty;
        var parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        var optional = parameter.IsOptional ? " optional" : string.Empty;
        return modifier + FormatType(parameterType) + optional;
    }

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
        {
            return FormatType(type.GetElementType()!) + "&";
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return "!" + type.GenericParameterPosition;
        }

        if (!type.IsGenericType)
        {
            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        var definition = type.GetGenericTypeDefinition();
        var definitionName = (definition.FullName ?? definition.Name).Replace('+', '.');
        var tickIndex = definitionName.IndexOf('`');
        if (tickIndex >= 0)
        {
            definitionName = definitionName.Substring(0, tickIndex);
        }

        return definitionName + "<" + string.Join(",", type.GetGenericArguments().Select(FormatType)) + ">";
    }

    private static bool IsVisible(MethodBase method)
    {
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    private static bool IsVisible(FieldInfo field)
    {
        return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
    }

    private static bool IsVisible(PropertyInfo property)
    {
        return property.GetAccessors(nonPublic: true).Any(IsVisible);
    }

    private static bool IsVisible(EventInfo eventInfo)
    {
        return (eventInfo.AddMethod is not null && IsVisible(eventInfo.AddMethod)) ||
               (eventInfo.RemoveMethod is not null && IsVisible(eventInfo.RemoveMethod));
    }

    private static string Visibility(MethodBase method)
    {
        if (method.IsPublic)
        {
            return "public";
        }

        return method.IsFamilyOrAssembly ? "protected-internal" : "protected";
    }

    private static string Visibility(FieldInfo field)
    {
        if (field.IsPublic)
        {
            return "public";
        }

        return field.IsFamilyOrAssembly ? "protected-internal" : "protected";
    }

    private static string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
