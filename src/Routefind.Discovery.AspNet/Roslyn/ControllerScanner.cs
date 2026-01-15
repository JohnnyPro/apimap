using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Routefind.Discovery.AspNet.Roslyn;

/// <summary>
/// Scans compilations for ASP.NET controller classes.
/// </summary>
public sealed class ControllerScanner
{
    private static readonly HashSet<string> ControllerBaseTypes = new(StringComparer.Ordinal)
    {
        "ControllerBase",
        "Controller",
        "Microsoft.AspNetCore.Mvc.ControllerBase",
        "Microsoft.AspNetCore.Mvc.Controller"
    };

    private static readonly HashSet<string> ControllerAttributes = new(StringComparer.Ordinal)
    {
        "ApiController",
        "ApiControllerAttribute",
        "Microsoft.AspNetCore.Mvc.ApiControllerAttribute"
    };

    /// <summary>
    /// Finds all controller classes in the given compilation.
    /// </summary>
    /// <param name="compilation">The compilation to scan.</param>
    /// <returns>Information about each controller found.</returns>
    public IEnumerable<ControllerInfo> FindControllers(Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                if (IsController(classSymbol))
                {
                    var routePrefix = GetControllerRoutePrefix(classSymbol);
                    yield return new ControllerInfo
                    {
                        ClassDeclaration = classDeclaration,
                        ClassSymbol = classSymbol,
                        RoutePrefix = routePrefix,
                        FilePath = syntaxTree.FilePath
                    };
                }
            }
        }
    }

    private bool IsController(INamedTypeSymbol classSymbol)
    {
        // Check if class inherits from ControllerBase or Controller
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (ControllerBaseTypes.Contains(baseType.Name) ||
                ControllerBaseTypes.Contains(baseType.ToDisplayString()))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        // Check if class has [ApiController] attribute
        foreach (var attribute in classSymbol.GetAttributes())
        {
            var attrName = attribute.AttributeClass?.Name ?? "";
            var attrFullName = attribute.AttributeClass?.ToDisplayString() ?? "";

            if (ControllerAttributes.Contains(attrName) ||
                ControllerAttributes.Contains(attrFullName))
            {
                return true;
            }
        }

        return false;
    }

    private string? GetControllerRoutePrefix(INamedTypeSymbol classSymbol)
    {
        foreach (var attribute in classSymbol.GetAttributes())
        {
            var attrName = attribute.AttributeClass?.Name ?? "";

            if (attrName is "Route" or "RouteAttribute")
            {
                // Get the route template from the constructor argument
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var template = attribute.ConstructorArguments[0].Value?.ToString();
                    return template;
                }

                // Check named arguments
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if (namedArg.Key == "template" || namedArg.Key == "Template")
                    {
                        return namedArg.Value.Value?.ToString();
                    }
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Information about a discovered controller class.
/// </summary>
public sealed class ControllerInfo
{
    public required ClassDeclarationSyntax ClassDeclaration { get; init; }
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public string? RoutePrefix { get; init; }
    public required string FilePath { get; init; }
}
