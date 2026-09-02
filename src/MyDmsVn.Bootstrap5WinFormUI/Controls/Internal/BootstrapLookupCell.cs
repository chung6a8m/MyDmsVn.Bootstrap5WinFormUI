using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(BootstrapLookupEditingControl);
    public override Type ValueType => typeof(object);
    public override object? DefaultNewRowValue => null;

    public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
        if (DataGridView?.EditingControl is BootstrapLookupEditingControl editor && OwningColumn is BootstrapLookupColumn column)
            editor.Configure(column, rowIndex, ColumnIndex, Value);
    }

    protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
        TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
    {
        return OwningColumn is BootstrapLookupColumn column ? column.ResolveDisplayText(value) : base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);
    }

    public override object ParseFormattedValue(object formattedValue, DataGridViewCellStyle cellStyle,
        TypeConverter formattedValueTypeConverter, TypeConverter valueTypeConverter) => formattedValue;
}
