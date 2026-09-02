using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Defines one text-backed column in a lookup result grid.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapLookupColumnDefinition
{
    private int _width = 100;
    private int _minimumWidth = 5;
    private string _dataPropertyName = string.Empty;
    private string _headerText = string.Empty;
    private string _format = string.Empty;

    /// <summary>Gets or sets the source member displayed by the column.</summary>
    [DefaultValue("")]
    public string DataPropertyName { get => _dataPropertyName; set => _dataPropertyName = value ?? string.Empty; }

    /// <summary>Gets or sets the result-column header text.</summary>
    [DefaultValue("")]
    public string HeaderText { get => _headerText; set => _headerText = value ?? string.Empty; }

    /// <summary>Gets or sets the logical column width.</summary>
    [DefaultValue(100)]
    public int Width
    {
        get => _width;
        set
        {
            if (value < _minimumWidth) throw new ArgumentOutOfRangeException(nameof(value));
            _width = value;
        }
    }

    /// <summary>Gets or sets the minimum logical column width.</summary>
    [DefaultValue(5)]
    public int MinimumWidth
    {
        get => _minimumWidth;
        set
        {
            if (value < 2 || value > _width) throw new ArgumentOutOfRangeException(nameof(value));
            _minimumWidth = value;
        }
    }

    /// <summary>Gets or sets whether the result column is visible.</summary>
    [DefaultValue(true)]
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets the native DataGridView autosizing mode.</summary>
    [DefaultValue(DataGridViewAutoSizeColumnMode.None)]
    public DataGridViewAutoSizeColumnMode AutoSizeMode { get; set; } = DataGridViewAutoSizeColumnMode.None;

    /// <summary>Gets or sets the result-cell content alignment.</summary>
    [DefaultValue(DataGridViewContentAlignment.MiddleLeft)]
    public DataGridViewContentAlignment Alignment { get; set; } = DataGridViewContentAlignment.MiddleLeft;

    /// <summary>Gets or sets the native DataGridView format string.</summary>
    [DefaultValue("")]
    public string Format { get => _format; set => _format = value ?? string.Empty; }

    /// <summary>Gets or sets the optional native cell value type.</summary>
    [DefaultValue(null)]
    public Type? ValueType { get; set; }

    /// <inheritdoc />
    public override string ToString() => string.IsNullOrEmpty(HeaderText) ? DataPropertyName : HeaderText;
}
