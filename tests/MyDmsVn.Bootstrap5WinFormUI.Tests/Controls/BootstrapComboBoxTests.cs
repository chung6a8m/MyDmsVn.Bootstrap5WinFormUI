using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapComboBoxTests
{
    [Test]
    public void NativeComboBoxCharacterizationPreservesUnboundSelectionAndEvents()
    {
        using var comboBox = new ComboBox();
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        var selectedIndexChanged = 0;
        var selectedValueChanged = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectedIndexChanged++;
        comboBox.SelectedValueChanged += (_, _) => selectedValueChanged++;

        comboBox.SelectedIndex = 1;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.EqualTo("Beta"));
            Assert.That(comboBox.Text, Is.EqualTo("Beta"));
            Assert.That(selectedIndexChanged, Is.EqualTo(1));
            Assert.That(selectedValueChanged, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeComboBoxCharacterizationPreservesBindingAndEditableConfiguration()
    {
        var source = new List<LookupItem>
        {
            new LookupItem(10, "Ten"),
            new LookupItem(20, "Twenty")
        };
        using var form = new Form { ShowInTaskbar = false };
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DisplayMember = nameof(LookupItem.Name),
            ValueMember = nameof(LookupItem.Id),
            DataSource = source
        };
        form.Controls.Add(comboBox);
        form.Show();
        Application.DoEvents();

        comboBox.SelectedValue = 20;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DataSource, Is.SameAs(source));
            Assert.That(comboBox.DisplayMember, Is.EqualTo(nameof(LookupItem.Name)));
            Assert.That(comboBox.ValueMember, Is.EqualTo(nameof(LookupItem.Id)));
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.SameAs(source[1]));
            Assert.That(comboBox.SelectedValue, Is.EqualTo(20));
            Assert.That(comboBox.GetItemText(source[1]), Is.EqualTo("Twenty"));
            Assert.That(comboBox.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
            Assert.That(comboBox.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.SuggestAppend));
            Assert.That(comboBox.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.ListItems));
        }));
    }

    private sealed class LookupItem
    {
        public LookupItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
