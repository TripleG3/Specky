# Plan To Complete Specky DI Coverage

Status: Implemented on 2026-06-30.

The plan below is retained as a design record. Factory registrations, descriptor-provider registrations, cleaner open generic attributes, runtime scanning support, generated registration support, diagnostics, README updates, and validation tests have been added.

This plan focuses on the DI use cases from the Microsoft .NET dependency injection basics guide that Specky does not fully cover yet.

Specky already covers the common type-registration path well:

```csharp
[Scoped<IMailer>]
public sealed class Mailer : IMailer;

builder.Services.AddTripleG3Specky();
```

The remaining gaps are mostly around **factory registrations**, **instance registrations**, and **custom descriptor-style registration**.

---

## Goals

1. Support attribute-driven factory registrations.
2. Support attribute-driven instance registrations where practical.
3. Support explicit descriptor-style registrations for advanced cases.
4. Keep source generation as the preferred path.
5. Keep runtime scanning as a fallback.
6. Add diagnostics so invalid usage is caught early.
7. Keep examples small enough for humans and AI models to copy safely.

---

## Non-Goals

These should stay out of scope for Specky:

- replacing `BuildServiceProvider()`
- replacing `GetRequiredService<T>()`
- owning service provider lifetime management
- replacing Microsoft.Extensions.DependencyInjection itself
- hiding advanced .NET DI concepts from users who intentionally need them

Specky should populate `IServiceCollection`; .NET DI should still build and resolve.

---

## Current Coverage Snapshot

| Use Case | Current Status |
|---|---|
| `AddSingleton<TService, TImplementation>()` | Covered |
| `AddScoped<TService, TImplementation>()` | Covered |
| `AddTransient<TService, TImplementation>()` | Covered |
| concrete self-registration | Covered |
| keyed service registration | Covered |
| multi-service registration | Covered |
| conservative open generic registration | Covered |
| compile-time diagnostics | Covered for core invalid patterns |
| factory registration | Covered |
| implementation instance registration | Covered through factory methods |
| arbitrary `ServiceDescriptor` registration | Covered through descriptor providers |
| custom runtime construction logic | Covered through factory methods and descriptor providers |

---

## Phase 1: Factory Registration

### Problem

Microsoft DI supports this:

```csharp
services.AddSingleton<IConsole>(implementationFactory: static _ => new DefaultConsole
{
    IsEnabled = true
});
```

Specky does not currently have an attribute shape for this.

### Proposed API

Use a factory method marked with an attribute. This is source-generator friendly and avoids trying to put executable logic inside attribute constructor arguments.

```csharp
using Microsoft.Extensions.DependencyInjection;
using TripleG3.Specky;

public static class ConsoleRegistration
{
    [SingletonFactory<IConsole>]
    public static IConsole Create(IServiceProvider services)
        => new DefaultConsole
        {
            IsEnabled = true
        };
}
```

Also support parameterless factories:

```csharp
public static class ConsoleRegistration
{
    [SingletonFactory<IConsole>]
    public static IConsole Create()
        => new DefaultConsole { IsEnabled = true };
}
```

### Generated Output

```csharp
services.AddSingleton<IConsole>(static services => ConsoleRegistration.Create(services));
```

or:

```csharp
services.AddSingleton<IConsole>(static _ => ConsoleRegistration.Create());
```

### Attributes To Add

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SingletonFactoryAttribute<TService> : Attribute where TService : class;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ScopedFactoryAttribute<TService> : Attribute where TService : class;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TransientFactoryAttribute<TService> : Attribute where TService : class;
```

Optional non-generic `Type` versions for open generics or runtime scanning:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ServiceFactoryAttribute : Attribute
{
    public ServiceFactoryAttribute(ServiceLifetime lifetime, Type serviceType);
}
```

### Validation Rules

Diagnostics should report errors when:

- method returns `void`
- method return type is not assignable to service type
- method has unsupported parameters
- method is generic but cannot be mapped safely
- factory method is instance-based and no construction strategy exists

Recommended supported signatures for first pass:

