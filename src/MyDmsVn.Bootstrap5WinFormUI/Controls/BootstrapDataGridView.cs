using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired themed <see cref="DataGridView"/> while retaining native WinForms grid behavior.
/// </summary>
public class BootstrapDataGridView : DataGridView
{
    private string _emptyStateText = "No data to display.";
    private string _loadingText = "Loading...";
    private bool _loading;

    /// <summary>
    /// Initializes a designer-safe Bootstrap-inspired data grid.
    /// </summary>
    public BootstrapDataGridView()
    {
    }

    /// <summary>
    /// Gets or sets the message shown when the grid has no data rows.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the message shown when the grid contains no data rows.")]
    [DefaultValue("No data to display.")]
    public string EmptyStateText
    {
        get => _emptyStateText;
        set
        {
            var normalized = value ?? string.Empty;
            if (_emptyStateText == normalized)
            {
                return;
            }

            _emptyStateText = normalized;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the loading overlay is visible.
    /// </summary>
    [Category("Behavior")]
    [Description("Shows the framework loading overlay without changing the grid data source or enabled state.")]
    [DefaultValue(false)]
    public bool Loading
    {
        get => _loading;
        set
        {
            if (_loading == value)
            {
                return;
            }

            _loading = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the text shown beside the loading spinner.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the text shown beside the loading spinner.")]
    [DefaultValue("Loading...")]
    public string LoadingText
    {
        get => _loadingText;
        set
        {
            var normalized = value ?? string.Empty;
            if (_loadingText == normalized)
            {
                return;
            }

            _loadingText = normalized;
            Invalidate();
        }
    }
}
