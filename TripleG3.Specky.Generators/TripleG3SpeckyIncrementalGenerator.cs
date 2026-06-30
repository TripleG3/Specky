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
    private static readonly DiagnosticDescriptor ServiceContractMismatchRule = new(
        id: "SPKY001",
        title: "Service contract mismatch",
        messageFormat: "'{0}' does not implement or inherit service contract '{1}'",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor OpenGenericArityMismatchRule = new(
        id: "SPKY002",
        title: "Open generic arity mismatch",
        messageFormat: "Open generic implementation '{0}' cannot be registered for '{1}' because generic arity does not match",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InterfaceImplementationRule = new(
        id: "SPKY003",
        title: "Interface cannot be implementation",
        messageFormat: "'{0}' is an interface and cannot be registered as an implementation type",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateRegistrationRule = new(
        id: "SPKY004",
        title: "Duplicate registration mapping",
        messageFormat: "'{0}' is registered more than once for service contract '{1}' with lifetime '{2}'",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PostInitLifetimeRule = new(
        id: "SPKY005",
        title: "Post-init requires singleton lifetime",
        messageFormat: "'{0}' uses a post-init registration pattern but is not configured as singleton",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FactoryVoidReturnRule = new(
        id: "SPKY006",
        title: "Factory method cannot return void",
        messageFormat: "Factory method '{0}' cannot return void",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FactoryReturnMismatchRule = new(
        id: "SPKY007",
        title: "Factory return type mismatch",
        messageFormat: "Factory method '{0}' returns '{1}', which does not satisfy service contract '{2}'",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FactorySignatureRule = new(
        id: "SPKY008",
        title: "Factory method signature is unsupported",
        messageFormat: "Factory method '{0}' must be static and accept no parameters or a single IServiceProvider parameter",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DescriptorProviderContractRule = new(
        id: "SPKY009",
        title: "Descriptor provider contract mismatch",
        messageFormat: "Descriptor provider '{0}' must implement ISpeckyDescriptorProvider",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DescriptorProviderConcreteRule = new(
        id: "SPKY010",
        title: "Descriptor provider must be concrete",
        messageFormat: "Descriptor provider '{0}' must be a concrete class",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DescriptorProviderConstructorRule = new(
        id: "SPKY011",
        title: "Descriptor provider constructor missing",
        messageFormat: "Descriptor provider '{0}' must have a public parameterless constructor",
        category: "TripleG3.Specky",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetRegistrationCandidate(syntaxContext))
            .Where(static candidate => candidate is not null);

        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetFactoryCandidate(syntaxContext))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(typeDeclarations.Collect()).Combine(methodDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndCandidates, static (sourceProductionContext, pair) =>
        {
            var ((compilation, candidates), factoryMethods) = pair;
            var registrations = BuildRegistrations(compilation, candidates!, sourceProductionContext);
            var factoryRegistrations = BuildFactoryRegistrations(compilation, factoryMethods!, sourceProductionContext);
            var descriptorProviders = BuildDescriptorProviders(compilation, candidates!, sourceProductionContext);
            var source = GenerateExtensionsSource(registrations, factoryRegistrations, descriptorProviders);
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

    private static IMethodSymbol? GetFactoryCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
    }

    private static ImmutableArray<RegistrationModel> BuildRegistrations(Compilation compilation, ImmutableArray<INamedTypeSymbol?> candidates, SourceProductionContext sourceProductionContext)
    {
        var registrations = ImmutableArray.CreateBuilder<RegistrationModel>();
        var seenRegistrations = new HashSet<RegistrationKey>();
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

                var lifetime = GetServiceLifetime(attribute);

                var serviceTypes = GetServiceTypes(attribute, candidate);
                var isPostInit = attribute.NamedArguments.Any(kvp => kvp.Key == "IsPostInit" && kvp.Value.Value is true)
                    || attribute.AttributeClass?.Name.Contains("PostInit", StringComparison.Ordinal) == true;

                foreach (var serviceType in serviceTypes)
                {
                    if (!TryValidateRegistration(candidate, serviceType, attribute, sourceProductionContext))
                    {
                        continue;
                    }

                    var registrationKey = new RegistrationKey(
                        candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        lifetime);

                    if (!seenRegistrations.Add(registrationKey))
                    {
                        var duplicateLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? candidate.Locations.FirstOrDefault();
                        if (duplicateLocation is not null)
                        {
                            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                                DuplicateRegistrationRule,
                                duplicateLocation,
                                candidate.ToDisplayString(),
                                serviceType.ToDisplayString(),
                                registrationKey.ServiceLifetime));
                        }

                        continue;
                    }

                    registrations.Add(new RegistrationModel(
                        ToTypeExpression(serviceType),
                        ToTypeExpression(candidate),
                        lifetime,
                        isPostInit));
                }
            }
        }

        return registrations.ToImmutable();
    }

    private static ImmutableArray<FactoryRegistrationModel> BuildFactoryRegistrations(Compilation compilation, ImmutableArray<IMethodSymbol?> candidates, SourceProductionContext sourceProductionContext)
    {
        var registrations = ImmutableArray.CreateBuilder<FactoryRegistrationModel>();
        var factoryAttributeSymbol = compilation.GetTypeByMetadataName("TripleG3.Specky.SpeckyFactoryAttribute");
        if (factoryAttributeSymbol is null)
        {
            return registrations.ToImmutable();
        }

        var seenRegistrations = new HashSet<RegistrationKey>();

        foreach (var candidate in candidates)
        {
            if (candidate is null)
            {
                continue;
            }

            foreach (var attribute in candidate.GetAttributes())
            {
                if (!InheritsFrom(attribute.AttributeClass, factoryAttributeSymbol))
                {
                    continue;
                }

                var serviceType = GetFactoryServiceType(attribute);
                if (serviceType is null || !TryValidateFactory(candidate, serviceType, attribute, sourceProductionContext))
                {
                    continue;
                }

                var lifetime = GetFactoryLifetime(attribute);
                var registrationKey = new RegistrationKey(
                    candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    lifetime);

                if (!seenRegistrations.Add(registrationKey))
                {
                    continue;
                }

                registrations.Add(new FactoryRegistrationModel(
                    ToTypeExpression(serviceType),
                    candidate.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    candidate.Name,
                    lifetime,
                    candidate.Parameters.Length == 1));
            }
        }

        return registrations.ToImmutable();
    }

    private static ImmutableArray<string> BuildDescriptorProviders(Compilation compilation, ImmutableArray<INamedTypeSymbol?> candidates, SourceProductionContext sourceProductionContext)
    {
        var descriptorProviders = ImmutableArray.CreateBuilder<string>();
        var descriptorProviderAttributeSymbol = compilation.GetTypeByMetadataName("TripleG3.Specky.SpeckyDescriptorProviderAttribute");
        var descriptorProviderInterfaceSymbol = compilation.GetTypeByMetadataName("TripleG3.Specky.ISpeckyDescriptorProvider");
        if (descriptorProviderAttributeSymbol is null || descriptorProviderInterfaceSymbol is null)
        {
            return descriptorProviders.ToImmutable();
        }

        foreach (var candidate in candidates)
        {
            if (candidate is null)
            {
                continue;
            }

            var hasProviderAttribute = candidate.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, descriptorProviderAttributeSymbol));
            if (!hasProviderAttribute)
            {
                continue;
            }

            var location = candidate.Locations.FirstOrDefault();
            if (location is null)
            {
                continue;
            }

            if (candidate.IsAbstract || candidate.TypeKind != TypeKind.Class)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DescriptorProviderConcreteRule,
                    location,
                    candidate.ToDisplayString()));
                continue;
            }

            if (!candidate.AllInterfaces.Any(interfaceType => SymbolEqualityComparer.Default.Equals(interfaceType, descriptorProviderInterfaceSymbol)))
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DescriptorProviderContractRule,
                    location,
                    candidate.ToDisplayString()));
                continue;
            }

            if (!candidate.Constructors.Any(constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public))
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DescriptorProviderConstructorRule,
                    location,
                    candidate.ToDisplayString()));
                continue;
            }

            descriptorProviders.Add(candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return descriptorProviders.ToImmutable();
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
            if (attribute.AttributeClass is { TypeArguments.Length: > 0 })
            {
                builder.Add(attribute.AttributeClass.TypeArguments[0]);
            }
            else
            {
                builder.Add(implementationType);
            }
        }

        return builder.ToImmutable();
    }

    private static ITypeSymbol? GetFactoryServiceType(AttributeData attribute)
    {
        foreach (var constructorArgument in attribute.ConstructorArguments)
        {
            if (constructorArgument.Kind == TypedConstantKind.Type && constructorArgument.Value is ITypeSymbol typeSymbol)
            {
                return typeSymbol;
            }
        }

        return attribute.AttributeClass is { TypeArguments.Length: > 0 }
            ? attribute.AttributeClass.TypeArguments[0]
            : null;
    }

    private static string GetFactoryLifetime(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length > 0)
        {
            return GetLifetimeName(attribute.ConstructorArguments[0]);
        }

        var attributeName = attribute.AttributeClass?.Name ?? string.Empty;
        if (attributeName.Contains("Scoped", StringComparison.Ordinal))
        {
            return "Scoped";
        }

        if (attributeName.Contains("Transient", StringComparison.Ordinal))
        {
            return "Transient";
        }

        return "Singleton";
    }

    private static bool TryValidateFactory(IMethodSymbol method, ITypeSymbol serviceType, AttributeData attribute, SourceProductionContext sourceProductionContext)
    {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? method.Locations.FirstOrDefault();
        if (location is null)
        {
            return false;
        }

        var methodName = method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        if (!method.IsStatic || method.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(FactorySignatureRule, location, methodName));
            return false;
        }

        if (method.IsGenericMethod || method.Parameters.Length > 1)
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(FactorySignatureRule, location, methodName));
            return false;
        }

        if (method.Parameters.Length == 1 && method.Parameters[0].Type.ToDisplayString() != "System.IServiceProvider")
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(FactorySignatureRule, location, methodName));
            return false;
        }

        if (method.ReturnsVoid)
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(FactoryVoidReturnRule, location, methodName));
            return false;
        }

        if (serviceType is INamedTypeSymbol namedServiceType && namedServiceType.IsGenericType && (namedServiceType.IsUnboundGenericType || namedServiceType.IsDefinition))
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(FactorySignatureRule, location, methodName));
            return false;
        }

        if (!FactoryReturnSatisfiesService(method.ReturnType, serviceType))
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                FactoryReturnMismatchRule,
                location,
                methodName,
                method.ReturnType.ToDisplayString(),
                serviceType.ToDisplayString()));
            return false;
        }

        return true;
    }

    private static bool FactoryReturnSatisfiesService(ITypeSymbol returnType, ITypeSymbol serviceType)
    {
        if (SymbolEqualityComparer.Default.Equals(returnType, serviceType))
        {
            return true;
        }

        if (returnType is INamedTypeSymbol namedReturnType)
        {
            return namedReturnType.AllInterfaces.Any(interfaceType => SymbolEqualityComparer.Default.Equals(interfaceType, serviceType))
                   || InheritsConcrete(namedReturnType, serviceType);
        }

        return false;
    }

    private static bool TryValidateRegistration(INamedTypeSymbol implementationType, ITypeSymbol serviceType, AttributeData attribute, SourceProductionContext sourceProductionContext)
    {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? implementationType.Locations.FirstOrDefault();
        if (location is null)
        {
            return false;
        }

        if (implementationType.TypeKind == TypeKind.Interface)
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                InterfaceImplementationRule,
                location,
                implementationType.ToDisplayString()));
            return false;
        }

        if ((attribute.AttributeClass?.Name.Contains("PostInit", StringComparison.Ordinal) ?? false)
            && GetServiceLifetime(attribute) != "Singleton")
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                PostInitLifetimeRule,
                location,
                implementationType.ToDisplayString()));
            return false;
        }

        if (serviceType is INamedTypeSymbol namedServiceType && namedServiceType.IsGenericType && implementationType.IsGenericType)
        {
            var serviceArity = namedServiceType.IsUnboundGenericType || namedServiceType.IsDefinition ? namedServiceType.TypeArguments.Length : namedServiceType.Arity;
            var implementationArity = implementationType.Arity;
            if (namedServiceType.IsUnboundGenericType || namedServiceType.IsDefinition || implementationType.IsDefinition)
            {
                if (serviceArity != implementationArity)
                {
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        OpenGenericArityMismatchRule,
                        location,
                        implementationType.ToDisplayString(),
                        serviceType.ToDisplayString()));
                    return false;
                }
            }
        }

        if (!ImplementsContract(implementationType, serviceType))
        {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                ServiceContractMismatchRule,
                location,
                implementationType.ToDisplayString(),
                serviceType.ToDisplayString()));
            return false;
        }

        return true;
    }

    private static string GetServiceLifetime(AttributeData attribute)
    {
        var lifetime = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ServiceLifetime").Value;
        if (lifetime.Value is null && attribute.ConstructorArguments.Length > 0)
        {
            lifetime = attribute.ConstructorArguments[0];
        }

        return GetLifetimeName(lifetime);
    }

    private static string GetLifetimeName(TypedConstant lifetime)
    {
        return lifetime.Value switch
        {
            1 => "Scoped",
            2 => "Transient",
            "Scoped" => "Scoped",
            "Transient" => "Transient",
            "Singleton" => "Singleton",
            _ => "Singleton"
        };
    }

    private static bool ImplementsContract(INamedTypeSymbol implementationType, ITypeSymbol serviceType)
    {
        if (SymbolEqualityComparer.Default.Equals(implementationType, serviceType))
        {
            return true;
        }

        if (serviceType is INamedTypeSymbol namedServiceType && namedServiceType.IsGenericType)
        {
            if (namedServiceType.IsUnboundGenericType || namedServiceType.IsDefinition)
            {
                var serviceDefinition = namedServiceType.ConstructedFrom;
                return implementationType.AllInterfaces.Any(i =>
                           i.IsGenericType &&
                           SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, serviceDefinition))
                       || InheritsOpenGeneric(implementationType, serviceDefinition);
            }
        }

        return implementationType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, serviceType))
               || InheritsConcrete(implementationType, serviceType);
    }

    private static bool InheritsConcrete(INamedTypeSymbol implementationType, ITypeSymbol serviceType)
    {
        for (var current = implementationType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, serviceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsOpenGeneric(INamedTypeSymbol implementationType, INamedTypeSymbol serviceType)
    {
        for (var current = implementationType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && SymbolEqualityComparer.Default.Equals(current.ConstructedFrom, serviceType))
            {
                return true;
            }
        }

        return false;
    }

    private static string ToTypeExpression(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            if (namedType.IsUnboundGenericType || namedType.TypeArguments.Any(static typeArgument => typeArgument.TypeKind == TypeKind.TypeParameter))
            {
                var genericDefinitionName = namedType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var genericArgumentStart = genericDefinitionName.IndexOf('<');
                if (genericArgumentStart >= 0)
                {
                    genericDefinitionName = genericDefinitionName.Substring(0, genericArgumentStart);
                }

                return $"typeof({genericDefinitionName}<{new string(',', namedType.Arity - 1)}>)";
            }
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

    private static string GenerateExtensionsSource(ImmutableArray<RegistrationModel> registrations, ImmutableArray<FactoryRegistrationModel> factoryRegistrations, ImmutableArray<string> descriptorProviderTypes)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
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

        foreach (var registration in factoryRegistrations.Distinct())
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
                .Append(", static services => ")
                .Append(registration.FactoryTypeName)
                .Append('.')
                .Append(registration.FactoryMethodName)
                .Append(registration.UsesServiceProvider ? "(services)" : "()")
                .AppendLine(");");
        }

        foreach (var descriptorProviderType in descriptorProviderTypes.Distinct())
        {
            builder.Append("        foreach (var descriptor in new ")
                .Append(descriptorProviderType)
                .AppendLine("().GetDescriptors())");
            builder.AppendLine("        {");
            builder.AppendLine("            services.Add(descriptor);");
            builder.AppendLine("        }");
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

    private sealed class FactoryRegistrationModel : IEquatable<FactoryRegistrationModel>
    {
        public FactoryRegistrationModel(string serviceTypeName, string factoryTypeName, string factoryMethodName, string serviceLifetime, bool usesServiceProvider)
        {
            ServiceTypeName = serviceTypeName;
            FactoryTypeName = factoryTypeName;
            FactoryMethodName = factoryMethodName;
            ServiceLifetime = serviceLifetime;
            UsesServiceProvider = usesServiceProvider;
        }

        public string ServiceTypeName { get; }

        public string FactoryTypeName { get; }

        public string FactoryMethodName { get; }

        public string ServiceLifetime { get; }

        public bool UsesServiceProvider { get; }

        public bool Equals(FactoryRegistrationModel? other)
            => other is not null
               && ServiceTypeName == other.ServiceTypeName
               && FactoryTypeName == other.FactoryTypeName
               && FactoryMethodName == other.FactoryMethodName
               && ServiceLifetime == other.ServiceLifetime
               && UsesServiceProvider == other.UsesServiceProvider;

        public override bool Equals(object? obj) => Equals(obj as FactoryRegistrationModel);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ServiceTypeName.GetHashCode();
                hash = (hash * 397) ^ FactoryTypeName.GetHashCode();
                hash = (hash * 397) ^ FactoryMethodName.GetHashCode();
                hash = (hash * 397) ^ ServiceLifetime.GetHashCode();
                hash = (hash * 397) ^ UsesServiceProvider.GetHashCode();
                return hash;
            }
        }
    }

    private sealed class RegistrationKey : IEquatable<RegistrationKey>
    {
        public RegistrationKey(string implementationType, string serviceType, string serviceLifetime)
        {
            ImplementationType = implementationType;
            ServiceType = serviceType;
            ServiceLifetime = serviceLifetime;
        }

        public string ImplementationType { get; }

        public string ServiceType { get; }

        public string ServiceLifetime { get; }

        public bool Equals(RegistrationKey? other)
            => other is not null
               && ImplementationType == other.ImplementationType
               && ServiceType == other.ServiceType
               && ServiceLifetime == other.ServiceLifetime;

        public override bool Equals(object? obj) => Equals(obj as RegistrationKey);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ImplementationType.GetHashCode();
                hash = (hash * 397) ^ ServiceType.GetHashCode();
                hash = (hash * 397) ^ ServiceLifetime.GetHashCode();
                return hash;
            }
        }
    }
}
