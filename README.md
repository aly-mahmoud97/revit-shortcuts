# Revit Shortcuts Add-in

A template for creating Revit add-ins using C#. This add-in provides a basic structure for building custom commands and functionality for Autodesk Revit.

## Project Structure

```
RevitShortcuts/
├── Application.cs              # Main application class (IExternalApplication)
├── Commands/
│   └── HelloWorldCommand.cs   # Example external command
├── RevitShortcuts.csproj      # C# project file
└── RevitShortcuts.addin       # Revit manifest file
```

## Prerequisites

Before you begin, ensure you have the following installed:

1. **Visual Studio 2019 or later** (Community, Professional, or Enterprise)
   - Install the ".NET desktop development" workload

2. **Autodesk Revit** (2020 or later)
   - The template is configured for Revit 2024 by default

3. **.NET Framework 4.8**
   - Usually comes with Visual Studio

## Building the Add-in

### Step 1: Update Revit API References

1. Open `RevitShortcuts.csproj` in a text editor
2. Update the Revit API DLL paths to match your Revit installation:

```xml
<Reference Include="RevitAPI">
  <HintPath>C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll</HintPath>
  <Private>False</Private>
</Reference>
<Reference Include="RevitAPIUI">
  <HintPath>C:\Program Files\Autodesk\Revit 2024\RevitAPIUI.dll</HintPath>
  <Private>False</Private>
</Reference>
```

Replace `2024` with your Revit version (e.g., 2023, 2025).

### Step 2: Build the Project

1. Open the project in Visual Studio:
   - Double-click `RevitShortcuts.csproj` or
   - Open Visual Studio → File → Open → Project/Solution → Select `RevitShortcuts.csproj`

2. Select your build configuration:
   - Debug (for development)
   - Release (for production)

3. Build the project:
   - Press `Ctrl + Shift + B` or
   - Go to Build → Build Solution

4. The compiled DLL will be in:
   - `bin/Debug/RevitShortcuts.dll` or
   - `bin/Release/RevitShortcuts.dll`

## Installing and Activating the Add-in in Revit

### Method 1: Copy Files to Revit Add-ins Folder (Recommended)

1. **Locate your Revit Add-ins folder:**
   - For all users: `C:\ProgramData\Autodesk\Revit\Addins\2024\`
   - For current user: `C:\Users\[YourUsername]\AppData\Roaming\Autodesk\Revit\Addins\2024\`

   Replace `2024` with your Revit version.

2. **Copy the following files to the Add-ins folder:**
   ```
   - RevitShortcuts.dll (from bin/Debug or bin/Release)
   - RevitShortcuts.addin
   ```

3. **Verify the .addin file content:**
   Open `RevitShortcuts.addin` and ensure the `<Assembly>` path is correct:
   ```xml
   <Assembly>RevitShortcuts.dll</Assembly>
   ```

   If the DLL is in the same folder as the .addin file, this relative path works.
   Otherwise, use the full path:
   ```xml
   <Assembly>C:\Path\To\RevitShortcuts.dll</Assembly>
   ```

4. **Start Revit**
   - The add-in will load automatically

### Method 2: Development Setup (Auto-copy on Build)

For easier development, you can automatically copy files after each build:

1. Add a post-build event to `RevitShortcuts.csproj`:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="copy &quot;$(TargetPath)&quot; &quot;C:\ProgramData\Autodesk\Revit\Addins\2024\&quot;" />
  <Exec Command="copy &quot;$(ProjectDir)RevitShortcuts.addin&quot; &quot;C:\ProgramData\Autodesk\Revit\Addins\2024\&quot;" />
</Target>
```

2. Update the path to match your Revit version
3. Rebuild the project - files will copy automatically

## Verifying the Add-in is Loaded

1. **Start Revit**

2. **Check for the Ribbon Tab:**
   - Look for a new tab called "Revit Shortcuts" in the Revit ribbon
   - You should see a "Commands" panel with a "Hello World" button

3. **Check Revit Add-in Manager:**
   - In Revit, type `AD` (for Add-in Manager)
   - Or go to: Revit button → Options → Add-in Manager
   - Look for "Revit Shortcuts" in the list

4. **Test the Command:**
   - Click the "Hello World" button
   - You should see a dialog showing project information

## Troubleshooting

### Add-in doesn't appear in Revit

1. **Check the .addin file location:**
   - Ensure it's in the correct Addins folder for your Revit version

2. **Verify the .addin file syntax:**
   - XML must be valid
   - `<Assembly>` path must be correct (absolute or relative)
   - `<FullClassName>` must match your namespace and class name exactly

3. **Check Revit version compatibility:**
   - .NET Framework version must be compatible
   - API references must match your Revit version

4. **View Revit Warnings:**
   - Look at the Revit journal file for errors:
     `C:\Users\[YourUsername]\AppData\Local\Autodesk\Revit\[RevitVersion]\Journals\`

### Build errors

1. **RevitAPI.dll not found:**
   - Update the paths in the .csproj file to match your installation

2. **Wrong .NET Framework version:**
   - Revit 2020-2024 use .NET Framework 4.8
   - Update `<TargetFramework>` in the .csproj if needed

### Add-in loads but button doesn't work

1. **Check the namespace:**
   - In Application.cs, the button references `"RevitShortcuts.Commands.HelloWorldCommand"`
   - Ensure this matches the actual namespace and class name in HelloWorldCommand.cs

2. **Rebuild and recopy:**
   - Rebuild the project
   - Copy the new DLL to the Addins folder
   - Restart Revit

## Customizing the Add-in

### Adding New Commands

1. Create a new class in the `Commands` folder
2. Implement `IExternalCommand`
3. Add the `[Transaction]` attribute
4. Implement the `Execute` method
5. Add a button in `Application.cs` → `OnStartup` method

### Changing the Ribbon Tab Name

In `Application.cs`, modify:
```csharp
string tabName = "Revit Shortcuts";  // Change this
```

### Adding Icons

1. Add the required assembly reference to `RevitShortcuts.csproj`:
   ```xml
   <Reference Include="PresentationCore" />
   ```

2. Add the using statement to `Application.cs`:
   ```csharp
   using System.Windows.Media.Imaging;
   ```

3. Add image files (PNG, 32x32 pixels for large icons)
4. Set Build Action to "Embedded Resource"
5. Update button creation code in `Application.cs`:

```csharp
Uri iconUri = new Uri("pack://application:,,,/RevitShortcuts;component/Resources/icon.png");
BitmapImage icon = new BitmapImage(iconUri);
buttonData.LargeImage = icon;
```

## Development Tips

1. **Close Revit before rebuilding** - DLL files are locked while Revit is running

2. **Use Debug mode** during development for better error messages

3. **Check the Revit Journal file** for debugging information

4. **Use TaskDialog.Show()** for quick debugging messages

5. **Always wrap code in try-catch blocks** to handle errors gracefully

## Project Files Explained

- **Application.cs**: Loaded when Revit starts, creates the ribbon UI
- **HelloWorldCommand.cs**: Example command that executes when button is clicked
- **RevitShortcuts.csproj**: Project configuration, references, build settings
- **RevitShortcuts.addin**: Manifest file that tells Revit how to load the add-in

## References

- [Revit API Documentation](https://www.revitapidocs.com/)
- [Autodesk Revit Developer Center](https://www.autodesk.com/developer-network/platform-technologies/revit)
- [The Building Coder Blog](https://thebuildingcoder.typepad.com/)

## License

This template is provided as-is for educational and development purposes.

## Support

For issues or questions, please refer to the Revit API documentation or community forums.
