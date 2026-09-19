using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapModalOwnerContext
{
    public BootstrapModalOwnerContext(Form? managedOwner, IntPtr ownerHandle, Rectangle ownerBounds)
    {
        ManagedOwner = managedOwner;
        OwnerHandle = ownerHandle;
        OwnerBounds = ownerBounds;
    }

    public Form? ManagedOwner { get; }
    public IntPtr OwnerHandle { get; }
    public Rectangle OwnerBounds { get; }

    public static BootstrapModalOwnerContext Resolve(BootstrapModal modal)
    {
        var managed = modal.Owner;
        if (managed is not null && !managed.IsDisposed && managed.IsHandleCreated)
            return new BootstrapModalOwnerContext(managed, managed.Handle, managed.Bounds);

        var ownerHandle = BootstrapModalNativeWindow.GetOwner(modal.Handle);
        BootstrapModalNativeWindow.TryGetBounds(ownerHandle, out var bounds);
        return new BootstrapModalOwnerContext(null, ownerHandle, bounds);
    }
}

internal sealed class BootstrapModalNativeOwnerWindow : IWin32Window
{
    public BootstrapModalNativeOwnerWindow(IntPtr handle) => Handle = handle;
    public IntPtr Handle { get; }
}
