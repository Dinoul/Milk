# .NET Framework 4.8 Migration Plan

## Overview

**Target**: Migrate the Milk Windows Forms application from `net10.0-windows` to .NET Framework 4.8 (`net48`), retaining its WebView2-based HTML UI and native window behavior where compatible.
**Scope**: One SDK-style WinForms project with one direct NuGet package reference and bundled `wwwroot` content.

### Selected Strategy
**All-at-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: One project and no project dependency graph; an incremental strategy adds no value.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | The solution contains one project and has no project dependency graph to migrate incrementally. |

## Tasks

### 01-migrate-milk-net48: Migrate Milk to .NET Framework 4.8

Migrate `Milk/Milk.csproj` from `net10.0-windows` to the .NET Framework 4.8 TFM `net48`. Preserve the SDK-style Windows Forms project, `Form1` WebView2 host, HTML/JavaScript window controls, and `wwwroot` content-copy behavior where Framework-compatible. Replace the .NET 6+ WinForms startup initialization in `Program.cs` with the Framework-compatible startup sequence, and address any additional compile-time API differences revealed by the target build without dropping current UI or window functionality. Restore per-monitor DPI awareness and WinForms high-DPI resizing so the app does not become blurry after retargeting.

The generated assessment omitted the explicit `Microsoft.Web.WebView2` package reference. Manual inspection of version `1.0.4314.35` found `net462` Core and WinForms assemblies, which are compatible with `net48`; keep this exact package version and verify restore/build. Check the project's native Windows API calls and content output as part of the migration. The machine has the .NET 4.8 reference assemblies installed; the app still requires a WebView2 Runtime installation to display the HTML UI at runtime.

**Done when**: The project targets `net48`; it builds with no errors or warnings against .NET Framework 4.8 reference assemblies; WebView2 `1.0.4314.35` remains referenced and resolves; bundled `wwwroot` content remains configured for output; the current window-management and WebView2 messaging code remains present, with any incompatible APIs replaced by Framework-compatible equivalents; per-monitor DPI awareness and WinForms high-DPI auto-resizing are enabled.
