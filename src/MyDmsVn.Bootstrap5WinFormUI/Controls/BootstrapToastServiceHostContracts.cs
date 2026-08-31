using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapToastHostSettings
{
    public BootstrapToastHostSettings(
        BootstrapToastPlacement placement,
        int toastSpacing,
        int maximumVisibleToasts,
        Padding screenMargin,
        bool topMost)
    {
        BootstrapToastLayoutLogic.ValidatePlacement(placement);
        if (toastSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toastSpacing), toastSpacing, "Toast spacing cannot be negative.");
        }

        if (maximumVisibleToasts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisibleToasts), maximumVisibleToasts, "Maximum visible toasts must be greater than zero.");
        }

        if (screenMargin.Left < 0 || screenMargin.Top < 0 || screenMargin.Right < 0 || screenMargin.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(screenMargin), screenMargin, "Screen margin edges cannot be negative.");
        }

        Placement = placement;
        ToastSpacing = toastSpacing;
        MaximumVisibleToasts = maximumVisibleToasts;
        ScreenMargin = screenMargin;
        TopMost = topMost;
    }

    public BootstrapToastPlacement Placement { get; }

    public int ToastSpacing { get; }

    public int MaximumVisibleToasts { get; }

    public Padding ScreenMargin { get; }

    public bool TopMost { get; }
}

internal interface IBootstrapToastHostWindow : IDisposable
{
    string ScreenDeviceName { get; }

    bool HasOwnedToasts { get; }

    event EventHandler? BecameEmpty;

    void ApplySettings(BootstrapToastScreenInfo screen, BootstrapToastHostSettings settings);

    void ShowToast(BootstrapToast toast);

    void DismissAll();

    void RetireForScreenRemoval();
}

internal interface IBootstrapToastHostWindowFactory
{
    IBootstrapToastHostWindow Create();
}

internal sealed class BootstrapToastHostWindowFactory : IBootstrapToastHostWindowFactory
{
    public IBootstrapToastHostWindow Create()
    {
        return new BootstrapToastHostWindow();
    }
}
