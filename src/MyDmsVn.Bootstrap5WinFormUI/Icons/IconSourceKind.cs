namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Identifies the source represented by an <see cref="IconDescriptor"/>.
/// </summary>
public enum IconSourceKind
{
    /// <summary>
    /// A glyph from the Windows Segoe MDL2 Assets font.
    /// </summary>
    SegoeMdl2 = 0,

    /// <summary>
    /// SVG markup rendered by an application-supplied SVG adapter.
    /// </summary>
    Svg = 1,

    /// <summary>
    /// A framework-owned vector glyph.
    /// </summary>
    FrameworkVector = 2,

    /// <summary>
    /// An icon owned by an optional or application-defined provider.
    /// </summary>
    External = 3
}
