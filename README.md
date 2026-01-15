# ApiMap

A fast, extensible CLI tool that discovers, indexes, and searches HTTP routes from backend codebases.

## Features

- **Fuzzy search** - Find routes with case-insensitive fuzzy matching
- **Static route discovery** - Analyze your codebase without running it
- **ASP.NET Web API support** - Discovers attribute-based routes from controllers
- **Cached index** - Fast lookups after initial discovery
- **Extensible architecture** - Designed to support additional frameworks/languages

## Requirements

1. **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Visual Studio 2022** or **Build Tools** - Required for Roslyn's MSBuild workspaces

## Installation

### Build and Install Globally

```bash
cd ./apimap
dotnet pack src\Routefind.Cli -c Release
dotnet tool install --global --add-source src\Routefind.Cli\bin\Release apimap
```

## Usage

### Search Routes (Default)

```bash
# Search for routes matching a pattern (default command)
apimap settings
apimap users/
apimap 'api/v1'

# Filter by type: 'c' for controller, 'e' for endpoint
apimap settings -t c          # Controllers only
apimap users -t e             # Endpoints only

# Filter by HTTP method (implies -t e)
apimap users --method GET
apimap users -m POST
```

### Other Commands

```bash
apimap list                   # List all routes
apimap discover               # Force route discovery
apimap rediscover             # Alias for discover
apimap --help                 # Show help
```

### Example Output

```
# Endpoint search
GET     /api/settings         Controllers/SettingsController.cs:25
POST    /api/settings         Controllers/SettingsController.cs:48
GET     /api/settings/{id}    Controllers/SettingsController.cs:62

Found 3 result(s)

# Controller search (-t c)
SettingsController
  Controllers/SettingsController.cs
    :25 GET     /api/settings
    :48 POST    /api/settings
    :62 GET     /api/settings/{id}
```

## Index File

Routes are cached in `.apimap/index.json`. Add it to your `.gitignore`:

```
.apimap/
```

## Supported Patterns

### ASP.NET Web API

- Controllers inheriting from `ControllerBase` or `Controller`
- `[ApiController]` attribute
- `[Route("api/[controller]")]` on controllers
- `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
- Route templates with parameters: `[HttpGet("{id}")]`

## Architecture

```
CLI (Routefind.Cli)         ← System.CommandLine
         │
Core (Routefind.Core)       ← Language-agnostic
         │
Discovery.AspNet            ← Roslyn + MSBuildWorkspace
```

## License

MIT
