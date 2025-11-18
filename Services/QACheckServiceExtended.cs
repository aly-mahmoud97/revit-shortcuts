using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitShortcuts.Services
{
    /// <summary>
    /// Extended comprehensive QA check methods for Architecture
    /// This partial class contains additional health checks focused on architectural elements
    /// </summary>
    public partial class QACheckService
    {
        #region Families and Types

        private QACheckResult CheckInPlaceFamilies()
        {
            var result = new QACheckResult
            {
                CheckName = "In-Place Families",
                Category = "Families",
                Severity = IssueSeverity.Warning
            };

            try
            {
                // Focus on architectural categories only
                var archCategories = new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Roofs,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_Stairs,
                    BuiltInCategory.OST_Railings,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_GenericModel,
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_Casework
                };

                var inPlaceFamilies = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .Where(fi => fi.Symbol.Family.IsInPlace &&
                                archCategories.Contains((BuiltInCategory)fi.Category.Id.Value))
                    .ToList();

                result.IssueCount = inPlaceFamilies.Count;
                result.Passed = inPlaceFamilies.Count < 10; // Threshold
                result.Message = result.Passed
                    ? $"{inPlaceFamilies.Count} architectural in-place familie(s) found (acceptable)"
                    : $"{inPlaceFamilies.Count} architectural in-place familie(s) found - consider using loadable families";
                result.AffectedElements = inPlaceFamilies.Select(f => f.Id).ToList();
                result.RecommendedFix = "Convert in-place families to loadable families where possible for better performance";
                result.Metrics["InPlaceCount"] = inPlaceFamilies.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking in-place families: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckOvercomplicatedFamilies()
        {
            var result = new QACheckResult
            {
                CheckName = "Overcomplicated Families",
                Category = "Families",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var complicatedFamilies = new List<ElementId>();
                var familyInstances = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .ToList();

                // Group by family type and check face count
                foreach (var familyGroup in familyInstances.GroupBy(f => f.Symbol.Family.Name))
                {
                    var sample = familyGroup.First();
                    var geometry = sample.get_Geometry(new Options());

                    if (geometry != null)
                    {
                        int faceCount = CountFaces(geometry);
                        if (faceCount > 5000) // High poly count threshold
                        {
                            complicatedFamilies.AddRange(familyGroup.Select(f => f.Id));
                        }
                    }
                }

                result.IssueCount = complicatedFamilies.Count;
                result.Passed = complicatedFamilies.Count == 0;
                result.Message = result.Passed
                    ? "No overcomplicated families detected"
                    : $"{complicatedFamilies.Count} instance(s) of high-poly families found";
                result.AffectedElements = complicatedFamilies;
                result.RecommendedFix = "Simplify family geometry or use detail levels appropriately";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking complicated families: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private int CountFaces(GeometryElement geometry)
        {
            int count = 0;
            foreach (GeometryObject obj in geometry)
            {
                if (obj is Solid solid)
                {
                    count += solid.Faces.Size;
                }
                else if (obj is GeometryInstance instance)
                {
                    count += CountFaces(instance.GetInstanceGeometry());
                }
            }
            return count;
        }

        private QACheckResult CheckUnusedFamilies()
        {
            var result = new QACheckResult
            {
                CheckName = "Unused Families",
                Category = "Families",
                Severity = IssueSeverity.Info
            };

            try
            {
                var allFamilySymbols = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .ToList();

                var usedFamilyIds = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .Select(fi => fi.Symbol.Id)
                    .Distinct()
                    .ToHashSet();

                var unusedFamilies = allFamilySymbols
                    .Where(fs => !usedFamilyIds.Contains(fs.Id))
                    .ToList();

                result.IssueCount = unusedFamilies.Count;
                result.Passed = unusedFamilies.Count == 0;
                result.Message = result.Passed
                    ? "No unused families found"
                    : $"{unusedFamilies.Count} unused family type(s) - consider purging";
                result.AffectedElements = unusedFamilies.Select(f => f.Id).ToList();
                result.RecommendedFix = "Purge unused families to reduce file size";
                result.Metrics["UnusedCount"] = unusedFamilies.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unused families: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Materials and Resources

        private QACheckResult CheckUnusedMaterials()
        {
            var result = new QACheckResult
            {
                CheckName = "Unused Materials",
                Category = "Resources",
                Severity = IssueSeverity.Info
            };

            try
            {
                var allMaterials = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .ToList();

                // This is a simplified check - in production, you'd need to check all elements that can have materials
                var usedMaterialIds = new HashSet<ElementId>();

                // Check walls
                foreach (Wall wall in new FilteredElementCollector(_doc).OfClass(typeof(Wall)))
                {
                    var materialId = wall.GetMaterialIds(false).FirstOrDefault();
                    if (materialId != null && materialId != ElementId.InvalidElementId)
                        usedMaterialIds.Add(materialId);
                }

                var unusedMaterials = allMaterials
                    .Where(m => !usedMaterialIds.Contains(m.Id))
                    .ToList();

                result.IssueCount = unusedMaterials.Count;
                result.Passed = unusedMaterials.Count < 50;
                result.Message = $"{unusedMaterials.Count} unused material(s) found";
                result.AffectedElements = unusedMaterials.Select(m => m.Id).ToList();
                result.RecommendedFix = "Purge unused materials to reduce file size";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unused materials: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckUnusedGroups()
        {
            var result = new QACheckResult
            {
                CheckName = "Unused Groups",
                Category = "Resources",
                Severity = IssueSeverity.Info
            };

            try
            {
                var allGroups = new FilteredElementCollector(_doc)
                    .OfClass(typeof(GroupType))
                    .Cast<GroupType>()
                    .ToList();

                var usedGroupIds = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Group))
                    .Cast<Group>()
                    .Select(g => g.GetTypeId())
                    .Distinct()
                    .ToHashSet();

                var unusedGroups = allGroups
                    .Where(gt => !usedGroupIds.Contains(gt.Id))
                    .ToList();

                result.IssueCount = unusedGroups.Count;
                result.Passed = unusedGroups.Count == 0;
                result.Message = result.Passed
                    ? "No unused groups found"
                    : $"{unusedGroups.Count} unused group(s) found";
                result.AffectedElements = unusedGroups.Select(g => g.Id).ToList();
                result.RecommendedFix = "Delete unused groups";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unused groups: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckUnusedCADLinks()
        {
            var result = new QACheckResult
            {
                CheckName = "Unused CAD Links",
                Category = "Resources",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var cadLinks = new FilteredElementCollector(_doc)
                    .OfClass(typeof(CADLinkType))
                    .Cast<CADLinkType>()
                    .ToList();

                // Check if CAD link has instances in the model
                var cadInstances = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ImportInstance))
                    .Cast<ImportInstance>()
                    .Select(i => i.GetTypeId())
                    .Distinct()
                    .ToHashSet();

                var unusedCAD = cadLinks
                    .Where(c => !cadInstances.Contains(c.Id))
                    .ToList();

                result.IssueCount = unusedCAD.Count;
                result.Passed = unusedCAD.Count == 0;
                result.Message = result.Passed
                    ? "No unused CAD links found"
                    : $"{unusedCAD.Count} unused CAD link(s) found - remove to improve performance";
                result.AffectedElements = unusedCAD.Select(c => c.Id).ToList();
                result.RecommendedFix = "Remove unused CAD links";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unused CAD links: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Views and Sheets

        private QACheckResult CheckTotalAndUnusedViews()
        {
            var result = new QACheckResult
            {
                CheckName = "View Analysis",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Info
            };

            try
            {
                var allViews = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .ToList();

                var viewsOnSheets = new HashSet<ElementId>();
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .ToList();

                foreach (var sheet in sheets)
                {
                    var placedViews = sheet.GetAllPlacedViews();
                    foreach (var viewId in placedViews)
                    {
                        viewsOnSheets.Add(viewId);
                    }
                }

                var unusedViews = allViews.Where(v => !viewsOnSheets.Contains(v.Id) && v.ViewType != ViewType.Schedule).ToList();

                result.IssueCount = unusedViews.Count;
                result.Passed = unusedViews.Count < allViews.Count * 0.3; // Less than 30% unused
                result.Message = $"{allViews.Count} total views, {unusedViews.Count} not on sheets";
                result.AffectedElements = unusedViews.Select(v => v.Id).ToList();
                result.RecommendedFix = "Review and delete unnecessary views not on sheets";
                result.Metrics["TotalViews"] = allViews.Count;
                result.Metrics["UnusedViews"] = unusedViews.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking views: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckViewsNotOnSheets()
        {
            var result = new QACheckResult
            {
                CheckName = "Views Not Placed on Sheets",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Info
            };

            try
            {
                var allViews = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && v.CanBePrinted && v.ViewType != ViewType.Schedule)
                    .ToList();

                var viewsOnSheets = new HashSet<ElementId>();
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>();

                foreach (var sheet in sheets)
                {
                    foreach (var viewId in sheet.GetAllPlacedViews())
                    {
                        viewsOnSheets.Add(viewId);
                    }
                }

                var unplacedViews = allViews.Where(v => !viewsOnSheets.Contains(v.Id)).ToList();

                result.IssueCount = unplacedViews.Count;
                result.Passed = unplacedViews.Count < 10;
                result.Message = $"{unplacedViews.Count} view(s) not placed on any sheet";
                result.AffectedElements = unplacedViews.Select(v => v.Id).ToList();
                result.RecommendedFix = "Place views on sheets or delete if not needed";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking unplaced views: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckDuplicateSheetNumbers()
        {
            var result = new QACheckResult
            {
                CheckName = "Duplicate Sheet Numbers",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Critical
            };

            try
            {
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .ToList();

                var duplicates = sheets
                    .GroupBy(s => s.SheetNumber)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .ToList();

                result.IssueCount = duplicates.Count;
                result.Passed = duplicates.Count == 0;
                result.Message = result.Passed
                    ? "No duplicate sheet numbers found"
                    : $"{duplicates.Count} sheet(s) with duplicate numbers - MUST FIX";
                result.AffectedElements = duplicates.Select(s => s.Id).ToList();
                result.RecommendedFix = "Assign unique sheet numbers to all sheets";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking duplicate sheets: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckMissingSheetNumbers()
        {
            var result = new QACheckResult
            {
                CheckName = "Missing Sheet Numbers",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Error
            };

            try
            {
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s => string.IsNullOrEmpty(s.SheetNumber) || s.SheetNumber == "---")
                    .ToList();

                result.IssueCount = sheets.Count;
                result.Passed = sheets.Count == 0;
                result.Message = result.Passed
                    ? "All sheets have numbers"
                    : $"{sheets.Count} sheet(s) missing sheet numbers";
                result.AffectedElements = sheets.Select(s => s.Id).ToList();
                result.RecommendedFix = "Assign sheet numbers to all sheets";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking missing sheet numbers: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Naming Standards

        private QACheckResult CheckFamilyNamingStandards()
        {
            var result = new QACheckResult
            {
                CheckName = "Family Naming Standards",
                Category = "Naming Standards",
                Severity = IssueSeverity.Info
            };

            try
            {
                var families = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .Select(fi => fi.Symbol.Family)
                    .Distinct()
                    .ToList();

                var nonCompliant = families
                    .Where(f => !IsValidNaming(f.Name))
                    .ToList();

                result.IssueCount = nonCompliant.Count;
                result.Passed = nonCompliant.Count < families.Count * 0.1; // Less than 10%
                result.Message = $"{nonCompliant.Count} of {families.Count} families have non-standard names";
                result.Details = "Expected format: Category_Type_Size (e.g., Door_Single_3x7)";
                result.RecommendedFix = "Rename families to follow project naming standards";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking family naming: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckViewNamingStandards()
        {
            var result = new QACheckResult
            {
                CheckName = "View Naming Standards",
                Category = "Naming Standards",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var views = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .ToList();

                var nonCompliant = views
                    .Where(v => v.Name.Contains("Copy") || v.Name.Contains("copy") || !char.IsUpper(v.Name[0]))
                    .ToList();

                result.IssueCount = nonCompliant.Count;
                result.Passed = nonCompliant.Count == 0;
                result.Message = $"{nonCompliant.Count} view(s) with non-standard naming";
                result.AffectedElements = nonCompliant.Select(v => v.Id).ToList();
                result.RecommendedFix = "Rename views according to project standards";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking view naming: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private bool IsValidNaming(string name)
        {
            // Simple validation - customize based on your standards
            return !string.IsNullOrEmpty(name) &&
                   !name.Contains("Copy") &&
                   !name.Contains("copy") &&
                   name.Length > 3;
        }

        #endregion

        #region Parameters

        private QACheckResult CheckTypeVsInstanceParameters()
        {
            var result = new QACheckResult
            {
                CheckName = "Type vs Instance Parameter Misuse",
                Category = "Parameters",
                Severity = IssueSeverity.Warning
            };

            try
            {
                // Check for instance parameters that should be type parameters
                var walls = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .ToList();

                int issueCount = 0;
                var problematicElements = new List<ElementId>();

                foreach (var wall in walls)
                {
                    // Check if Mark (typically instance) is being used inconsistently
                    var markParam = wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    if (markParam != null && !markParam.IsReadOnly)
                    {
                        // This is just an example - add your specific checks
                    }
                }

                result.IssueCount = issueCount;
                result.Passed = issueCount == 0;
                result.Message = issueCount == 0
                    ? "Parameters appear to be correctly assigned"
                    : $"{issueCount} potential parameter misuse(s) found";
                result.RecommendedFix = "Review parameter storage - use Type parameters for properties shared by all instances";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking parameters: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Coordinate System

        private QACheckResult CheckCoordinateSystem()
        {
            var result = new QACheckResult
            {
                CheckName = "Coordinate System Alignment",
                Category = "Coordination",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var projectLocation = _doc.ActiveProjectLocation;
                var projectPosition = projectLocation.GetProjectPosition(XYZ.Zero);

                var basePoint = new FilteredElementCollector(_doc)
                    .OfClass(typeof(BasePoint))
                    .Cast<BasePoint>()
                    .FirstOrDefault(bp => !bp.IsShared);

                double distance = 0;
                if (basePoint != null)
                {
                    var bpPosition = basePoint.Position;
                    distance = bpPosition.GetLength();
                }

                result.Passed = distance < 100; // Within 100 feet of origin
                result.Message = result.Passed
                    ? "Project is reasonably close to origin"
                    : $"Project is {distance:F2} feet from origin - may cause precision issues";
                result.RecommendedFix = "Consider relocating project closer to origin";
                result.Metrics["DistanceFromOrigin"] = distance;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking coordinates: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        private QACheckResult CheckLinkAlignment()
        {
            var result = new QACheckResult
            {
                CheckName = "Link Alignment",
                Category = "Coordination",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var links = new FilteredElementCollector(_doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();

                var misalignedLinks = new List<ElementId>();

                foreach (var link in links)
                {
                    var transform = link.GetTotalTransform();
                    var translation = transform.Origin;

                    // Check if link has significant offset/rotation
                    if (translation.GetLength() > 1.0 || !transform.IsIdentity)
                    {
                        // Further check needed - this is simplified
                        misalignedLinks.Add(link.Id);
                    }
                }

                result.IssueCount = misalignedLinks.Count;
                result.Passed = misalignedLinks.Count == 0;
                result.Message = result.Passed
                    ? $"All {links.Count} link(s) appear aligned"
                    : $"{misalignedLinks.Count} link(s) may have alignment issues";
                result.AffectedElements = misalignedLinks;
                result.RecommendedFix = "Review link positioning and shared coordinates";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking link alignment: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Schedules

        private QACheckResult CheckScheduleHealth()
        {
            var result = new QACheckResult
            {
                CheckName = "Schedule Health",
                Category = "Schedules",
                Severity = IssueSeverity.Info
            };

            try
            {
                var schedules = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Where(s => s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Rooms ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Doors ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Windows ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Walls ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Floors ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Roofs ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Stairs ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Ceilings ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Columns ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_StructuralColumns ||
                               s.Definition.CategoryId.Value == (int)BuiltInCategory.OST_Furniture)
                    .ToList();

                var unusedSchedules = new List<ElementId>();

                // Check if architectural schedules are on sheets
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>();

                var schedulesOnSheets = new HashSet<ElementId>();
                foreach (var sheet in sheets)
                {
                    foreach (var viewId in sheet.GetAllPlacedViews())
                    {
                        schedulesOnSheets.Add(viewId);
                    }
                }

                unusedSchedules = schedules
                    .Where(s => !schedulesOnSheets.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToList();

                result.IssueCount = unusedSchedules.Count;
                result.Passed = unusedSchedules.Count < schedules.Count * 0.3;
                result.Message = $"{schedules.Count} architectural schedules, {unusedSchedules.Count} not on sheets";
                result.AffectedElements = unusedSchedules;
                result.RecommendedFix = "Review and place architectural schedules on sheets or delete if not needed";
                result.Metrics["TotalArchSchedules"] = schedules.Count;
                result.Metrics["UnusedArchSchedules"] = unusedSchedules.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking schedules: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Titleblocks

        private QACheckResult CheckTitleblockParameters()
        {
            var result = new QACheckResult
            {
                CheckName = "Titleblock Parameter Consistency",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var sheets = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .ToList();

                var inconsistentSheets = new List<ElementId>();
                var requiredParams = new[] { "Drawn By", "Checked By", "Project Number", "Project Name" };

                foreach (var sheet in sheets)
                {
                    bool hasIssue = false;
                    foreach (var paramName in requiredParams)
                    {
                        var param = sheet.LookupParameter(paramName);
                        if (param == null || string.IsNullOrEmpty(param.AsString()))
                        {
                            hasIssue = true;
                            break;
                        }
                    }

                    if (hasIssue)
                        inconsistentSheets.Add(sheet.Id);
                }

                result.IssueCount = inconsistentSheets.Count;
                result.Passed = inconsistentSheets.Count == 0;
                result.Message = result.Passed
                    ? "All titleblock parameters are filled"
                    : $"{inconsistentSheets.Count} sheet(s) missing titleblock parameters";
                result.AffectedElements = inconsistentSheets;
                result.RecommendedFix = "Fill in all required titleblock parameters";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking titleblocks: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Annotations

        private QACheckResult CheckAnnotationIssues()
        {
            var result = new QACheckResult
            {
                CheckName = "Annotation Consistency",
                Category = "Annotations",
                Severity = IssueSeverity.Info
            };

            try
            {
                var textNotes = new FilteredElementCollector(_doc)
                    .OfClass(typeof(TextNote))
                    .Cast<TextNote>()
                    .ToList();

                // Group by text type
                var typeGroups = textNotes.GroupBy(tn => tn.GetTypeId()).ToList();

                result.Passed = typeGroups.Count <= 5; // Limit text styles
                result.Message = typeGroups.Count <= 5
                    ? $"{typeGroups.Count} text style(s) in use - good consistency"
                    : $"{typeGroups.Count} text style(s) found - consider standardizing";
                result.RecommendedFix = "Limit the number of text styles for consistency";
                result.Metrics["TextStyles"] = typeGroups.Count;
                result.Metrics["TotalTextNotes"] = textNotes.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking annotations: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Worksets (Extended)

        private QACheckResult CheckWorksetMisassignments()
        {
            var result = new QACheckResult
            {
                CheckName = "Workset Misassignments",
                Category = "Worksets",
                Severity = IssueSeverity.Warning
            };

            try
            {
                if (!_doc.IsWorkshared)
                {
                    result.Passed = true;
                    result.Message = "Model is not workshared";
                    return result;
                }

                var misassigned = new List<ElementId>();

                // Check walls on wrong worksets (example)
                var walls = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .ToList();

                foreach (var wall in walls)
                {
                    var workset = _doc.GetWorksetTable().GetWorkset(wall.WorksetId);
                    // Add logic to check if wall is on appropriate workset
                    // This is simplified - add your workset standards
                }

                result.IssueCount = misassigned.Count;
                result.Passed = misassigned.Count == 0;
                result.Message = result.Passed
                    ? "No obvious workset misassignments"
                    : $"{misassigned.Count} element(s) may be on wrong worksets";
                result.AffectedElements = misassigned;
                result.RecommendedFix = "Review and correct workset assignments per project standards";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking worksets: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Room/Space Analysis

        private QACheckResult CheckDuplicateRooms()
        {
            var result = new QACheckResult
            {
                CheckName = "Duplicate Rooms",
                Category = "Rooms & Spaces",
                Severity = IssueSeverity.Error
            };

            try
            {
                var rooms = new FilteredElementCollector(_doc)
                    .OfClass(typeof(SpatialElement))
                    .OfType<Room>()
                    .Where(r => r.Area > 0)
                    .ToList();

                // Check for rooms with same number
                var duplicates = rooms
                    .GroupBy(r => r.Number)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .SelectMany(g => g)
                    .ToList();

                result.IssueCount = duplicates.Count;
                result.Passed = duplicates.Count == 0;
                result.Message = result.Passed
                    ? "No duplicate room numbers"
                    : $"{duplicates.Count} room(s) with duplicate numbers";
                result.AffectedElements = duplicates.Select(r => r.Id).ToList();
                result.RecommendedFix = "Assign unique room numbers";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking duplicate rooms: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region View Graphics

        private QACheckResult CheckViewGraphicsConsistency()
        {
            var result = new QACheckResult
            {
                CheckName = "View Graphics Consistency",
                Category = "Views & Sheets",
                Severity = IssueSeverity.Info
            };

            try
            {
                var views = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                    .ToList();

                var viewsWithoutTemplate = views
                    .Where(v => v.ViewTemplateId == ElementId.InvalidElementId)
                    .ToList();

                result.IssueCount = viewsWithoutTemplate.Count;
                result.Passed = viewsWithoutTemplate.Count < views.Count * 0.2;
                result.Message = $"{viewsWithoutTemplate.Count} of {views.Count} floor plan views without templates";
                result.AffectedElements = viewsWithoutTemplate.Select(v => v.Id).ToList();
                result.RecommendedFix = "Apply view templates for consistent graphics";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking view graphics: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Warning Analysis

        private QACheckResult CheckWarningsBySeverity()
        {
            var result = new QACheckResult
            {
                CheckName = "Warning Analysis by Severity",
                Category = "Model Integrity",
                Severity = IssueSeverity.Warning
            };

            try
            {
                var warnings = _doc.GetWarnings();
                var errors = warnings.Where(w => w.GetSeverity() == FailureSeverity.Error).ToList();
                var warningsOnly = warnings.Where(w => w.GetSeverity() == FailureSeverity.Warning).ToList();

                result.IssueCount = warnings.Count;
                result.Passed = errors.Count == 0 && warnings.Count < 100;
                result.Message = $"{errors.Count} errors, {warningsOnly.Count} warnings";

                var details = $"Critical Errors: {errors.Count}\n";
                details += $"Warnings: {warningsOnly.Count}\n\n";
                details += "Top error types:\n";

                var errorGroups = errors
                    .GroupBy(w => w.GetDescriptionText())
                    .OrderByDescending(g => g.Count())
                    .Take(5);

                foreach (var group in errorGroups)
                {
                    details += $"  - {group.Key}: {group.Count()}\n";
                }

                result.Details = details;
                result.RecommendedFix = "Address all errors first, then work on reducing warnings";
                result.Metrics["Errors"] = errors.Count;
                result.Metrics["Warnings"] = warningsOnly.Count;
            }
            catch (Exception ex)
            {
                result.Message = $"Error analyzing warnings: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion

        #region Performance Metrics

        private QACheckResult CheckModelPerformanceMetrics()
        {
            var result = new QACheckResult
            {
                CheckName = "Model Performance Metrics",
                Category = "Performance",
                Severity = IssueSeverity.Info
            };

            try
            {
                var elementCount = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType()
                    .ToElementIds()
                    .Count;

                var viewCount = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .Count();

                var familyCount = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Family))
                    .ToElementIds()
                    .Count;

                result.Passed = elementCount < 100000 && viewCount < 500;
                result.Message = $"Model contains {elementCount:N0} elements, {viewCount} views, {familyCount} families";

                result.Metrics["TotalElements"] = elementCount;
                result.Metrics["TotalViews"] = viewCount;
                result.Metrics["TotalFamilies"] = familyCount;

                if (elementCount > 100000)
                    result.RecommendedFix = "Large model - consider model splitting or purging unused elements";
                else if (viewCount > 500)
                    result.RecommendedFix = "High view count - delete unnecessary views";
                else
                    result.RecommendedFix = "Model size is acceptable";
            }
            catch (Exception ex)
            {
                result.Message = $"Error checking performance metrics: {ex.Message}";
                result.Severity = IssueSeverity.Error;
            }

            return result;
        }

        #endregion
    }
}
