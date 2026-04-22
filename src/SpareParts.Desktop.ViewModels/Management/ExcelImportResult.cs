using System;
using System.Collections.Generic;
using System.Linq;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportResult
    {
        public string TargetName { get; init; } = "selected table";
        public int ImportedCount { get; private set; }
        public List<string> Errors { get; } = new();

        public bool HasImportedRows => ImportedCount > 0;
        public bool HasErrors => Errors.Count > 0;
        public int FailedCount => Errors.Count;

        public string SummaryMessage =>
            !HasErrors
                ? $"✓ Imported {ImportedCount} row(s) into {TargetName}."
                : ImportedCount == 0
                    ? $"✗ Import into {TargetName} failed. {FailedCount} issue(s) found."
                    : $"⚠ Imported {ImportedCount} row(s) into {TargetName} with {FailedCount} issue(s).";

        public void AddImported()
        {
            ImportedCount++;
        }

        public void AddError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Errors.Add(message.Trim());
        }

        public string ToDialogMessage(int maxErrors = 10)
        {
            if (!HasErrors)
            {
                return SummaryMessage;
            }

            var visibleErrors = Errors
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .Take(maxErrors)
                .ToList();

            var details = string.Join(Environment.NewLine, visibleErrors);
            var hiddenCount = Errors.Count - visibleErrors.Count;
            if (hiddenCount > 0)
            {
                details += $"{Environment.NewLine}+ {hiddenCount} more issue(s).";
            }

            return $"{SummaryMessage}{Environment.NewLine}{Environment.NewLine}{details}";
        }
    }
}
