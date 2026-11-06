# 01-migrate-milk-net48: Migrate Milk to .NET Framework 4.8

## Scope Inventory
- **Projects affected**: `Milk/Milk.csproj` only; the solution contains one project and has no project-to-project references.
- **Distinct concerns**: Replace TFM, preserve SDK-style WinForms and implicit usings, update Framework-incompatible startup code, keep WebView2/package/content wiring, validate build and app assets.
- **Change signals**: Assessment reports one mandatory `Project.0002` TFM change, zero API issues, and zero packages. Its proposed `net48-windows` is not the .NET Framework TFM; use `net48`. The generated package inventory is incomplete: the csproj has an explicit WebView2 reference.
- **Decomposition**: Atomic single-project scope; no dependencies or test project found, so keep one task.

## Research Findings
- `Milk.csproj` defines `<TargetFramework>net10.0-windows</TargetFramework>` directly. `get_project_dependencies` found no imported project targets/package management files; the project parent chain has no `Directory.Build.props`, `Directory.Build.targets`, or `Directory.Packages.props`.
- Project settings are SDK-style Windows Forms, `Nullable=enable`, `ImplicitUsings=enable`, and `OutputType=WinExe`. Preserve those settings and keep singular `TargetFramework`, replacing it with `net48`.
- `Microsoft.Web.WebView2` is pinned at `1.0.4314.35`. The installed package contains `Microsoft.Web.WebView2.Core.dll` and `Microsoft.Web.WebView2.WinForms.dll` under `lib/net462`; these assets are framework-compatible with `net48`. Keep the version and verify restore.
- `Program.cs` calls `ApplicationConfiguration.Initialize()`, a modern .NET WinForms helper not present on .NET Framework. Use the supported classic startup setup (`EnableVisualStyles`, `SetCompatibleTextRenderingDefault(false)`, then `Application.Run`). Existing code uses nullable annotations, C# 8 `using` declarations, and SDK-generated implicit global usings; ensure the compiler language level supports the generated global usings without adding runtime dependencies.
- The user reported the migrated app looks low-resolution. The active `app.manifest` had its DPI-aware settings commented out, and startup no longer has the modern `ApplicationConfiguration.Initialize()` call. Preserve sharp rendering by enabling Framework-supported per-monitor DPI awareness in the manifest and WinForms DPI auto-resizing through app.config; retain the existing `DeviceDpi`-based sizing.
- `Form1.cs` contains WebView2 initialization/message handling, Win32 `DllImport` window drag/resize behavior, DWM rounded-corner handling, `System.Drawing` regions, `DeviceDpi`, and `AppContext.BaseDirectory`. The installed .NET Framework 4.8 WinForms reference assembly includes `Control.DeviceDpi`; preserve the behavior and let the build reveal any actual incompatibility.
- The csproj explicitly copies `wwwroot/**/*` to output and publish output. Keep this rule. `milk-shell.html` uses `milk_logo.png` for the welcome background and welcome page and `milk_logo1.jpg` in the header; both images are now present in `wwwroot` and should be copied to output.
- The .NET Framework 4.8 reference assemblies are present at the machine-level targeting pack path, .NET SDK 10.0.403 is installed, and Visual Studio MSBuild is available. The project includes `Form1.resx`; use the Visual Studio build path if a CLI build is needed. Runtime UI verification additionally requires the Microsoft Edge WebView2 Runtime, which is not established by a compile.
- `Milk.csproj.Backup.tmp` is not an active project file; do not modify it. No test project was discovered.

## Objective
Migrate the application to .NET Framework 4.8 while retaining its WebView2 package, UI assets, and window behavior where compatible.

## Scope
Modify the active project file and startup code as needed. Do not remove WebView2, replace the HTML UI, modify the inactive project backup, or silently change the target to a modern .NET framework.

## Steps
1. Replace the active project TFM with `net48` and retain WinForms, nullable, implicit usings, WebView2 1.0.4314.35, and `wwwroot` copy settings.
2. Replace modern-only startup initialization with the Framework-compatible WinForms startup sequence and restore per-monitor DPI awareness plus WinForms high-DPI auto-resizing.
3. Build the project/solution against .NET Framework 4.8 reference assemblies; resolve migration-related errors and warnings without suppressing warnings.
4. Verify package resolution, output content, DPI manifest, and generated runtime configuration; record runtime-only prerequisites and any unverified UI behavior.

**Done when**: The project targets `net48`; it builds with no errors or warnings against .NET Framework 4.8 reference assemblies; WebView2 `1.0.4314.35` remains referenced and resolves; bundled `wwwroot` content remains configured for output; the current window-management and WebView2 messaging code remains present, with any incompatible APIs replaced by Framework-compatible equivalents; the manifest enables per-monitor DPI awareness and `Milk.exe.config` enables WinForms high-DPI auto-resizing. Document any runtime-only prerequisite or verification blocker.
