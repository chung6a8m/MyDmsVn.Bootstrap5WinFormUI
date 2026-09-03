using System;
using System.Collections.Generic;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    internal BootstrapLookupCommitResult ResolvePendingText(BootstrapLookupCommitReason reason)
    {
        if (!HasPendingText) return BootstrapLookupCommitResult.Success(false);
        var originalText = Text;
        var normalized = TextNormalizer(originalText) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            CommitSelection(null, null, string.Empty, BootstrapLookupCommitReason.Clear);
            return BootstrapLookupCommitResult.Success();
        }

        var exactMatches = FindExactMatches(normalized);
        if (exactMatches.Count > 0)
        {
            var first = exactMatches[0];
            if (first.Value is null && ValueMember.Length > 0) return ApplyLookupValidation();
            var distinctValues = new List<object?>();
            foreach (var match in exactMatches)
            {
                var exists = false;
                foreach (var value in distinctValues)
                {
                    if (EqualityComparer<object?>.Default.Equals(value, match.Value)) { exists = true; break; }
                }
                if (!exists) distinctValues.Add(match.Value);
            }

            if (distinctValues.Count != 1) return ApplyLookupValidation();
            CommitSelection(first.Item, first.Value, first.DisplayText, BootstrapLookupCommitReason.ExactMatch);
            return BootstrapLookupCommitResult.Success();
        }

        switch (UnmatchedTextBehavior)
        {
            case BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection:
                CancelPendingEdit();
                return BootstrapLookupCommitResult.Success(false);
            case BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError:
                return ApplyLookupValidation();
            case BootstrapLookupUnmatchedTextBehavior.CommitAndAdd:
                return CommitNewItem(originalText, normalized);
            default:
                throw new ArgumentOutOfRangeException(nameof(UnmatchedTextBehavior));
        }
    }

    private List<BootstrapLookupSourceItem> FindExactMatches(string normalizedText)
    {
        var matches = new List<BootstrapLookupSourceItem>();
        if (_dataAdapter is null) return matches;
        foreach (var item in _dataAdapter.Snapshot)
        {
            var candidate = TextNormalizer(item.DisplayText) ?? string.Empty;
            if (TextComparer.Equals(candidate, normalizedText)) matches.Add(item);
        }
        return matches;
    }

    private BootstrapLookupCommitResult CommitNewItem(string originalText, string normalizedText)
    {
        if (_dataAdapter is null || !_dataAdapter.CanAdd) return ApplyLookupValidation();
        var workflowGeneration = BeginApplicationWorkflow();
        if (_activeCreateWorkflowGeneration == workflowGeneration)
        {
            EndApplicationWorkflow();
            return BootstrapLookupCommitResult.Success(false);
        }

        _activeCreateWorkflowGeneration = workflowGeneration;
        try
        {
            object? item = null;
            if (CreateItemFromText is null && IsStringSource())
            {
                item = originalText.Trim();
            }
            else
            {
                var args = new BootstrapLookupCreateItemFromTextEventArgs(originalText, normalizedText);
                RaiseCreateItemFromText(args);
                if (!IsApplicationWorkflowCurrent(workflowGeneration)) return BootstrapLookupCommitResult.Success(false);
                if (args.Cancel) return ApplyLookupValidation();
                item = args.Item;
            }

            if (item is null) return ApplyLookupValidation();
            if (!HasRequiredMembers(item)) return ApplyLookupValidation();
            object? value;
            try
            {
                value = BootstrapLookupMemberAccessor.GetValue(item, ValueMember);
            }
            catch (ArgumentException)
            {
                return ApplyLookupValidation();
            }
            if (value is null && ValueMember.Length > 0) return ApplyLookupValidation();

            var sourceChangeGeneration = _sourceChangeGeneration;
            _dataAdapter.Add(item);
            if (!IsApplicationWorkflowCurrent(workflowGeneration)) return BootstrapLookupCommitResult.Success(false);
            if (!_dataAdapter.TryFindByItem(item, out var accepted) || accepted is null)
                return ApplyLookupValidation();
            if (accepted.Value is null && ValueMember.Length > 0) return ApplyLookupValidation();
            CommitSelection(accepted.Item, accepted.Value, accepted.DisplayText, BootstrapLookupCommitReason.CommitAndAdd);
            if (!IsApplicationWorkflowCurrent(workflowGeneration)) return BootstrapLookupCommitResult.Success(false);
            if (sourceChangeGeneration == _sourceChangeGeneration) ExecuteSearchNow();
            return BootstrapLookupCommitResult.Success();
        }
        finally
        {
            if (_activeCreateWorkflowGeneration == workflowGeneration) _activeCreateWorkflowGeneration = -1;
            EndApplicationWorkflow();
        }
    }

    private bool HasRequiredMembers(object item)
    {
        try
        {
            BootstrapLookupMemberAccessor.Validate(item, DisplayMember);
            BootstrapLookupMemberAccessor.Validate(item, ValueMember);
            foreach (var member in SearchMembers)
                BootstrapLookupMemberAccessor.Validate(item, member);
            foreach (var column in Columns)
                BootstrapLookupMemberAccessor.Validate(item, column.DataPropertyName);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private bool IsStringSource()
    {
        if (_dataAdapter is null) return false;
        foreach (var item in _dataAdapter.Snapshot) return item.Item is string;
        return _dataAdapter.IsStringItemSource;
    }

    private BootstrapLookupCommitResult ApplyLookupValidation()
    {
        SetLookupValidation(InvalidTextMessage);
        Editor.Focus();
        return BootstrapLookupCommitResult.Blocked();
    }
}
