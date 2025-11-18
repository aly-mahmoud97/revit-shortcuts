using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitShortcuts.Services
{
    /// <summary>
    /// Represents the severity level of a QA check issue
    /// </summary>
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Represents a single QA check result
    /// </summary>
    public class QACheckResult
    {
        public string CheckName { get; set; }
        public string Category { get; set; }
        public IssueSeverity Severity { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
        public int IssueCount { get; set; }
        public List<ElementId> AffectedElements { get; set; }
        public string Details { get; set; }

        public QACheckResult()
        {
            AffectedElements = new List<ElementId>();
        }
    }

    /// <summary>
    /// Groups QA check results by category
    /// </summary>
    public class QACheckCategory
    {
        public string CategoryName { get; set; }
        public List<QACheckResult> Results { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int TotalIssues { get; set; }

        public QACheckCategory(string categoryName)
        {
            CategoryName = categoryName;
            Results = new List<QACheckResult>();
        }
    }
}
