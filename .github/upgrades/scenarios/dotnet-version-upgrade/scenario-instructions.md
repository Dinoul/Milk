# .NET Framework 4.8 Migration

## Strategy
**Selected**: All-at-Once
**Rationale**: One SDK-style WinForms project with no project dependency graph; an incremental strategy adds no value.

### Execution Constraints
- Treat the requested `net10.0-windows` to .NET Framework `net48` move as a single atomic migration; do not substitute a newer .NET target.
- Keep Microsoft.Web.WebView2 1.0.4314.35 and the bundled `wwwroot` HTML in the project; verify package restore and output content on the Framework target.
- Replace unsupported modern .NET startup APIs while retaining the current window and native message behavior where the Framework APIs allow.
- Preserve crisp, per-monitor DPI rendering with .NET Framework-compatible manifest and app.config settings; the prior DPI-aware manifest block was commented out.
- Build the solution against .NET Framework 4.8 reference assemblies and report any SDK, reference-assembly, WebView2 runtime, or API blockers.

## Preferences
- **Flow Mode**: Automatic
- **Commit Strategy**: After Each Task (not applicable because the workspace is not a Git repository)
- **Target Framework**: .NET Framework 4.8 (`net48`)
- **UI**: Preserve the existing WebView2 HTML UI and window behavior where compatible.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

## Decisions
- The installed scenario list does not include `dotnet-framework-version-upgrade`, and the available .NET options tool returned only .NET 11.0. Use the available `dotnet-version-upgrade` scenario as a workflow shell, but do not use its framework recommendation or assume its assessment model supports this downgrade.
- The workspace is not a Git repository; source-control branch setup and commits are not applicable.
- The user confirmed the All-at-Once strategy for this single-project migration.