```csharp
static TImplementation Create();
static TImplementation Create(IServiceProvider services);
```

### Diagnostics

Add diagnostics:

| ID | Severity | Meaning |
|---|---|---|
| `SPKY006` | Error | Factory method returns `void` |
| `SPKY007` | Error | Factory return type does not satisfy service type |
| `SPKY008` | Error | Factory method signature is unsupported |

---

## Phase 2: Runtime Factory Scanning

### Problem

Runtime scanning should support the same factory method model where possible.

### Runtime Registration Example

```csharp
builder.Services.AddSpecks<Program>();
```

Specky scans:

```csharp
public static class ConsoleRegistration
{
    [SingletonFactory<IConsole>]
    public static IConsole Create(IServiceProvider services)
        => new DefaultConsole { IsEnabled = true };
}
```

and registers equivalent behavior.

### Runtime Implementation Approach

Use reflection to create a delegate around the static method.

Pseudo-code:

```csharp
private static object InvokeFactory(MethodInfo method, IServiceProvider services)
{
    var parameters = method.GetParameters();

    return parameters.Length switch
    {
        0 => method.Invoke(null, null)!,
        1 when parameters[0].ParameterType == typeof(IServiceProvider)
            => method.Invoke(null, [services])!,
        _ => throw new SpeckyException("Unsupported factory method signature.")
    };
}
```

Register:

```csharp
services.Add(ServiceDescriptor.Describe(
    serviceType,
    provider => InvokeFactory(methodInfo, provider),
    lifetime));
```

---

## Phase 3: Instance Registration

### Problem

Microsoft DI supports this:

```csharp
services.AddSingleton<IConsole>(new DefaultConsole
{
    IsEnabled = true
});
```

Attributes cannot safely hold arbitrary object instances.

### Recommended Specky Model

Use a factory method for instance-like registration.

```csharp
public static class ConsoleRegistration
{
    private static readonly DefaultConsole Console = new()
    {
        IsEnabled = true
    };

    [SingletonFactory<IConsole>]
    public static IConsole Create() => Console;
}
```

This covers most practical instance-registration scenarios without inventing strange attribute behavior.

### Optional Convenience Attribute

If true instance registration is still desired, add a provider type pattern:

```csharp
public interface ISpeckyInstanceProvider<out TService>
{
    TService Instance { get; }
}
```

Example:

```csharp
[SingletonInstance<IConsole, ConsoleInstanceProvider>]
public sealed class ConsoleInstanceProvider : ISpeckyInstanceProvider<IConsole>
{
    public IConsole Instance { get; } = new DefaultConsole { IsEnabled = true };
}
```

Generated output:

```csharp
services.AddSingleton<IConsole>(new ConsoleInstanceProvider().Instance);
```

### Recommendation

Do **not** implement instance registration first. Factory registration covers this more cleanly.

---

## Phase 4: Descriptor-Style Registration

### Problem

Microsoft DI supports direct `ServiceDescriptor` construction:

```csharp
services.Add(ServiceDescriptor.Describe(
    serviceType: typeof(IConsole),
    implementationType: typeof(DefaultConsole),
    lifetime: ServiceLifetime.Singleton));
```

Specky already covers the implementation-type case, but not custom descriptor factories.

### Proposed API

```csharp
public interface ISpeckyDescriptorProvider
{
    IEnumerable<ServiceDescriptor> GetDescriptors();
}
```

