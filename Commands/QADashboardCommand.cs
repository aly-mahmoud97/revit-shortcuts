using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitShortcuts.Views;

namespace RevitShortcuts.Commands
{
    /// <summary>
    /// Command to launch the QA & Quality Checks Dashboard
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class QADashboardCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Check if a document is open
                if (doc == null)
                {
                    TaskDialog.Show("Error", "Please open a Revit document before running QA checks.");
                    return Result.Failed;
                }

                // Create and show the dashboard window
                var dashboard = new QADashboardWindow(uiDoc);
                dashboard.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle errors
                message = ex.Message;
                TaskDialog.Show("Error", $"An error occurred while launching the QA Dashboard:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
