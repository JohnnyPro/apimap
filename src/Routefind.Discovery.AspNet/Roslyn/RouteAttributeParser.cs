using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Routefind.Core.Index;

namespace Routefind.Discovery.AspNet.Roslyn;

/// <summary>
/// Parses HTTP method attributes on action methods to extract route information.
/// </summary>
public sealed class RouteAttributeParser
{
    private static readonly Dictionary<string, string> HttpMethodAttributes = new(StringComparer.Ordinal)
    {
        { "HttpGet", "GET" },
        { "HttpGetAttribute", "GET" },
        { "HttpPost", "POST" },
        { "HttpPostAttribute", "POST" },
        { "HttpPut", "PUT" },
        { "HttpPutAttribute", "PUT" },
        { "HttpDelete", "DELETE" },
        { "HttpDeleteAttribute", "DELETE" },
        { "HttpPatch", "PATCH" },
        { "HttpPatchAttribute", "PATCH" },
        { "HttpHead", "HEAD" },
        { "HttpHeadAttribute", "HEAD" },
        { "HttpOptions", "OPTIONS" },
        { "HttpOptionsAttribute", "OPTIONS" }
    };

    /// <summary>
    /// Extracts route definitions from action methods in a controller.
    /// </summary>
    /// <param name="controller">The controller info.</param>
    /// <param name="semanticModel">The semantic model for the controller's syntax tree.</param>
    /// <param name="repositoryRoot">The repository root for calculating relative paths.</param>
    /// <returns>Route definitions for each action method.</returns>
    public IEnumerable<RouteDefinition> ParseActions(
        ControllerInfo controller,
        SemanticModel semanticModel,
        string repositoryRoot)
    {
        var controllerName = controller.ClassSymbol.Name;

        foreach (var method in controller.ClassDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
            if (methodSymbol == null) continue;

            // Skip non-public methods
            if (methodSymbol.DeclaredAccessibility != Accessibility.Public) continue;

            foreach (var routeInfo in ExtractRoutesFromMethod(methodSymbol, method, controller, repositoryRoot, controllerName))
            {
                yield return routeInfo;
            }
        }
    }

    private IEnumerable<RouteDefinition> ExtractRoutesFromMethod(
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax,
        ControllerInfo controller,
        string repositoryRoot,
        string controllerName)
    {
        var actionName = methodSymbol.Name;
        var lineNumber = GetLineNumber(methodSyntax);
        var relativePath = GetRelativePath(controller.FilePath, repositoryRoot);

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            var attrName = attribute.AttributeClass?.Name ?? "";

            if (HttpMethodAttributes.TryGetValue(attrName, out var httpMethod))
            {
                var actionRoute = GetRouteTemplate(attribute);
                var fullPath = CombineRoutes(controller.RoutePrefix, actionRoute, controllerName, actionName);

                yield return new RouteDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    HttpMethod = httpMethod,
                    Path = fullPath,
                    Source = new SourceLocation
                    {
                        File = relativePath,
                        Line = lineNumber
                    },
                    Symbols = new RouteSymbols
                    {
                        Controller = controllerName,
                        Action = actionName
                    }
                };
            }
        }
    }

    private string? GetRouteTemplate(AttributeData attribute)
    {
        // Check constructor arguments first
        if (attribute.ConstructorArguments.Length > 0)
        {
            var value = attribute.ConstructorArguments[0].Value;
            if (value != null)
            {
                return value.ToString();
            }
        }

        // Check named arguments
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key is "template" or "Template")
            {
                return namedArg.Value.Value?.ToString();
            }
        }

        return null;
    }

    private string CombineRoutes(string? controllerPrefix, string? actionRoute, string controllerName, string actionName)
    {
        // Handle [controller] and [action] placeholders
        var prefix = controllerPrefix ?? "";
        var route = actionRoute ?? "";

        prefix = prefix.Replace("[controller]", GetControllerRouteName(controllerName), StringComparison.OrdinalIgnoreCase);
        prefix = prefix.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);

        route = route.Replace("[controller]", GetControllerRouteName(controllerName), StringComparison.OrdinalIgnoreCase);
        route = route.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);

        // Combine the paths
        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(route))
        {
            return "/";
        }

        if (string.IsNullOrEmpty(prefix))
        {
            return EnsureLeadingSlash(route);
        }

        if (string.IsNullOrEmpty(route))
        {
            return EnsureLeadingSlash(prefix);
        }

        // If action route starts with /, it's an absolute route
        if (route.StartsWith('/'))
        {
            return route;
        }

        // Combine prefix and route
        var combined = $"{prefix.TrimEnd('/')}/{route.TrimStart('/')}";
        return EnsureLeadingSlash(combined);
    }

    private string GetControllerRouteName(string controllerName)
    {
        // Remove "Controller" suffix if present
        if (controllerName.EndsWith("Controller", StringComparison.Ordinal))
        {
            return controllerName[..^10];
        }
        return controllerName;
    }

    private string EnsureLeadingSlash(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        return path.StartsWith('/') ? path : $"/{path}";
    }

    private int GetLineNumber(MethodDeclarationSyntax method)
    {
        var lineSpan = method.GetLocation().GetLineSpan();
        return lineSpan.StartLinePosition.Line + 1; // Convert to 1-indexed
    }

    private string GetRelativePath(string filePath, string repositoryRoot)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(repositoryRoot))
        {
            return filePath;
        }

        var fullPath = Path.GetFullPath(filePath);
        var rootPath = Path.GetFullPath(repositoryRoot);

        if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath[(rootPath.Length)..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }

        return filePath;
    }
}
