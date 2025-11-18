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

        // Enhanced properties for comprehensive reporting
        public string Location { get; set; }
        public string RecommendedFix { get; set; }
        public List<string> IssueLocations { get; set; }
        public Dictionary<string, object> Metrics { get; set; }

        public QACheckResult()
        {
            AffectedElements = new List<ElementId>();
            IssueLocations = new List<string>();
            Metrics = new Dictionary<string, object>();
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

    /// <summary>
    /// Represents the overall model health summary
    /// </summary>
    public class ModelHealthSummary
    {
        public string ProjectName { get; set; }
        public string FilePath { get; set; }
        public double FileSizeMB { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }

        // Quality Score (0-100)
        public double OverallQualityScore { get; set; }
        public string QualityGrade { get; set; } // A, B, C, D, F

        // Summary statistics
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int ErrorIssues { get; set; }
        public int WarningIssues { get; set; }
        public int InfoIssues { get; set; }

        // Performance metrics
        public int TotalElements { get; set; }
        public int TotalViews { get; set; }
        public int TotalSheets { get; set; }
        public int TotalFamilies { get; set; }
        public int TotalWarnings { get; set; }

        // Categories
        public List<QACheckCategory> Categories { get; set; }

        public ModelHealthSummary()
        {
            Categories = new List<QACheckCategory>();
            GeneratedDate = DateTime.Now;
        }

        /// <summary>
        /// Calculate quality score based on check results
        /// </summary>
        public void CalculateQualityScore()
        {
            if (TotalChecks == 0)
            {
                OverallQualityScore = 0;
                QualityGrade = "F";
                return;
            }

            // Base score from pass rate
            double passRate = (double)PassedChecks / TotalChecks;
            double baseScore = passRate * 70; // 70% of score from pass rate

            // Deduct points for issues by severity
            double severityPenalty = 0;
            severityPenalty += CriticalIssues * 3;
            severityPenalty += ErrorIssues * 2;
            severityPenalty += WarningIssues * 1;
            severityPenalty += InfoIssues * 0.1;

            // Scale penalty (max 30 points deduction)
            severityPenalty = Math.Min(severityPenalty, 30);

            OverallQualityScore = Math.Max(0, baseScore + (30 - severityPenalty));

            // Assign grade
            if (OverallQualityScore >= 90) QualityGrade = "A";
            else if (OverallQualityScore >= 80) QualityGrade = "B";
            else if (OverallQualityScore >= 70) QualityGrade = "C";
            else if (OverallQualityScore >= 60) QualityGrade = "D";
            else QualityGrade = "F";
        }
    }
}
