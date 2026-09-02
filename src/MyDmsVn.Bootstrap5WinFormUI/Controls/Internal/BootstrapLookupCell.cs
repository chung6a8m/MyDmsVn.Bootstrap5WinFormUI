using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(BootstrapLookupEditingControl);
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
        TypeConverter formattedValueTypeConverter, TypeConverter valueTypeConverter)
    {
        if (formattedValue is null && !TargetAllowsNull())
            throw new FormatException("A null lookup value cannot be assigned to the non-nullable bound property.");
        return formattedValue!;
    }

    private bool TargetAllowsNull()
    {
        var targetType = OwningColumn?.ValueType;
        var boundItem = OwningRow?.DataBoundItem;
        var propertyName = OwningColumn?.DataPropertyName;
        if (boundItem is not null && !string.IsNullOrEmpty(propertyName))
            targetType = TypeDescriptor.GetProperties(boundItem)[propertyName!]?.PropertyType ?? targetType;
        return targetType is null || !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
    }
}
