# Specky7

Attribute-driven helper to register services with `Microsoft.Extensions.DependencyInjection` using simple attributes. Supports .NET 6+ (multi-targets net6.0, net8.0, net9.0).

## Required at builder.Services to add Specks (may include other assemblies)

`builder.Services.AddSpecks<T>();` `T` helps Specky infer the assembly to use so `T` should be part of the assembly you want scanned.
If you want to scan multiple assemblies you can with options:
```csharp
builder.Services.AddSpecks(opts =>
{
    opts.AddAssembly<App>();
    opts.AddAssembly<MyCustomPackage>();
    opts.AddAssembly<MyCustomDll>();
});
```

You may also provide interfaces (instructions later in this read me) that will be used for the types to inject.
Note: You do not need to specify an assembly if you are using configurations because the types will be inferred at that time.

```csharp
builder.Services.AddSpecks(opts =>
{
    opts.AddConfiguration<IProdConfiguration>();
    opts.AddConfiguration<IUatConfiguration>();
    opts.AddConfiguration<IDevConfiguration>();
    opts.AddOption("Dev");
});
```

If you want to speck out your types for auto injection you can do so by placing a speck attribute on the type. Specky will locate the type and inject it automatically.
## Examples

If you're familiar with .NET's built in injection then the naming here should be straight forward.

### Transient, Scoped, or Singleton attributes inject the type as the implementation

`[Transient]` will inject the type as transient.

`[Scoped]` will inject the type as scoped.

`[Singleton]` will inject the type as singleton.

```csharp
[Scoped]
public class MyClass { }
```
> Is the same as:
```csharp
public class MyClass { }

builder.Services.AddScoped<MyClass>();
```

### TransientAs, ScopedAs, or SingletonAs attributes inject the implementation as the interface provided in the attribute

`[Transient<T>]` will inject the service type `T` to be implemented by the decorated class. The decorated class must implement / inherit from `T`.

The same logic is applied for `[Scoped<T>]` and `[Singleton<T>]`

> Example:
```csharp
public interface IWorker { }

[Scoped<IWorker>]
public class Worker : IWorker { }
```
> Is the same as:
```csharp
public interface IWorker { }

public class Worker : IWorker { }

builder.Services.AddScoped<IWorker, Worker>();
```

## Transient
Transient will inject a new instance for every request.

`[Transient] public class MyClass { }` or `[Transient<IMyClass>] public class MyClass : IMyClass { }`

## Scoped
Scoped will inject a new instance for every session, typically, in a web app for example, this is each time the Http connection is reset.

`[Scoped] public class MyClass { }` or `[Scoped<IMyClass>] public class MyClass : IMyClass { }`

## Singleton
Singleton will inject a the same instance for the lifetime of the application.

`[Singleton] public class MyClass { }` or `[Singleton<IMyClass>] public class MyClass : IMyClass { }`

# Using Configurations (Interface-based configuration)
With Specky you can create an interface that declares the services you want registered. The property/field *type* (or a generic attribute argument) determines the service & implementation types.

To use configurations you will need to first make any `interface` and then add the `SpeckyConfigurationAttribute` to that `interface`.

Next add the properties or methods with the proper injection attribute.

Notes:
* Property / method names do not matter.
* Interfaces cannot actually contain fields; only properties & methods are meaningful. (Field scanning remains for potential future class-based configs.)
* A configuration method must return the type to register (it cannot be `void`).
```csharp
[SpeckyConfiguration]
interface IExampleConfiguration
{
    [Singleton] Reader reader;               // Makes Reader the service type and implementation as singleton
    [Transient<ILogger>] TraceLogger logger; // Makes ILogger the service type and TraceLogger the implementation
    [Scoped<IWorker>] Worker worker;         // Makes IWorker the service type and Worker the implementation
}
```

# Full example of using Specky configurations:
### Note you do not have to put attributes on the types themselves or modify the StartUp.cs, program.cs, or any files where you're declaring DI.

Program.cs

    using Microsoft.Extensions.Hosting;
    using Specky7;

    using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) => services.AddSpecks<StartUp>(opts => opts.UseConfigurationsOnly = true))
        .Build();

    ((StartUp)host.Services.GetService(typeof(StartUp))!).Start();

### StartUp.cs

    public class StartUp
    {
        private readonly IWorker worker;
        private readonly ILogger loger;

        public StartUp(IWorker worker, ILogger logger)
        {
            this.worker = worker;
            this.logger = logger;
        }

        public void Start()
        {
            logger.Log("Start");
            worker.DoWork(() => Console.WriteLine("App has started"));
            logger.Log("End");
        }
    }

### Logging interface and implementations:

    public interface ILogger
    {
        void Log(string message);
    }

    public class TraceLogger : ILogger
    {
        public void Log(string message) => Trace.WriteLine(message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }

### Worker interface and implementation:

    public interface IWorker
    {
        void DoWork(Action action);
    }

    public class Worker : IWorker
    {
        public void DoWork(Action action) => action.Invoke();
    }

### The Specky configuration file
```csharp
/* 
 * Note: Specky configurations do not get injected. 
 * The interface is used as a reference for Specky to locate and inject types.
 * You can use properties or methods. (Fields are ignored for interfaces.)
 */
[SpeckyConfiguration]
interface IExampleConfiguration
{
    [Singleton<ILogger>] ConsoleLogger logger;
    [Singleton] StartUp startup;
    [Singleton<IWorker>] Worker worker;
}
```

### Pseudo Example (Options Filtering)
```csharp
builder.Services.AddSpecks<App>(opts =>
{
   opts.AddConfiguration<IFooConfiguration>();
   opts.AddOption("Ok");
});
```

```csharp
[SpeckyConfiguration(Option = "Ok")]
interface IFooConfiguration
{
    [Singleton] Foo foo;
    [Scoped<IWorker>] Worker worker;
}
```

## Configurations Only Mode
If you want to skip attribute scanning on classes and only use configuration interfaces, set `UseConfigurationsOnly`:

```csharp
builder.Services.AddSpecks<Program>(opts =>
{
    opts.UseConfigurationsOnly = true; // auto-discovers all interfaces with [SpeckyConfiguration]
});
```

Or add them explicitly:
```csharp
builder.Services.AddSpecks<Program>(opts =>
{
    opts.UseConfigurationsOnly = true;
    opts.AddConfiguration<IMyConfiguration>();
});
```

## Post-Initialization
Mark a singleton with `[SingletonPostInit]` (or generic variant) to force resolution immediately after the app builds using `app.UseSpeckyPostSpecks();` This simply calls `GetService` to trigger construction.

## Duplicate Registrations
Specky now guards against identical duplicates (same service, implementation, and lifetime) to avoid accidental multi-registration.

## Safety & Limitations
* If a property type is an interface and you use a non-generic attribute (e.g. `[Singleton]`), registration will fail because the implementation cannot be an interface.
* Multiple service interfaces for one implementation currently require multiple attributes.
* Open generics are not yet supported.

## Roadmap
* Optional source generator for compile-time registration
* Open generic support
* Multi-service single attribute shorthand

---
Contributions & issues welcome.
