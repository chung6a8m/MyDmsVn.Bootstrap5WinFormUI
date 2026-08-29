using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapSelect
{
    /// <inheritdoc />
    protected override AccessibleObject CreateAccessibilityInstance()
    {
        return new BootstrapSelectAccessibleObject(this);
    }

    private sealed class BootstrapSelectAccessibleObject : ControlAccessibleObject
    {
        private readonly BootstrapSelect _owner;

        internal BootstrapSelectAccessibleObject(BootstrapSelect owner)
            : base(owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public override AccessibleRole Role => AccessibleRole.ComboBox;

        public override string? Value
        {
            get
            {
                if (_owner.SelectionMode == BootstrapSelectMode.Multiple)
                {
                    return _owner.SelectedItems.Count + " selected";
                }

                return _owner.SelectedItem?.Text ?? string.Empty;
            }
        }

        public override AccessibleStates State
        {
            get
            {
                var state = base.State | AccessibleStates.Focusable;
                state &= ~(AccessibleStates.Expanded | AccessibleStates.Collapsed);
                state |= _owner.IsDropDownOpenForTest ? AccessibleStates.Expanded : AccessibleStates.Collapsed;
                return state;
            }
        }

        public override string? DefaultAction => _owner.IsDropDownOpenForTest ? "Collapse" : "Expand";

        public override void DoDefaultAction()
        {
            if (!_owner.Enabled)
            {
                return;
            }

            if (_owner.IsDropDownOpenForTest)
            {
                _owner.CloseDropDownInternal(true);
            }
            else
            {
                _owner.OpenDropDownInternal();
            }
        }
    }
}