Attribute:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SpeckyDescriptorProviderAttribute : Attribute;
```

Example:

```csharp
[SpeckyDescriptorProvider]
public sealed class ConsoleDescriptorProvider : ISpeckyDescriptorProvider
{
    public IEnumerable<ServiceDescriptor> GetDescriptors()
    {
        yield return ServiceDescriptor.Describe(
            typeof(IConsole),
            static _ => new DefaultConsole { IsEnabled = true },
            ServiceLifetime.Singleton);
    }
}
```

Generated output:

```csharp
foreach (var descriptor in new ConsoleDescriptorProvider().GetDescriptors())
{
    services.Add(descriptor);
}
```

### Validation Rules

Diagnostics should report errors when:

- provider type does not implement `ISpeckyDescriptorProvider`
- provider has no public parameterless constructor
- provider is abstract
- provider is an interface

---

## Phase 5: Improve Open Generic Ergonomics

### Problem

Current open generic usage is awkward:

```csharp
[MultiServiceSpeck(ServiceLifetime.Scoped, typeof(IRepository<>), typeof(IRepository<>))]
public sealed class Repository<T> : IRepository<T>;
```

### Proposed API

Add `Type`-based overload attributes:

```csharp
[Scoped(typeof(IRepository<>))]
public sealed class Repository<T> : IRepository<T>;
```

Also:

```csharp
[Singleton(typeof(ICache<>))]
public sealed class Cache<T> : ICache<T>;

[Transient(typeof(IHandler<,>))]
public sealed class Handler<TRequest, TResponse> : IHandler<TRequest, TResponse>;
```

### Implementation

Add constructors:

```csharp
public class ScopedAttribute : SpeckAttribute
{
    public ScopedAttribute() : base(ServiceLifetime.Scoped) { }
    public ScopedAttribute(Type serviceType) : base(ServiceLifetime.Scoped, serviceType) { }
}
```

Repeat for:

- `SingletonAttribute`
- `TransientAttribute`
- `SingletonPostInitAttribute`

### Benefit

This removes one README limit and makes open generic registrations feel natural.

---

## Phase 6: Documentation Updates

Update `README.md` with the new patterns.

### Factory Example

```csharp
public static class ConsoleFactory
{
    [SingletonFactory<IConsole>]
    public static IConsole Create()
        => new DefaultConsole { IsEnabled = true };
}
```

### Descriptor Provider Example

```csharp
[SpeckyDescriptorProvider]
public sealed class ConsoleDescriptors : ISpeckyDescriptorProvider
{
    public IEnumerable<ServiceDescriptor> GetDescriptors()
    {
        yield return ServiceDescriptor.Singleton<IConsole>(
            new DefaultConsole { IsEnabled = true });
    }
}
```

### Cleaner Open Generic Example

```csharp
[Scoped(typeof(IRepository<>))]
public sealed class Repository<T> : IRepository<T>;
```

---

## Phase 7: Tests

Add focused tests for:

- generated factory registration
- runtime-scanned factory registration
- invalid factory return type diagnostics
- invalid factory signature diagnostics
- descriptor provider registration
- invalid descriptor provider diagnostics
- cleaner open generic attribute constructors

Suggested test names:

```csharp
GeneratedFactoryRegistrationAddsDescriptor()
RuntimeFactoryRegistrationAddsDescriptor()
InvalidFactoryReturnTypeReportsDiagnostic()
InvalidFactorySignatureReportsDiagnostic()
DescriptorProviderAddsDescriptors()
InvalidDescriptorProviderReportsDiagnostic()
ScopedTypeConstructorRegistersOpenGeneric()
```

---

## Suggested Order Of Work

1. Add cleaner `Type` constructors for `Scoped`, `Singleton`, `Transient`, and `SingletonPostInit`.
2. Add source-generator support for factory method attributes.
3. Add runtime scanning support for factory method attributes.
4. Add diagnostics for invalid factory methods.
5. Add descriptor-provider support.
6. Update `README.md`.
7. Add tests.
8. Run full validation.

---

## Success Criteria

Specky should cover every registration concept from the Microsoft DI basics page except service-provider lifecycle operations.

It should support:

```csharp
// Type registration
[Singleton<IConsole>]
public sealed class DefaultConsole : IConsole;

// Concrete registration
[Singleton]
public sealed class FarewellService;

// Factory registration
[SingletonFactory<IConsole>]
public static IConsole CreateConsole()
    => new DefaultConsole { IsEnabled = true };

// Runtime scanning fallback
builder.Services.AddSpecks<Program>();

// Generator-first path
builder.Services.AddTripleG3Specky();
```

At that point, Specky would cover the practical registration-side use cases in the Microsoft DI basics article while still leaving provider building and service resolution to .NET DI.
