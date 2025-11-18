# NuGet Package Restore Instructions

If you're experiencing NuGet package restore errors, follow these steps:

## Option 1: Visual Studio Package Restore

1. **Open Visual Studio**
2. **Right-click on the solution** in Solution Explorer
3. **Select "Restore NuGet Packages"**
4. **Rebuild the solution**

## Option 2: Clear NuGet Cache

If packages still won't restore, clear the NuGet cache:

1. **Close Visual Studio**
2. **Open PowerShell or Command Prompt**
3. **Run the following command:**
   ```powershell
   dotnet nuget locals all --clear
   ```
   OR
   ```cmd
   nuget locals all -clear
   ```
4. **Reopen Visual Studio and restore packages**

## Option 3: Manual Package Restore via Command Line

In the project directory, run:

```powershell
dotnet restore
```

OR if using NuGet.exe:

```cmd
nuget restore RevitShortcuts.csproj
```

## Option 4: Check NuGet Package Sources

1. In Visual Studio, go to **Tools → Options → NuGet Package Manager → Package Sources**
2. Ensure **nuget.org** is enabled: `https://api.nuget.org/v3/index.json`
3. If it's missing, add it manually

## Required Packages

The project requires these NuGet packages:
- **EPPlus 4.5.3.3** - Excel export functionality
- **iTextSharp 5.5.13.3** - PDF export functionality
- **Newtonsoft.Json 13.0.3** - JSON export functionality

All packages are compatible with .NET Framework 4.8.

## Troubleshooting

If issues persist:

1. **Check internet connection** - NuGet needs to download packages
2. **Check firewall/proxy settings** - May block NuGet.org
3. **Verify .NET Framework 4.8 SDK** is installed
4. **Try running Visual Studio as Administrator**
5. **Check the nuget.config** file in the project root

## Alternative: Offline Package Installation

If you have network restrictions:

1. Download packages manually from https://www.nuget.org/
2. Place .nupkg files in a local folder
3. Add that folder as a package source in Visual Studio
4. Restore packages from the local source
