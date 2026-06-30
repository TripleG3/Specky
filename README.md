# TripleG3.Specky

Simple, attribute‑driven service registration for `Microsoft.Extensions.DependencyInjection` (.NET 6+).

Stop writing repetitive `AddScoped<,>()`, `AddSingleton<>()`, and boilerplate configuration glue. Annotate your types (or list them in lightweight configuration interfaces) and let Specky do the rest.

---
## ✨ Features

* Tiny API – one call: `builder.Services.AddSpecks<Program>();`
* Attribute registration for classes, properties, fields, and methods (in configuration interfaces)
* Interface configuration model (declare everything in one place – zero changes to `Program.cs` later)
* Options filtering (`[SpeckyConfiguration(Option="Prod")]` + `opts.AddOption("Prod")`)
* Post‑initialization singleton activation (`[SingletonPostInit]` + `app.UseSpeckyPostSpecks()`)
* Multi‑assembly scanning (`opts.AddAssemblies(...)`)
* Configurations‑only mode (skip full assembly scan)
* Duplicate registration prevention & internal caching for speed
* Multi‑target builds: `net6.0; net8.0; net9.0`

---
## 🚀 Quick Start

1. Install the package (placeholder id, adjust if different):

    ```bash
    dotnet add package TripleG3.Specky
    ```

2. Annotate a class:

    ```csharp
    using TripleG3.Specky;

    [Scoped]
    public class TimeProvider { public DateTime Now() => DateTime.UtcNow; }
    ```

3. Register in `Program.cs` (or wherever you build the container):

    ```csharp
    builder.Services.AddSpecks<Program>();
    ```

4. Inject & use:

    ```csharp
    public class HomeController(TimeProvider clock) : Controller
    {
         public IActionResult Index() => Content(clock.Now().ToString());
    }
    ```

That’s it – no explicit `AddScoped<TimeProvider>()` needed.

---
## 🔖 Attribute Reference

| Attribute | Effect | Service Type | Lifetime |
|-----------|--------|--------------|----------|
| `[Singleton]` | Self registration | Decorated type | Singleton |
| `[Scoped]` | Self registration | Decorated type | Scoped |
| `[Transient]` | Self registration | Decorated type | Transient |
| `[Singleton<T>]` | Registers `<T>` implemented by decorated type | `T` | Singleton |
| `[Scoped<T>]` | Registers `<T>` implemented by decorated type | `T` | Scoped |
| `[Transient<T>]` | Registers `<T>` implemented by decorated type | `T` | Transient |
| `[SingletonPostInit]` / `[SingletonPostInit<T>]` | Like Singleton + eager resolve after build | Self / `T` | Singleton |

Apply multiple attributes if you want one class to fulfill several interfaces:

```csharp
[Scoped<IFoo>]
[Scoped<IBar>]
public class FooBar : IFoo, IBar { }
```

---
## 🧩 Configuration Interfaces

Prefer a central declaration over scattered attributes? Define an interface, mark it with `[SpeckyConfiguration]`, and list members whose types you want registered.

```csharp
using TripleG3.Specky;

[SpeckyConfiguration]
public interface ICoreServices
{
    [Singleton]     AppStartup Startup;          // registers AppStartup as singleton
    [Scoped<IRepo>] EfRepository Repo;           // registers IRepo -> EfRepository scoped
    [Transient<ILogger>] ConsoleLogger Logger;  // registers ILogger -> ConsoleLogger transient
    [Scoped<IWorker>] Worker Worker;            // registers IWorker -> Worker scoped
}
```

Activate them:
 
```csharp
builder.Services.AddSpecks<Program>(opts =>
{
    opts.UseConfigurationsOnly = true; // discover all [SpeckyConfiguration] interfaces in assemblies
});
```
or explicitly:
 
```csharp
builder.Services.AddSpecks<Program>(opts =>
{
    opts.AddConfiguration<ICoreServices>();
});
```

### Methods as Declarations

You can use methods (return type becomes the implementation):
 
```csharp
[SpeckyConfiguration]
public interface IMethodConfig
{
    [Singleton] Startup BuildStartup(); // Registers Startup as singleton
}
```
`void` return methods are ignored (and will trigger an error if attributed).

 
## 🎛 Options Filtering

Target environment‑specific sets:
 
```csharp
[SpeckyConfiguration(Option="Prod")] public interface IProdConfig { [Singleton] ProdService S; }
[SpeckyConfiguration(Option="Dev")]  public interface IDevConfig  { [Singleton] DevService  S; }

builder.Services.AddSpecks<Program>(opts =>
{
    opts.AddConfiguration<IProdConfig>();
    opts.AddConfiguration<IDevConfig>();
    opts.AddOption("Dev"); // Only IDevConfig applied
});
```

Add multiple options for additive inclusion.

 
## 🔄 Multi‑Assembly Scanning

```csharp
builder.Services.AddSpecks<Program>(opts =>
{
    opts.AddAssembly<Program>();
    opts.AddAssemblies(typeof(SomeSharedType).Assembly, typeof(OtherLib.Marker).Assembly);
});
```
If you don’t specify any, the entry assembly is used.

 
## ⚡ Post‑Initialization

Need a singleton constructed immediately after building the app (e.g., warm caches)?
 
```csharp
[SingletonPostInit]
public class WarmupCache { public WarmupCache(IMyDataSource ds) { /* pre-load */ } }

builder.Services.AddSpecks<Program>();
app.UseSpeckyPostSpecks(); // triggers resolution of all post-init singletons
```

 
## 🛡 Duplicate Protection & Performance

Specky maintains a hash set of existing descriptors to avoid duplicate identical registrations and caches discovered attributes to minimize reflection overhead.

 
## 🧪 Full Minimal Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSpecks<Program>();
var app = builder.Build();
app.MapGet("/time", (Clock c) => c.Now());
app.Run();

// Clock.cs
using TripleG3.Specky;
[Singleton] public class Clock { public DateTime Now() => DateTime.UtcNow; }
```

 
## ❗ Common Errors

| Message (truncated) | Cause | Fix |
|---------------------|-------|-----|
| "cannot be assigned" | Implementation doesn’t implement service interface | Make class implement interface or change attribute generic |
| "expected to inject with configurations but none was found" | `UseConfigurationsOnly` true but no config interfaces discovered | Add / mark config interface |
| "methods cannot return void" | Attributed config method returns `void` | Return a concrete type |

 
## 🧭 Limitations (Current)

* Multiple service interfaces require multiple attributes
* Open generics not yet supported
* Source generator not (yet) provided

### Roadmap

* Open generic support
* Multi‑service attribute (e.g., `[Scoped(typeof(IFoo), typeof(IBar))]`)
* Optional source generator for AOT / zero reflection

 
## 🤝 Contributing

Issues and PRs welcome. Keep it lightweight and reflection‑lean.

 
## 📄 License

See `LICENSE` in repository.

---
Happy injecting! 🎯
