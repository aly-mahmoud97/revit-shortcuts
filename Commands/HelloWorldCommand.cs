using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitShortcuts.Commands
{
    /// <summary>
    /// Example External Command that displays a simple message
    /// This demonstrates the basic structure of a Revit command
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class HelloWorldCommand : IExternalCommand
    {
        /// <summary>
        /// Execute method - called when the command is invoked
        /// </summary>
        /// <param name="commandData">Contains the Revit application and document</param>
        /// <param name="message">Can be set to show an error message</param>
        /// <param name="elements">Can be used to highlight elements in case of error</param>
        /// <returns>Result indicating success or failure</returns>
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

                // Example: Get some basic information from the document
                string projectName = doc.Title;
                string userName = doc.Application.Username;

                // Display a message dialog
                TaskDialog mainDialog = new TaskDialog("Hello World")
                {
                    MainInstruction = "Welcome to Revit Shortcuts!",
                    MainContent = $"This is your first Revit add-in command.\n\n" +
                                  $"Project: {projectName}\n" +
                                  $"User: {userName}",
                    CommonButtons = TaskDialogCommonButtons.Ok
                };

                mainDialog.Show();

                // Example: Start a transaction to modify the document
                // Uncomment the code below to create a simple wall

                /*
                using (Transaction trans = new Transaction(doc, "Create Sample Wall"))
                {
                    trans.Start();

                    // Create two points for the wall
                    XYZ start = new XYZ(0, 0, 0);
                    XYZ end = new XYZ(10, 0, 0);

                    // Create a line
                    Line line = Line.CreateBound(start, end);

                    // Get a level (assuming level 1 exists)
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    Level level = collector.OfClass(typeof(Level)).FirstElement() as Level;

                    if (level != null)
                    {
                        // Create the wall
                        Wall wall = Wall.Create(doc, line, level.Id, false);
                        TaskDialog.Show("Success", "Wall created successfully!");
                    }

                    trans.Commit();
                }
                */

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle errors
                message = ex.Message;
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
