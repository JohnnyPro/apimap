# Routefind (ApiMap)

A fast, extensible CLI tool that discovers, indexes, and surfaces HTTP routes from backend codebases.

## Features

- **Static route discovery** - Analyze your codebase without running it
- **ASP.NET Web API support** - Discovers attribute-based routes from controllers
- **Cached index** - Fast lookups after initial discovery
- **Extensible architecture** - Designed to support additional frameworks/languages

## Requirements

Before running the application, ensure you have:

1. **.NET 8.0 SDK** - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Visual Studio 2022** or **Visual Studio Build Tools** - Required for Roslyn's MSBuild workspaces to analyze .csproj files

## Installation

### Build from Source

```bash
# Clone the repository
cd c:\Work\Fun\apimap

# Build the solution
dotnet build

# Run the CLI
cd src\Routefind.Cli
dotnet run -- --help
```

### Global Tool (Optional)

```bash
# From the solution root
dotnet pack src\Routefind.Cli
dotnet tool install --global --add-source src\Routefind.Cli\bin\Release routefind
```

## Usage

### Discover Routes

Navigate to your ASP.NET project directory and run:

```bash
# Using dotnet run (from Routefind.Cli directory)
dotnet run -- discover

# Or with the built executable
routefind discover
```

On first run, you'll be prompted to select the project type:

```
No route index found.
What type of project is this?

  [1] ASP.NET Web API

Select option: 1
```

### List Routes

```bash
# List all routes
dotnet run -- list

# Filter by HTTP method
dotnet run -- list --method GET

# Filter by path
dotnet run -- list --path users
```

Example output:

```
GET     /api/users          Controllers/UsersController.cs:25
POST    /api/users          Controllers/UsersController.cs:48
GET     /api/users/{id}     Controllers/UsersController.cs:62
DELETE  /api/users/{id}     Controllers/UsersController.cs:85

Total: 4 route(s)
```

### Force Re-discovery

```bash
dotnet run -- rediscover
# or
dotnet run -- discover
```

## Index File

Routes are cached in `.apimap/index.json`:

```json
{
  "version": 1,
  "generatedAt": "2026-01-13T14:00:00.000Z",
  "project": {
    "root": "/path/to/project",
    "language": "csharp",
    "framework": "aspnet"
  },
  "routes": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "httpMethod": "GET",
      "path": "/api/users",
      "source": {
        "file": "Controllers/UsersController.cs",
        "line": 25
      },
      "symbols": {
        "controller": "UsersController",
        "action": "GetAll"
      }
    }
  ]
}
```

> **Note:** Add `.apimap/` to your `.gitignore` file.

## Supported Patterns

### ASP.NET Web API

- Controllers inheriting from `ControllerBase` or `Controller`
- Classes with `[ApiController]` attribute
- `[Route("api/[controller]")]` on controllers
- `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]` on actions
- Route templates with parameters: `[HttpGet("{id}")]`
- `[controller]` and `[action]` placeholders

### Not Yet Supported

- Minimal APIs
- Conventional routing
- Route inheritance chains
- Endpoint filters

## Architecture

```
┌─────────────────────────────┐
│ CLI (Routefind.Cli)         │
│ - Command parsing           │
│ - User interaction          │
│ - Index orchestration       │
└─────────────┬───────────────┘
              │
┌─────────────▼───────────────┐
│ Core (Routefind.Core)       │
│ - Index schema              │
│ - Cache handling            │
│ - Search / filtering        │
└─────────────┬───────────────┘
              │
┌─────────────▼───────────────┐
│ Discovery Backends          │
│ - ASP.NET (Roslyn)          │
│ - (future) FastAPI, Express │
└─────────────────────────────┘
```

## License

MIT
