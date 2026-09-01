using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal interface IBootstrapConnectedControl
{
    CornerRadius? ConnectedCornerRadius { get; set; }

    BootstrapConnectedControlSize? ConnectedSizeOverride { get; set; }

    int GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize size, int dpi);
}
