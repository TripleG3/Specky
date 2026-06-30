using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TripleG3.Specky.Generators;

[Generator]
public sealed class TripleG3SpeckyIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetRegistrationCandidate(syntaxContext))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndCandidates, static (sourceProductionContext, pair) =>
        {
            var (compilation, candidates) = pair;
            var registrations = BuildRegistrations(compilation, candidates!);
            var source = GenerateExtensionsSource(registrations);
            sourceProductionContext.AddSource("TripleG3.Specky.Generated.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static INamedTypeSymbol? GetRegistrationCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
    }

    private static ImmutableArray<RegistrationModel> BuildRegistrations(Compilation compilation, ImmutableArray<INamedTypeSymbol?> candidates)
    {
        var registrations = ImmutableArray.CreateBuilder<RegistrationModel>();
        var speckAttributeSymbol = compilation.GetTypeByMetadataName("TripleG3.Specky.SpeckAttribute");
        if (speckAttributeSymbol is null)
        {
            return registrations.ToImmutable();
        }

        var seenCandidates = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var candidate in candidates)
        {
            if (candidate is null || !seenCandidates.Add(candidate))
            {
                continue;
            }

            foreach (var attribute in candidate.GetAttributes())
            {
                if (!InheritsFrom(attribute.AttributeClass, speckAttributeSymbol))
                {
                    continue;
                }

                var lifetime = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ServiceLifetime").Value;
                if (lifetime.Value is null && attribute.ConstructorArguments.Length > 0)
                {
                    lifetime = attribute.ConstructorArguments[0];
                }

                var serviceTypes = GetServiceTypes(attribute, candidate);
                var isPostInit = attribute.NamedArguments.Any(kvp => kvp.Key == "IsPostInit" && kvp.Value.Value is true)
                    || attribute.AttributeClass?.Name.Contains("PostInit", StringComparison.Ordinal) == true;

                foreach (var serviceType in serviceTypes)
                {
                    registrations.Add(new RegistrationModel(
                        ToTypeExpression(serviceType),
                        ToTypeExpression(candidate),
                        lifetime.Value?.ToString() ?? "Singleton",
                        isPostInit));
                }
            }
        }

        return registrations.ToImmutable();
    }

    private static ImmutableArray<ITypeSymbol> GetServiceTypes(AttributeData attribute, INamedTypeSymbol implementationType)
    {
        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();

        foreach (var constructorArgument in attribute.ConstructorArguments)
        {
            if (constructorArgument.Kind == TypedConstantKind.Type && constructorArgument.Value is ITypeSymbol typeSymbol)
            {
                builder.Add(typeSymbol);
            }
            else if (constructorArgument.Kind == TypedConstantKind.Array)
            {
                foreach (var value in constructorArgument.Values)
                {
                    if (value.Kind == TypedConstantKind.Type && value.Value is ITypeSymbol arrayTypeSymbol)
                    {
                        builder.Add(arrayTypeSymbol);
                    }
                }
            }
        }

        if (builder.Count == 0)
        {
            builder.Add(implementationType);
        }

        return builder.ToImmutable();
    }

    private static string ToTypeExpression(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var genericDefinitionName = namedType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var genericArgumentStart = genericDefinitionName.IndexOf('<');
            if (genericArgumentStart >= 0)
            {
                genericDefinitionName = genericDefinitionName.Substring(0, genericArgumentStart);
            }

            return $"typeof({genericDefinitionName})";
        }

        return $"typeof({symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
    }

    private static bool InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol baseType)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateExtensionsSource(ImmutableArray<RegistrationModel> registrations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine();
        builder.AppendLine("namespace TripleG3.Specky;");
        builder.AppendLine();
        builder.AppendLine("public static partial class GeneratedServiceCollectionExtensions");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Adds compile-time generated TripleG3.Specky registrations.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"services\">The service collection to populate.</param>");
        builder.AppendLine("    /// <returns>The same service collection instance.</returns>");
        builder.AppendLine("    public static IServiceCollection AddTripleG3SpeckyGenerated(this IServiceCollection services)");
        builder.AppendLine("    {");
        builder.AppendLine("        ArgumentNullException.ThrowIfNull(services);");
        builder.AppendLine();

        foreach (var registration in registrations.Distinct())
        {
            var methodName = registration.ServiceLifetime switch
            {
                "Scoped" => "AddScoped",
                "Transient" => "AddTransient",
                _ => "AddSingleton"
            };

            builder.Append("        services.")
                .Append(methodName)
                .Append("(")
                .Append(registration.ServiceTypeName)
                .Append(", ")
                .Append(registration.ImplementationTypeName)
                .AppendLine(");");
        }

        builder.AppendLine();
        builder.AppendLine("        return services;");
        builder.AppendLine("    }");

        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Adds compile-time generated TripleG3.Specky registrations.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"services\">The service collection to populate.</param>");
        builder.AppendLine("    /// <returns>The same service collection instance.</returns>");
        builder.AppendLine("    public static IServiceCollection AddTripleG3Specky(this IServiceCollection services)");
        builder.AppendLine("        => services.AddTripleG3SpeckyGenerated();");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private sealed class RegistrationModel : IEquatable<RegistrationModel>
    {
        public RegistrationModel(string serviceTypeName, string implementationTypeName, string serviceLifetime, bool isPostInit)
        {
            ServiceTypeName = serviceTypeName;
            ImplementationTypeName = implementationTypeName;
            ServiceLifetime = serviceLifetime;
            IsPostInit = isPostInit;
        }

        public string ServiceTypeName { get; }

        public string ImplementationTypeName { get; }

        public string ServiceLifetime { get; }

        public bool IsPostInit { get; }

        public bool Equals(RegistrationModel? other)
            => other is not null
               && ServiceTypeName == other.ServiceTypeName
               && ImplementationTypeName == other.ImplementationTypeName
               && ServiceLifetime == other.ServiceLifetime
               && IsPostInit == other.IsPostInit;

        public override bool Equals(object? obj) => Equals(obj as RegistrationModel);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ServiceTypeName.GetHashCode();
                hash = (hash * 397) ^ ImplementationTypeName.GetHashCode();
                hash = (hash * 397) ^ ServiceLifetime.GetHashCode();
                hash = (hash * 397) ^ IsPostInit.GetHashCode();
                return hash;
            }
        }
    }
}
