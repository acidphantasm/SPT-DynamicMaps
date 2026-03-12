# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v9.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [B:\source\SPT-DynamicMaps\Server\mpstark-dynamicmaps.csproj](#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj)
  - [DynamicMaps.csproj](#dynamicmapscsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 2 | 1 require upgrade |
| Total NuGet Packages | 3 | All compatible |
| Total Code Files | 53 |  |
| Total Code Files with Incidents | 1 |  |
| Total Lines of Code | 8591 |  |
| Total Number of Issues | 1 |  |
| Estimated LOC to modify | 0+ | at least 0.0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [B:\source\SPT-DynamicMaps\Server\mpstark-dynamicmaps.csproj](#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj) | net9.0 | ✅ None | 0 | 0 |  | AspNetCore, Sdk Style = True |
| [DynamicMaps.csproj](#dynamicmapscsproj) | net471 | 🟢 Low | 0 | 0 |  | ClassLibrary, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 3 | 100.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 0 | 0.0% |
| ***Total NuGet Packages*** | ***3*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 7774 |  |
| ***Total APIs Analyzed*** | ***7774*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| SPTarkov.Common | 4.0.0 |  | [mpstark-dynamicmaps.csproj](#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj) | ✅Compatible |
| SPTarkov.DI | 4.0.0 |  | [mpstark-dynamicmaps.csproj](#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj) | ✅Compatible |
| SPTarkov.Server.Core | 4.0.0 |  | [mpstark-dynamicmaps.csproj](#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;DynamicMaps.csproj</b><br/><small>net471</small>"]
    P2["<b>📦&nbsp;mpstark-dynamicmaps.csproj</b><br/><small>net9.0</small>"]
    click P1 "#dynamicmapscsproj"
    click P2 "#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj"

```

## Project Details

<a id="b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj"></a>
### B:\source\SPT-DynamicMaps\Server\mpstark-dynamicmaps.csproj

#### Project Info

- **Current Target Framework:** net9.0✅
- **SDK-style**: True
- **Project Kind:** AspNetCore
- **Dependencies**: 0
- **Dependants**: 0
- **Number of Files**: 3
- **Lines of Code**: 384
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["mpstark-dynamicmaps.csproj"]
        MAIN["<b>📦&nbsp;mpstark-dynamicmaps.csproj</b><br/><small>net9.0</small>"]
        click MAIN "#b:sourcespt-dynamicmapsservermpstark-dynamicmapscsproj"
    end

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

#### Project Package References

| Package | Type | Current Version | Suggested Version | Description |
| :--- | :---: | :---: | :---: | :--- |
| SPTarkov.Common | Explicit | 4.0.0 |  | ✅Compatible |
| SPTarkov.DI | Explicit | 4.0.0 |  | ✅Compatible |
| SPTarkov.Server.Core | Explicit | 4.0.0 |  | ✅Compatible |

<a id="dynamicmapscsproj"></a>
### DynamicMaps.csproj

#### Project Info

- **Current Target Framework:** net471
- **Proposed Target Framework:** net9.0
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 0
- **Dependants**: 0
- **Number of Files**: 51
- **Number of Files with Incidents**: 1
- **Lines of Code**: 8207
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["DynamicMaps.csproj"]
        MAIN["<b>📦&nbsp;DynamicMaps.csproj</b><br/><small>net471</small>"]
        click MAIN "#dynamicmapscsproj"
    end

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 7774 |  |
| ***Total APIs Analyzed*** | ***7774*** |  |

