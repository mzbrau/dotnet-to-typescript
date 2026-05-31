---
id: contributing
title: Contributing
sidebar_position: 9
description: How to contribute safely to dotnet-to-typescript.
---

## Development setup

```bash
dotnet restore Source/DotnetToTypescript.sln
dotnet build Source/DotnetToTypescript.sln --no-restore
dotnet test Source/DotnetToTypescript.sln --no-build --verbosity normal
```

## Pull request expectations

- Keep changes focused and test-backed
- Update docs when user-facing behavior changes
- Include generated output updates when applicable

## Useful repository paths

- `Source/DotnetToTypescript` — core generator implementation
- `Source/DotnetToTypescript.Tests` — unit/integration tests
- `docs` — documentation content used by Docusaurus
- `website` — Docusaurus site configuration and UI
