using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitShortcuts.Services
{
    /// <summary>
    /// Service class that performs various QA checks on a Revit document
    /// </summary>
    public partial class QACheckService
    {
        private readonly Document _doc;

        public QACheckService(Document doc)
        {
            _doc = doc;
        }

        /// <summary>
        /// Runs all QA checks and returns a comprehensive model health summary
        /// </summary>
        public ModelHealthSummary RunAllChecksWithSummary()
        {
            var summary = new ModelHealthSummary();
            summary.ProjectName = _doc.Title;
            summary.FilePath = _doc.PathName;
            summary.GeneratedBy = _doc.Application.Username;
            summary.Categories = RunAllChecks();

            // Calculate file size
            if (!string.IsNullOrEmpty(_doc.PathName) && System.IO.File.Exists(_doc.PathName))
            {
                var fileInfo = new System.IO.FileInfo(_doc.PathName);
                summary.FileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
            }

            // Gather statistics
            summary.TotalElements = new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .ToElementIds()
                .Count;

            summary.TotalViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Count(v => !v.IsTemplate);

            summary.TotalSheets = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .ToElementIds()
                .Count;

            summary.TotalFamilies = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Select(fi => fi.Symbol.Family.Name)
                .Distinct()
                .Count();

            summary.TotalWarnings = _doc.GetWarnings().Count;

            // Calculate check statistics
            summary.TotalChecks = summary.Categories.Sum(c => c.Results.Count);
            summary.PassedChecks = summary.Categories.Sum(c => c.PassedCount);
            summary.FailedChecks = summary.Categories.Sum(c => c.FailedCount);
            summary.TotalIssues = summary.Categories.Sum(c => c.TotalIssues);

            // Count by severity
            foreach (var category in summary.Categories)
            {
                foreach (var result in category.Results)
                {
                    if (!result.Passed)
                    {
                        switch (result.Severity)
                        {
                            case IssueSeverity.Critical:
                                summary.CriticalIssues++;
                                break;
                            case IssueSeverity.Error:
                                summary.ErrorIssues++;
                                break;
                            case IssueSeverity.Warning:
                                summary.WarningIssues++;
                                break;
                            case IssueSeverity.Info:
                                summary.InfoIssues++;
                                break;
                        }
                    }
                }
            }

            summary.CalculateQualityScore();
            return summary;
        }

        /// <summary>
        /// Runs all QA checks and returns categorized results
        /// </summary>
        public List<QACheckCategory> RunAllChecks()
        {
            var categories = new List<QACheckCategory>();

            // Model Integrity Checks
            var modelIntegrity = new QACheckCategory("Model Integrity");
            modelIntegrity.Results.Add(CheckForWarnings());
            modelIntegrity.Results.Add(CheckForErrors());
            modelIntegrity.Results.Add(CheckWarningsBySeverity());
            modelIntegrity.Results.Add(CheckForUnplacedRooms());
            modelIntegrity.Results.Add(CheckForUnenclosedRooms());
            UpdateCategoryStats(modelIntegrity);
            categories.Add(modelIntegrity);

            // Geometry Checks
            var geometry = new QACheckCategory("Geometry");
            geometry.Results.Add(CheckForOverlappingWalls());
            geometry.Results.Add(CheckForOverlappingRooms());
            geometry.Results.Add(CheckShortWalls());
            UpdateCategoryStats(geometry);
            categories.Add(geometry);

            // Families
            var families = new QACheckCategory("Families");
            families.Results.Add(CheckInPlaceFamilies());
            families.Results.Add(CheckOvercomplicatedFamilies());
            families.Results.Add(CheckUnusedFamilies());
            UpdateCategoryStats(families);
            categories.Add(families);

            // Resources (Materials, Groups, CAD)
            var resources = new QACheckCategory("Resources");
            resources.Results.Add(CheckUnusedMaterials());
            resources.Results.Add(CheckUnusedGroups());
            resources.Results.Add(CheckUnusedCADLinks());
            UpdateCategoryStats(resources);
            categories.Add(resources);

            // Views & Sheets
            var viewsSheets = new QACheckCategory("Views & Sheets");
            viewsSheets.Results.Add(CheckTotalAndUnusedViews());
            viewsSheets.Results.Add(CheckViewsNotOnSheets());
            viewsSheets.Results.Add(CheckUnnamedViews());
            viewsSheets.Results.Add(CheckUnnamedSheets());
            viewsSheets.Results.Add(CheckDuplicateSheetNumbers());
            viewsSheets.Results.Add(CheckMissingSheetNumbers());
            viewsSheets.Results.Add(CheckTitleblockParameters());
            viewsSheets.Results.Add(CheckViewGraphicsConsistency());
            UpdateCategoryStats(viewsSheets);
            categories.Add(viewsSheets);

            // Naming Standards
            var namingStandards = new QACheckCategory("Naming Standards");
            namingStandards.Results.Add(CheckFamilyNamingStandards());
            namingStandards.Results.Add(CheckViewNamingStandards());
            namingStandards.Results.Add(CheckNamingConventions());
            UpdateCategoryStats(namingStandards);
            categories.Add(namingStandards);

            // Parameters
            var parameters = new QACheckCategory("Parameters");
            parameters.Results.Add(CheckMissingParameters());
            parameters.Results.Add(CheckTypeVsInstanceParameters());
            UpdateCategoryStats(parameters);
            categories.Add(parameters);

            // Rooms & Spaces
            var roomsSpaces = new QACheckCategory("Rooms & Spaces");
            roomsSpaces.Results.Add(CheckDuplicateRooms());
            UpdateCategoryStats(roomsSpaces);
            categories.Add(roomsSpaces);

            // Coordination
            var coordination = new QACheckCategory("Coordination");
            coordination.Results.Add(CheckCoordinateSystem());
            coordination.Results.Add(CheckLinkAlignment());
            UpdateCategoryStats(coordination);
            categories.Add(coordination);

            // Schedules
            var schedules = new QACheckCategory("Schedules");
            schedules.Results.Add(CheckScheduleHealth());
            UpdateCategoryStats(schedules);
            categories.Add(schedules);

            // Annotations
            var annotations = new QACheckCategory("Annotations");
            annotations.Results.Add(CheckAnnotationIssues());
            UpdateCategoryStats(annotations);
            categories.Add(annotations);

            // Performance
            var performance = new QACheckCategory("Performance");
            performance.Results.Add(CheckLargeFileSize());
            performance.Results.Add(CheckModelPerformanceMetrics());
            performance.Results.Add(CheckLinkedFiles());
            performance.Results.Add(CheckImportedCAD());
            performance.Results.Add(CheckViewTemplates());
            UpdateCategoryStats(performance);
            categories.Add(performance);

            // Worksets (if applicable)
            if (_doc.IsWorkshared)
            {
                var worksets = new QACheckCategory("Worksets");
                worksets.Results.Add(CheckWorksetOrganization());
                worksets.Results.Add(CheckWorksetMisassignments());
                UpdateCategoryStats(worksets);
                categories.Add(worksets);
            }

            return categories;
        }

        private void UpdateCategoryStats(QACheckCategory category)
        {
            category.PassedCount = category.Results.Count(r => r.Passed);
            category.FailedCount = category.Results.Count(r => !r.Passed);
            category.TotalIssues = category.Results.Sum(r => r.IssueCount);
        }

        #region Model Integrity Checks

        private QACheckResult CheckForWarnings()
        {
            var result = new QACheckResult
            {
                CheckName = "Model Warnings",
                Category = "Model Integrity",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var warnings = _doc.GetWarnings();
                result.IssueCount = warnings.Count;
                result.Passed = warnings.Count == 0;
                result.Message = result.Passed
                    ? "No warnings found"
                    : $"{warnings.Count} warning(s) found in the model";
                result.Details = result.Passed
                    ? ""
                    : string.Join("\n", warnings.Take(10).Select(w => w.GetDescriptionText()));
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking warnings: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckForErrors()
        {
            var result = new QACheckResult
            {
                CheckName = "Model Errors",
                Category = "Model Integrity",
                Severity = IssueSeverity.Critical
            };

            try
            {
                var warnings = _doc.GetWarnings();
                var errors = warnings.Where(w => w.GetSeverity() == FailureSeverity.Error).ToList();
                result.IssueCount = errors.Count;
                result.Passed = errors.Count == 0;
                result.Message = result.Passed
                    ? "No errors found"
                    : $"{errors.Count} error(s) found in the model";
                result.Details = result.Passed
                    ? ""
                    : string.Join("\n", errors.Take(10).Select(w => w.GetDescriptionText()));
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking for errors: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckForUnplacedRooms()
        {
            var result = new QACheckResult
            {
                CheckName = "Unplaced Rooms",
                Category = "Model Integrity",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var rooms = new FilteredElementCollector(_doc)
                    .OfClass(typeof(SpatialElement))
                    .OfType<Room>()
                    .Where(r => r.Area == 0 || r.Location == null)
                    .ToList();

                result.IssueCount = rooms.Count;
                result.Passed = rooms.Count == 0;
                result.Message = result.Passed
                    ? "All rooms are placed"
                    : $"{rooms.Count} unplaced room(s) found";
                result.AffectedElements = rooms.Select(r => r.Id).ToList();
                result.Details = result.Passed
                    ? ""
                    : string.Join("\n", rooms.Take(10).Select(r => $"Room: {r.Name} (ID: {r.Id.IntegerValue})"));
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unplaced rooms: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckForUnenclosedRooms()
        {
            var result = new QACheckResult
            {
                CheckName = "Unenclosed Rooms",
                Category = "Model Integrity",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var rooms = new FilteredElementCollector(_doc)
                    .OfClass(typeof(SpatialElement))
                    .OfType<Room>()
                    .Where(r => r.Area > 0 && !r.IsAreaSchemeValid)
                    .ToList();

                result.IssueCount = rooms.Count;
                result.Passed = rooms.Count == 0;
                result.Message = result.Passed
                    ? "All rooms are properly enclosed"
                    : $"{rooms.Count} unenclosed room(s) found";
                result.AffectedElements = rooms.Select(r => r.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unenclosed rooms: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Geometry Checks

        private QACheckResult CheckForOverlappingWalls()
        {
            var result = new QACheckResult
            {
                CheckName = "Overlapping Walls",
                Category = "Geometry",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var walls = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .ToList();

                var overlappingWalls = new List<ElementId>();

                // Simplified check - in production, you'd want more sophisticated geometry comparison
                for (int i = 0; i < walls.Count; i++)
                {
                    for (int j = i + 1; j < walls.Count; j++)
                    {
                        if (AreWallsOverlapping(walls[i], walls[j]))
                        {
                            if (!overlappingWalls.Contains(walls[i].Id))
                                overlappingWalls.Add(walls[i].Id);
                            if (!overlappingWalls.Contains(walls[j].Id))
                                overlappingWalls.Add(walls[j].Id);
                        }
                    }
                }

                result.IssueCount = overlappingWalls.Count;
                result.Passed = overlappingWalls.Count == 0;
                result.Message = result.Passed
                    ? "No overlapping walls detected"
                    : $"{overlappingWalls.Count} potentially overlapping wall(s) found";
                result.AffectedElements = overlappingWalls;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking overlapping walls: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private bool AreWallsOverlapping(Wall wall1, Wall wall2)
        {
            try
            {
                var curve1 = (wall1.Location as LocationCurve)?.Curve;
                var curve2 = (wall2.Location as LocationCurve)?.Curve;

                if (curve1 == null || curve2 == null) return false;

                // Check if the curves are very close to each other
                var result = curve1.Intersect(curve2);
                return result == SetComparisonResult.Overlap || result == SetComparisonResult.Equal;
            }
            catch
            {
                return false;
            }
        }

        private QACheckResult CheckForOverlappingRooms()
        {
            var result = new QACheckResult
            {
                CheckName = "Overlapping Rooms",
                Category = "Geometry",
                Severity = IssueSeverity.Error
            };

            try
            {
                var rooms = new FilteredElementCollector(_doc)
                    .OfClass(typeof(SpatialElement))
                    .OfType<Room>()
                    .Where(r => r.Area > 0)
                    .ToList();

                var overlappingRooms = new List<ElementId>();

                // Check room overlap using bounding boxes
                for (int i = 0; i < rooms.Count; i++)
                {
                    for (int j = i + 1; j < rooms.Count; j++)
                    {
                        var bb1 = rooms[i].get_BoundingBox(null);
                        var bb2 = rooms[j].get_BoundingBox(null);

                        if (bb1 != null && bb2 != null && BoundingBoxesOverlap(bb1, bb2))
                        {
                            if (!overlappingRooms.Contains(rooms[i].Id))
                                overlappingRooms.Add(rooms[i].Id);
                            if (!overlappingRooms.Contains(rooms[j].Id))
                                overlappingRooms.Add(rooms[j].Id);
                        }
                    }
                }

                result.IssueCount = overlappingRooms.Count;
                result.Passed = overlappingRooms.Count == 0;
                result.Message = result.Passed
                    ? "No overlapping rooms detected"
                    : $"{overlappingRooms.Count} potentially overlapping room(s) found";
                result.AffectedElements = overlappingRooms;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking overlapping rooms: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private bool BoundingBoxesOverlap(BoundingBoxXYZ bb1, BoundingBoxXYZ bb2)
        {
            return !(bb1.Max.X < bb2.Min.X || bb2.Max.X < bb1.Min.X ||
                     bb1.Max.Y < bb2.Min.Y || bb2.Max.Y < bb1.Min.Y ||
                     bb1.Max.Z < bb2.Min.Z || bb2.Max.Z < bb1.Min.Z);
        }

        private QACheckResult CheckShortWalls()
        {
            var result = new QACheckResult
            {
                CheckName = "Short Walls (< 1 ft)",
                Category = "Geometry",
                Severity = IssueSeverity.Info
            };

            try
            {
                var shortWalls = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .Where(w =>
                    {
                        var curve = (w.Location as LocationCurve)?.Curve;
                        return curve != null && curve.Length < 1.0; // Less than 1 foot
                    })
                    .ToList();

                result.IssueCount = shortWalls.Count;
                result.Passed = shortWalls.Count == 0;
                result.Message = result.Passed
                    ? "No unusually short walls found"
                    : $"{shortWalls.Count} short wall(s) found (< 1 ft)";
                result.AffectedElements = shortWalls.Select(w => w.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking short walls: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Element Properties

        private QACheckResult CheckUnnamedViews()
        {
            var result = new QACheckResult
            {
                CheckName = "Unnamed Views",
                Category = "Element Properties",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var views = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && v.Name.Contains("Copy"))
                    .ToList();

                result.IssueCount = views.Count;
                result.Passed = views.Count == 0;
                result.Message = result.Passed
                    ? "All views are properly named"
                    : $"{views.Count} view(s) with default names (containing 'Copy')";
                result.AffectedElements = views.Select(v => v.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unnamed views: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckUnnamedSheets()
        {
            var result = new QACheckResult
            {
                CheckName = "Unnamed Sheets",
                Category = "Element Properties",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s => s.Name.Contains("Sheet") && s.SheetNumber == "---")
                    .ToList();

                result.IssueCount = sheets.Count;
                result.Passed = sheets.Count == 0;
                result.Message = result.Passed
                    ? "All sheets are properly numbered"
                    : $"{sheets.Count} sheet(s) with default numbers";
                result.AffectedElements = sheets.Select(s => s.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unnamed sheets: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckMissingParameters()
        {
            var result = new QACheckResult
            {
                CheckName = "Missing Required Parameters",
                Category = "Element Properties",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s =>
                    {
                        var drawnBy = s.LookupParameter("Drawn By");
                        var checkedBy = s.LookupParameter("Checked By");
                        return (drawnBy != null && string.IsNullOrEmpty(drawnBy.AsString())) ||
                               (checkedBy != null && string.IsNullOrEmpty(checkedBy.AsString()));
                    })
                    .ToList();

                result.IssueCount = sheets.Count;
                result.Passed = sheets.Count == 0;
                result.Message = result.Passed
                    ? "All required parameters are filled"
                    : $"{sheets.Count} sheet(s) with missing parameters";
                result.AffectedElements = sheets.Select(s => s.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking missing parameters: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Standards & Naming

        private QACheckResult CheckNamingConventions()
        {
            var result = new QACheckResult
            {
                CheckName = "Naming Conventions",
                Category = "Standards & Naming",
                Severity = IssueSeverity.Info
            };

            try
            {
                var families = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .GroupBy(f => f.Symbol.Family.Name)
                    .Count();

                result.Passed = true;
                result.Message = $"{families} unique families in the model";
                result.Details = "Review family naming for consistency with project standards";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking naming conventions: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckViewTemplates()
        {
            var result = new QACheckResult
            {
                CheckName = "Views Without Templates",
                Category = "Standards & Naming",
                Severity = IssueSeverity.Info
            };

            try
            {
                var viewsWithoutTemplates = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && v.ViewTemplateId == ElementId.InvalidElementId
                           && v.ViewType != ViewType.Schedule)
                    .ToList();

                result.IssueCount = viewsWithoutTemplates.Count;
                result.Passed = viewsWithoutTemplates.Count == 0;
                result.Message = result.Passed
                    ? "All views use templates"
                    : $"{viewsWithoutTemplates.Count} view(s) without templates";
                result.AffectedElements = viewsWithoutTemplates.Select(v => v.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking view templates: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Performance

        private QACheckResult CheckLargeFileSize()
        {
            var result = new QACheckResult
            {
                CheckName = "File Size Check",
                Category = "Performance",
                Severity = IssueSeverity.Info
            };

            try
            {
                var filePath = _doc.PathName;
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    var fileInfo = new System.IO.FileInfo(filePath);
                    var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

                    result.Passed = fileSizeMB < 500; // Warning if over 500 MB
                    result.Message = $"File size: {fileSizeMB:F2} MB";
                    result.Details = fileSizeMB > 500
                        ? "Large file size may impact performance. Consider purging unused elements."
                        : "File size is acceptable";
                }
                else
                {
                    result.Passed = true;
                    result.Message = "File not saved - size check skipped";
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking file size: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckLinkedFiles()
        {
            var result = new QACheckResult
            {
                CheckName = "Linked Revit Files",
                Category = "Performance",
                Severity = IssueSeverity.Info
            };

            try
            {
                var linkedFiles = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();

                result.IssueCount = linkedFiles.Count;
                result.Passed = true;
                result.Message = $"{linkedFiles.Count} linked Revit file(s)";
                result.AffectedElements = linkedFiles.Select(l => l.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking linked files: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckImportedCAD()
        {
            var result = new QACheckResult
            {
                CheckName = "Imported CAD Files",
                Category = "Performance",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var importedCAD = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ImportInstance))
                    .Cast<ImportInstance>()
                    .ToList();

                result.IssueCount = importedCAD.Count;
                result.Passed = importedCAD.Count == 0;
                result.Message = result.Passed
                    ? "No imported CAD files found"
                    : $"{importedCAD.Count} imported CAD file(s) - consider linking instead";
                result.AffectedElements = importedCAD.Select(i => i.Id).ToList();
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking imported CAD: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Worksets

        private QACheckResult CheckWorksetOrganization()
        {
            var result = new QACheckResult
            {
                CheckName = "Workset Organization",
                Category = "Worksets",
                Severity = IssueSeverity.Info
            };

            try
            {
                var worksets = new FilteredWorksetCollector(_doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .ToList();

                result.Passed = true;
                result.Message = $"{worksets.Count} user workset(s) in the model";
                result.Details = string.Join(", ", worksets.Take(10).Select(w => w.Name));
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking worksets: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion
    }
}
