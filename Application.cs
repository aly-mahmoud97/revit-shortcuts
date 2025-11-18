using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace RevitShortcuts
{
    /// <summary>
    /// Main application class that implements IExternalApplication
    /// This class is loaded when Revit starts and can be used to add ribbon buttons, panels, etc.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        /// <summary>
        /// Called when Revit starts up
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create a ribbon tab
                string tabName = "Revit Shortcuts";
                application.CreateRibbonTab(tabName);

                // Create a ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Commands");

                // Add Hello World button
                PushButtonData helloButtonData = new PushButtonData(
                    "HelloWorldButton",
                    "Hello World",
                    typeof(Application).Assembly.Location,
                    "RevitShortcuts.Commands.HelloWorldCommand"
                );

                helloButtonData.ToolTip = "Click to execute Hello World command";
                helloButtonData.LongDescription = "This is a sample command that demonstrates basic Revit add-in functionality.";

                PushButton helloButton = panel.AddItem(helloButtonData) as PushButton;

                // Add QA Dashboard button
                PushButtonData qaDashboardButtonData = new PushButtonData(
                    "QADashboardButton",
                    "QA Dashboard",
                    typeof(Application).Assembly.Location,
                    "RevitShortcuts.Commands.QADashboardCommand"
                );

                qaDashboardButtonData.ToolTip = "Open QA & Quality Checks Dashboard";
                qaDashboardButtonData.LongDescription = "Launch the comprehensive Quality Assurance dashboard to check your Revit model for common issues, warnings, and quality problems.";

                PushButton qaDashboardButton = panel.AddItem(qaDashboardButtonData) as PushButton;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to initialize add-in: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Called when Revit shuts down
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            // Clean up resources if needed
            return Result.Succeeded;
        }
    }
}
