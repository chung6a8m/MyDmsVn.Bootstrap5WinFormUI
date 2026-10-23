using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapDecimalCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(BootstrapDecimalEditingControl);
    public override object? DefaultNewRowValue => null;

    public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
        if (DataGridView?.EditingControl is BootstrapDecimalEditingControl editor && OwningColumn is BootstrapDecimalColumn column)
            editor.Configure(column, this, rowIndex, Value);
    }

    public override void DetachEditingControl()
    {
        if (DataGridView?.EditingControl is BootstrapDecimalEditingControl editor && ReferenceEquals(editor.EditingCell, this))
            editor.ProjectCurrentModel();
        base.DetachEditingControl();
    }

    protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
        TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
    {
        if (OwningColumn is BootstrapDecimalColumn column)
        {
            if (value is null || value is DBNull) return string.Empty;
            if (value is decimal amount)
            {
                var candidate = amount.ToString("0.############################", column.Profile.Culture);
                return DecimalInputProjection.Project(candidate, column.Profile).DisplayText;
            }
        }
        return base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);
    }

    public override object ParseFormattedValue(object formattedValue, DataGridViewCellStyle cellStyle,
        TypeConverter formattedValueTypeConverter, TypeConverter valueTypeConverter)
    {
        if (OwningColumn is not BootstrapDecimalColumn column)
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        var profile = DataGridView?.EditingControl is BootstrapDecimalEditingControl editor && ReferenceEquals(editor.EditingCell, this)
            ? editor.SessionProfile : column.Profile;
        var parsed = DecimalInputParser.TryParsePending(formattedValue?.ToString() ?? string.Empty, profile);
        if (parsed.State != InputParseState.Valid)
            throw new FormatException($"Decimal candidate cannot be committed: {parsed.Failure?.Code}.");
        if (parsed.Value is null && !TargetAllowsNull())
            throw new FormatException("A null decimal value cannot be assigned to the non-nullable bound property.");
        if (parsed.Value is null && OwningRow?.DataBoundItem is DataRowView) return DBNull.Value;
        return parsed.Value!;
    }

    private bool TargetAllowsNull()
    {
        var targetType = OwningColumn?.ValueType;
        var boundItem = OwningRow?.DataBoundItem;
        var propertyName = OwningColumn?.DataPropertyName;
        if (boundItem is DataRowView rowView && !string.IsNullOrEmpty(propertyName))
            return rowView.Row.Table.Columns[propertyName!]?.AllowDBNull ?? false;
        if (boundItem is not null && !string.IsNullOrEmpty(propertyName))
            targetType = TypeDescriptor.GetProperties(boundItem)[propertyName!]?.PropertyType ?? targetType;
        return targetType is null || !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
    }
}
