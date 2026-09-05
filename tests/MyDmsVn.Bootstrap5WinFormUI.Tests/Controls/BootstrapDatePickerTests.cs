using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDatePickerTests
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
    public void NativeDefaultsAreCharacterizedForStage9()
    {
        var before = DateTime.Now;
        using var native = new DateTimePicker();
        var after = DateTime.Now;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Value, Is.InRange(before, after));
            Assert.That(native.MinDate, Is.EqualTo(DateTimePicker.MinimumDateTime));
            Assert.That(native.Format, Is.EqualTo(DateTimePickerFormat.Long));
            Assert.That(native.CustomFormat, Is.Null);
            Assert.That(native.ShowCheckBox, Is.False);
            Assert.That(native.Checked, Is.True);
            Assert.That(native.ShowUpDown, Is.False);
        }));
    }

    [Test]
    public void NativeRangeFormatAndCheckboxSemanticsAreCharacterizedForStage9()
    {
        var minimum = new DateTime(2020, 1, 1);
        var maximum = new DateTime(2030, 12, 31);
        var sample = new DateTime(2026, 8, 28, 10, 30, 0);
        using var native = new DateTimePicker
        {
            MinDate = minimum,
            MaxDate = maximum,
            Value = sample,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            ShowCheckBox = true,
            Checked = false
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.MinDate, Is.EqualTo(minimum));
            Assert.That(native.MaxDate, Is.EqualTo(maximum));
            Assert.That(native.Value, Is.EqualTo(sample));
            Assert.That(native.Format, Is.EqualTo(DateTimePickerFormat.Custom));
            Assert.That(native.CustomFormat, Is.EqualTo("yyyy-MM-dd HH:mm"));
            Assert.That(native.ShowCheckBox, Is.True);
            Assert.That(native.Checked, Is.False);
        }));

        native.Checked = true;
        Assert.That(native.Checked, Is.True);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => native.Value = maximum.AddDays(1)));
        Assert.Throws<InvalidEnumArgumentException>((Action)(() => native.Format = (DateTimePickerFormat)999));
    }

    [Test]
    public void DefaultsAndMetadataMatchNativeBackedContract()
    {
        var before = DateTime.Now;
        using var nativePeer = new DateTimePicker();
        using var input = new BootstrapDatePicker();
        var after = DateTime.Now;
        var native = GetNativePicker(input);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Controls.Count, Is.EqualTo(1));
            Assert.That(native.TabStop, Is.False);
            Assert.That(native.ShowUpDown, Is.False);
            Assert.That(input.TabStop, Is.True);
            Assert.That(input.AccessibleRole, Is.EqualTo(AccessibleRole.DropList));
            Assert.That(input.AccessibleDescription, Is.EqualTo("Bootstrap-inspired date picker."));
            Assert.That(input.Value, Is.InRange(before, after));
            Assert.That(input.MinDate, Is.EqualTo(nativePeer.MinDate));
            Assert.That(input.MaxDate, Is.EqualTo(nativePeer.MaxDate));
            Assert.That(input.Format, Is.EqualTo(nativePeer.Format));
            Assert.That(input.CustomFormat, Is.EqualTo(nativePeer.CustomFormat));
            Assert.That(input.ShowCheckBox, Is.EqualTo(nativePeer.ShowCheckBox));
            Assert.That(input.Checked, Is.EqualTo(nativePeer.Checked));
            Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(input.BorderRadius, Is.EqualTo(-1));
        }));

        Assert.That(
            typeof(BootstrapDatePicker).GetCustomAttribute<DefaultPropertyAttribute>()?.Name,
            Is.EqualTo(nameof(BootstrapDatePicker.Value)));
        Assert.That(
            typeof(BootstrapDatePicker).GetCustomAttribute<DefaultEventAttribute>()?.Name,
            Is.EqualTo(nameof(BootstrapDatePicker.ValueChanged)));
    }

    [Test]
    public void PublicDeclaredSurfaceContainsOnlyPlannedMembers()
    {
        var names = typeof(BootstrapDatePicker)
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
            "Checked",
            "CustomFormat",
            "Format",
            "MaxDate",
            "MinDate",
            "ShowCheckBox",
            "ValidationState",
            "Value",
            "ValueChanged"
        }));
    }

    [Test]
    public void NativeStatePropertiesForwardDirectly()
    {
        var minimum = new DateTime(2020, 1, 1);
        var maximum = new DateTime(2030, 12, 31);
        var sample = new DateTime(2026, 8, 28, 10, 30, 0);
        using var input = new BootstrapDatePicker();
        var native = GetNativePicker(input);

        input.MinDate = minimum;
        input.MaxDate = maximum;
        input.Value = sample;
        input.Format = DateTimePickerFormat.Custom;
        input.CustomFormat = "yyyy-MM-dd HH:mm";
        input.ShowCheckBox = true;
        input.Checked = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.MinDate, Is.EqualTo(native.MinDate));
            Assert.That(input.MaxDate, Is.EqualTo(native.MaxDate));
            Assert.That(input.Value, Is.EqualTo(native.Value));
            Assert.That(input.Format, Is.EqualTo(native.Format));
            Assert.That(input.CustomFormat, Is.EqualTo(native.CustomFormat));
            Assert.That(input.ShowCheckBox, Is.EqualTo(native.ShowCheckBox));
            Assert.That(input.Checked, Is.EqualTo(native.Checked));
            Assert.That(native.ShowUpDown, Is.False);
        }));

        input.CustomFormat = null!;
        Assert.That(native.CustomFormat, Is.Null);
    }

    [Test]
    public void NativeRangeAndFormatExceptionsArePreserved()
    {
        using var input = new BootstrapDatePicker
        {
            MinDate = new DateTime(2020, 1, 1),
            MaxDate = new DateTime(2030, 12, 31),
            Value = new DateTime(2026, 8, 28)
        };
        using var native = new DateTimePicker
        {
            MinDate = input.MinDate,
            MaxDate = input.MaxDate,
            Value = input.Value
        };

        var wrapperValueException = Assert.Catch((Action)(() => input.Value = input.MaxDate.AddDays(1)));
        var nativeValueException = Assert.Catch((Action)(() => native.Value = native.MaxDate.AddDays(1)));
        var wrapperFormatException = Assert.Catch((Action)(() => input.Format = (DateTimePickerFormat)999));
        var nativeFormatException = Assert.Catch((Action)(() => native.Format = (DateTimePickerFormat)999));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(wrapperValueException?.GetType(), Is.EqualTo(nativeValueException?.GetType()));
            Assert.That(wrapperFormatException?.GetType(), Is.EqualTo(nativeFormatException?.GetType()));
        }));
    }

    [Test]
    public void ValidationAndRadiusRejectInvalidValuesBeforeMutationAndPreserveNativeState()
    {
        var sample = new DateTime(2026, 8, 28);
        using var input = new BootstrapDatePicker { Value = sample };

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.ValidationState = (BootstrapValidationState)999));
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        Assert.That(input.Value, Is.EqualTo(sample));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.BorderRadius = -2));
        Assert.That(input.BorderRadius, Is.EqualTo(-1));
        Assert.That(input.Value, Is.EqualTo(sample));

        input.ValidationState = BootstrapValidationState.Valid;
        input.BorderRadius = 6;
        Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
        Assert.That(input.BorderRadius, Is.EqualTo(6));
        Assert.That(input.Value, Is.EqualTo(sample));
    }

    [Test]
    public void ValueChangedUsesExactlyOneNativeEventPathAndWrapperSender()
    {
        using var input = new BootstrapDatePicker
        {
            MinDate = new DateTime(2020, 1, 1),
            MaxDate = new DateTime(2030, 12, 31),
            Value = new DateTime(2026, 8, 28)
        };
        var native = GetNativePicker(input);
        var count = 0;
        object? senderSeen = null;

        input.ValueChanged += (sender, _) =>
        {
            count++;
            senderSeen = sender;
        };

        input.Value = new DateTime(2026, 8, 29);
        input.Value = new DateTime(2026, 8, 29);
        native.Value = new DateTime(2026, 8, 30);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(senderSeen, Is.SameAs(input));
            Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 8, 30)));
        }));
    }

    [Test]
    public void NativeRangeDrivenValueChangedParityIsPreserved()
    {
        var initial = new DateTime(2026, 8, 28);
        var newMinimum = new DateTime(2026, 9, 1);
        using var input = new BootstrapDatePicker
        {
            MinDate = new DateTime(2020, 1, 1),
            MaxDate = new DateTime(2030, 12, 31),
            Value = initial
        };
        using var native = new DateTimePicker
        {
            MinDate = input.MinDate,
            MaxDate = input.MaxDate,
            Value = initial
        };
        var wrapperEvents = 0;
        var nativeEvents = 0;
        input.ValueChanged += (_, _) => wrapperEvents++;
        native.ValueChanged += (_, _) => nativeEvents++;

        var wrapperException = RecordException(() => input.MinDate = newMinimum);
        var nativeException = RecordException(() => native.MinDate = newMinimum);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(wrapperException?.GetType(), Is.EqualTo(nativeException?.GetType()));
            Assert.That(input.MinDate, Is.EqualTo(native.MinDate));
            Assert.That(input.Value, Is.EqualTo(native.Value));
            Assert.That(wrapperEvents, Is.EqualTo(nativeEvents));
        }));
    }

    [Test]
    public void CheckboxStateAndEventCountsMatchNativePeer()
    {
        using var input = new BootstrapDatePicker { ShowCheckBox = true };
        using var native = new DateTimePicker { ShowCheckBox = true };
        var wrapperEvents = 0;
        var nativeEvents = 0;
        input.ValueChanged += (_, _) => wrapperEvents++;
        native.ValueChanged += (_, _) => nativeEvents++;

        input.Checked = false;
        native.Checked = false;
        input.Checked = true;
        native.Checked = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Checked, Is.EqualTo(native.Checked));
            Assert.That(wrapperEvents, Is.EqualTo(nativeEvents));
        }));
    }

    [Test]
    public void NativeKeyboardEventsAreForwardedThroughWrapperOnce()
    {
        using var input = new BootstrapDatePicker();
        var native = GetNativePicker(input);
        var downCount = 0;
        var pressCount = 0;
        var upCount = 0;
        var previewCount = 0;
        input.KeyDown += (_, e) =>
        {
            downCount++;
            e.Handled = true;
        };
        input.KeyPress += (_, e) =>
        {
            pressCount++;
            e.Handled = true;
        };
        input.KeyUp += (_, _) => upCount++;
        input.PreviewKeyDown += (_, e) =>
        {
            previewCount++;
            e.IsInputKey = true;
        };

        var down = new KeyEventArgs(Keys.Down);
        var press = new KeyPressEventArgs('1');
        var up = new KeyEventArgs(Keys.Down);
        var preview = new PreviewKeyDownEventArgs(Keys.Down);
        InvokeProtected(native, "OnKeyDown", down);
        InvokeProtected(native, "OnKeyPress", press);
        InvokeProtected(native, "OnKeyUp", up);
        InvokeProtected(native, "OnPreviewKeyDown", preview);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(downCount, Is.EqualTo(1));
            Assert.That(pressCount, Is.EqualTo(1));
            Assert.That(upCount, Is.EqualTo(1));
            Assert.That(previewCount, Is.EqualTo(1));
            Assert.That(down.Handled, Is.True);
            Assert.That(press.Handled, Is.True);
            Assert.That(preview.IsInputKey, Is.True);
        }));
    }

    [Test]
    public void WrapperOwnsSingleTabStopAndEnteringTransfersFocusToNativePicker()
    {
        using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual };
        using var before = new Button { TabIndex = 0 };
        using var input = new BootstrapDatePicker { TabIndex = 1 };
        using var after = new Button { TabIndex = 2 };
        var native = GetNativePicker(input);
        form.Controls.Add(before);
        form.Controls.Add(input);
        form.Controls.Add(after);
        form.Show();
        form.Activate();

        Assert.That(input.TabStop, Is.True);
        Assert.That(native.TabStop, Is.False);

        input.Select();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Focused, Is.True);
            Assert.That(input.ContainsFocus, Is.True);
        }));
    }

    [Test]
    public void LayoutUsesPureBoundsAndKeepsNativePickerInsideNormalClient()
    {
        using var input = new BootstrapDatePicker { Size = new Size(280, 40) };
        var native = GetNativePicker(input);

        input.PerformLayout();
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapDatePickerRenderLogic.ResolveMetrics(theme.Metrics, input.DeviceDpi, input.BorderRadius);
        var preferredHeight = Math.Max(1, native.PreferredSize.Height > 0 ? native.PreferredSize.Height : native.Height);
        var expected = BootstrapDatePickerRenderLogic.CalculateNativeBounds(input.ClientSize, preferredHeight, metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Bounds, Is.EqualTo(expected));
            Assert.That(input.ClientRectangle.Contains(native.Bounds), Is.True);
            Assert.That(native.Font, Is.SameAs(input.Font));
        }));
    }

    [Test]
    public void DrawToBitmapSmokeSupportsThemeValidationDisabledAndRadiusStates()
    {
        using var input = new BootstrapDatePicker { Size = new Size(280, 40) };
        using var bitmap = new Bitmap(input.Width, input.Height);

        Assert.DoesNotThrow((Action)(() => input.DrawToBitmap(bitmap, input.ClientRectangle)));
        input.ValidationState = BootstrapValidationState.Valid;
        Assert.DoesNotThrow((Action)(() => input.DrawToBitmap(bitmap, input.ClientRectangle)));
        input.ValidationState = BootstrapValidationState.Invalid;
        input.BorderRadius = 8;
        Assert.DoesNotThrow((Action)(() => input.DrawToBitmap(bitmap, input.ClientRectangle)));
        input.Enabled = false;
        Assert.DoesNotThrow((Action)(() => input.DrawToBitmap(bitmap, input.ClientRectangle)));

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        input.Enabled = true;
        Assert.DoesNotThrow((Action)(() => input.DrawToBitmap(bitmap, input.ClientRectangle)));
    }

    [Test]
    public void RuntimeThemeSwitchUpdatesThemeOwnedFontAndPreservesNativeDateState()
    {
        var sample = new DateTime(2026, 8, 28, 10, 30, 0);
        using var input = new BootstrapDatePicker
        {
            MinDate = new DateTime(2020, 1, 1),
            MaxDate = new DateTime(2030, 12, 31),
            Value = sample,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            ShowCheckBox = true,
            Checked = false
        };
        var native = GetNativePicker(input);
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
            Assert.That(input.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
            Assert.That(input.Font.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(native.Font, Is.SameAs(input.Font));
            Assert.That(input.MinDate, Is.EqualTo(new DateTime(2020, 1, 1)));
            Assert.That(input.MaxDate, Is.EqualTo(new DateTime(2030, 12, 31)));
            Assert.That(input.Value, Is.EqualTo(sample));
            Assert.That(input.Format, Is.EqualTo(DateTimePickerFormat.Custom));
            Assert.That(input.CustomFormat, Is.EqualTo("yyyy-MM-dd"));
            Assert.That(input.ShowCheckBox, Is.True);
            Assert.That(input.Checked, Is.False);
        }));
    }

    [Test]
    public void CallerAssignedFontRemainsCallerOwnedAcrossThemeChangesAndDispose()
    {
        using var callerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        var input = new BootstrapDatePicker { Font = callerFont };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.That(input.Font, Is.SameAs(callerFont));
        Assert.That(GetNativePicker(input).Font, Is.SameAs(callerFont));
        input.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void DisposalReleasesThemeSubscriptionAndThemeOwnedFont()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var input = new BootstrapDatePicker();
        var ownedFont = input.Font;

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        input.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)));

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", ownedFont)));
    }

    [Test]
    public void CultureSensitiveTextMatchesPlainNativePicker()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            using var input = new BootstrapDatePicker
            {
                Value = new DateTime(2026, 8, 28),
                Format = DateTimePickerFormat.Long
            };
            using var native = new DateTimePicker
            {
                Value = input.Value,
                Format = input.Format
            };

            Assert.That(GetNativePicker(input).Text, Is.EqualTo(native.Text));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void NativeCalendarTransitionEventsRemainAvailableOnOwnedPicker()
    {
        using var input = new BootstrapDatePicker();
        var native = GetNativePicker(input);
        var opened = 0;
        var closed = 0;
        native.DropDown += (_, _) => opened++;
        native.CloseUp += (_, _) => closed++;

        InvokeProtected(native, "OnDropDown", EventArgs.Empty);
        InvokeProtected(native, "OnCloseUp", EventArgs.Empty);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void RepeatedLifecycleStressDoesNotLeakStaticThemeHandlersOrDuplicateChildren()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();

        for (var index = 0; index < 50; index++)
        {
            using var input = new BootstrapDatePicker
            {
                MinDate = new DateTime(2020, 1, 1),
                MaxDate = new DateTime(2030, 12, 31),
                Value = new DateTime(2026, 1, 1).AddDays(index),
                BorderRadius = index % 10,
                ValidationState = (BootstrapValidationState)(index % 3),
                ShowCheckBox = index % 2 == 0,
                Checked = index % 3 != 0
            };

            input.PerformLayout();
            Assert.That(input.Controls.OfType<DateTimePicker>().Count(), Is.EqualTo(1));

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

    private static DateTimePicker GetNativePicker(BootstrapDatePicker input)
    {
        return input.Controls.OfType<DateTimePicker>().Single();
    }

    private static void InvokeProtected(Control control, string methodName, EventArgs args)
    {
        var method = control.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Could not find protected method {methodName}.");
        method!.Invoke(control, new object[] { args });
    }

    private static Exception? RecordException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }
}
