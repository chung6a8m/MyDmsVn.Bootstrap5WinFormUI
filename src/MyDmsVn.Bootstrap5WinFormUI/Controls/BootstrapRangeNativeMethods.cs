using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapRangeNativePart
{
    Unknown,
    Ticks,
    Thumb,
    Channel
}

internal static class BootstrapRangeNativeMethods
{
    internal const int WmReflectNotify = 0x204E;
    internal const int NmCustomDraw = -12;
    internal const uint CddsPrePaint = 0x00000001;
    internal const uint CddsItemPrePaint = 0x00010001;
    internal const int CdrfDoDefault = 0x00000000;
    internal const int CdrfNotifyItemDraw = 0x00000020;
    internal const int CdrfSkipDefault = 0x00000004;
    internal const uint CdisSelected = 0x0001;
    internal const uint CdisDisabled = 0x0004;
    internal const uint CdisFocus = 0x0010;
    internal const uint CdisHot = 0x0040;
    private const int TbmGetThumbRect = 0x0419;
    private const int TbmGetTicPos = 0x040F;
    private const int TbmGetNumTics = 0x0410;
    private const int TbmGetChannelRect = 0x041A;

    internal static bool TryReadCustomDraw(
        IntPtr parameter,
        IntPtr expectedWindow,
        out BootstrapRangeNativeCustomDraw customDraw)
    {
        customDraw = default;
        if (parameter == IntPtr.Zero)
        {
            return false;
        }

        customDraw = Marshal.PtrToStructure<BootstrapRangeNativeCustomDraw>(parameter);
        return customDraw.Header.Code == NmCustomDraw &&
               (expectedWindow == IntPtr.Zero || customDraw.Header.WindowFrom == expectedWindow);
    }

    internal static BootstrapRangeNativePart ClassifyPart(UIntPtr itemSpec)
    {
        switch (itemSpec.ToUInt64())
        {
            case 1UL:
                return BootstrapRangeNativePart.Ticks;
            case 2UL:
                return BootstrapRangeNativePart.Thumb;
            case 3UL:
                return BootstrapRangeNativePart.Channel;
            default:
                return BootstrapRangeNativePart.Unknown;
        }
    }

    internal static Rectangle GetThumbRectangle(IntPtr trackBarHandle)
    {
        if (trackBarHandle == IntPtr.Zero)
        {
            return Rectangle.Empty;
        }

        var rectangle = default(BootstrapRangeNativeRectangle);
        SendMessage(trackBarHandle, TbmGetThumbRect, IntPtr.Zero, ref rectangle);
        return rectangle.ToRectangle();
    }

    internal static Rectangle GetChannelRectangle(IntPtr trackBarHandle)
    {
        if (trackBarHandle == IntPtr.Zero)
        {
            return Rectangle.Empty;
        }

        var rectangle = default(BootstrapRangeNativeRectangle);
        SendMessage(trackBarHandle, TbmGetChannelRect, IntPtr.Zero, ref rectangle);
        return rectangle.ToRectangle();
    }

    internal static int[] GetIntermediateTickPositions(IntPtr trackBarHandle)
    {
        if (trackBarHandle == IntPtr.Zero)
        {
            return Array.Empty<int>();
        }

        var nativeTickCount = (int)SendMessage(trackBarHandle, TbmGetNumTics, IntPtr.Zero, IntPtr.Zero).ToInt64();
        var intermediateCount = Math.Max(0, nativeTickCount - 2);
        var positions = new List<int>(intermediateCount);
        for (var index = 0; index < intermediateCount; index++)
        {
            var position = SendMessage(trackBarHandle, TbmGetTicPos, new IntPtr(index), IntPtr.Zero).ToInt64();
            if (position >= 0 && position <= int.MaxValue)
            {
                positions.Add((int)position);
            }
        }

        return positions.ToArray();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int message,
        IntPtr wParam,
        ref BootstrapRangeNativeRectangle lParam);
}

[StructLayout(LayoutKind.Sequential)]
internal struct BootstrapRangeNativeNotifyHeader
{
    internal IntPtr WindowFrom;
    internal UIntPtr IdFrom;
    internal int Code;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BootstrapRangeNativeRectangle
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
}

[StructLayout(LayoutKind.Sequential)]
internal struct BootstrapRangeNativeCustomDraw
{
    internal BootstrapRangeNativeNotifyHeader Header;
    internal uint DrawStage;
    internal IntPtr DeviceContext;
    internal BootstrapRangeNativeRectangle Rectangle;
    internal UIntPtr ItemSpec;
    internal uint ItemState;
    internal IntPtr ItemParameter;

    internal readonly Rectangle Bounds => Rectangle.ToRectangle();
}
