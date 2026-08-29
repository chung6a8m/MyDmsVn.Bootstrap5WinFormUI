using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapSelectHitTarget
{
    None,
    Content,
    Arrow,
    Clear,
    Chip,
    ChipRemove
}

internal readonly struct BootstrapSelectHitTestInfo
{
    internal BootstrapSelectHitTestInfo(BootstrapSelectHitTarget target, BootstrapSelectItem? item, Rectangle bounds)
    {
        Target = target;
        Item = item;
        Bounds = bounds;
    }

    internal BootstrapSelectHitTarget Target { get; }
    internal BootstrapSelectItem? Item { get; }
    internal Rectangle Bounds { get; }
}
