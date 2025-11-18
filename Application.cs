using System;
using System.Windows.Media.Imaging;
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

                // Add a push button to the panel
                PushButtonData buttonData = new PushButtonData(
                    "HelloWorldButton",
                    "Hello World",
                    typeof(Application).Assembly.Location,
                    "RevitShortcuts.Commands.HelloWorldCommand"
                );

                // Optional: Set tooltip
                buttonData.ToolTip = "Click to execute Hello World command";
                buttonData.LongDescription = "This is a sample command that demonstrates basic Revit add-in functionality.";

                // Optional: Add an icon (uncomment if you have an icon file)
                // Uri iconUri = new Uri("pack://application:,,,/RevitShortcuts;component/Resources/icon.png");
                // BitmapImage icon = new BitmapImage(iconUri);
                // buttonData.LargeImage = icon;

                PushButton button = panel.AddItem(buttonData) as PushButton;

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
