# 🎯 TripleG3.Specky

> **Specky** = attribute-powered dependency injection for `.NET 10`, with compile-time generation first and runtime scanning when you actually want it.

If your DI setup is starting to read like a tax document written by `IServiceCollection`, Specky can clean that up.

Tag the type. Call one method. Move on with your life.

 ---

 ## ✨ Why Specky?

 **Fast to author.**
 Add an attribute. Done.

**Clean startup.**
Use `AddTripleG3Specky()` instead of hand-writing a decorative brick wall of `AddScoped`, `AddSingleton`, and `AddTransient` calls.

 **Generator-first.**
 Preferred for `.NET 10`: compile-time registrations, less runtime reflection, happier startup path.

**Flexible.**
Need runtime scanning? Specky still supports it without acting personally offended.

 **Modern.**
 Supports:

 - standard registrations
 - multi-service registrations
 - keyed registrations
 - post-init singletons
 - first-pass open generic registrations
 - configuration-interface patterns

In short: **less ceremony, more intent, fewer regret-scrolls in `Program.cs`**.

 ---

 ## 🚀 Preferred

 ```csharp
 builder.Services.AddTripleG3Specky();
 ```

That’s the happy path.

Generator on. Boilerplate off. Coffee still optional.

 ---

 ## 🧪 Fallback

 ```csharp
 builder.Services.AddSpecks<Program>();
 ```

When you want runtime scanning, plugin-ish behavior, or a carefully supervised amount of chaos.

 ---

 ## 🔥 Quick Win

 ```csharp
 using TripleG3.Specky;

 [Scoped<IMailer>]
 public sealed class Mailer : IMailer;
 ```

 ```csharp
 builder.Services.AddTripleG3Specky();
 ```

No registration wall. No repetitive glue. No tiny sadness in the startup pipeline.

 ---

 ## 🧭 Index

 - [Preferred](#-preferred)
 - [Fallback](#-fallback)
 - [Scoped](#-scoped)
 - [Singleton](#-singleton)
 - [Transient](#-transient)
 - [Self](#-self)
 - [Multi](#-multi)
 - [Keyed](#-keyed)
 - [PostInit](#-postinit)
 - [Generic](#-generic)
 - [Config](#-config)
 - [Options](#-options)
 - [Generated](#-generated)
 - [Errors](#-errors)
 - [Limits](#-limits)

 ---

 ## 🟦 Scoped

 ```csharp
 [Scoped<IClock>]
 public sealed class ClockService : IClock;
 ```

 **Per scope.**

 ---

 ## 🟨 Singleton

 ```csharp
 [Singleton<ICache>]
 public sealed class CacheService : ICache;
 ```

 **One forever.**

 ---

 ## 🟪 Transient

 ```csharp
 [Transient<IFormatter>]
 public sealed class Formatter : IFormatter;
 ```

 **Fresh every time.**

 ---

 ## 🟩 Self

 ```csharp
 [Scoped]
 public sealed class TimeProvider;
 ```

 **Register concrete as itself.**

 ---

 ## 🧩 Multi

 ```csharp
 [MultiScoped(typeof(IFoo), typeof(IBar))]
 public sealed class FooBar : IFoo, IBar;
 ```

 **One type. Multiple hats.**

 ---

 ## 🗝 Keyed

 ```csharp
 [ScopedKeyed<IParser>("json")]
 public sealed class JsonParser : IParser;
 ```

 **Named flavor.**

 ---

 ## ⚡ PostInit

 ```csharp
 [SingletonPostInit<IWarmupTask>]
 public sealed class WarmupTask : IWarmupTask;
 ```

 ```csharp
 builder.Services.AddSpecks<Program>();
 app.UseSpeckyPostSpecks();
 ```

 **Wake it up immediately.**

 ---

 ## 🧠 Generic

 ```csharp
 [MultiServiceSpeck(ServiceLifetime.Scoped, typeof(IRepository<>), typeof(IRepository<>))]
 public sealed class Repository<T> : IRepository<T>;
 ```

**Open generics, first-pass style.**

 Notes:

- conservative by design
- generic arity must match
- implementation must actually satisfy the open generic contract
- generator diagnostics help catch invalid mappings early

 ---

 ## 🧱 Config

 ```csharp
 [SpeckyConfiguration]
 public interface ICoreServices
 {
     [Singleton] AppState State { get; }
     [Scoped<IClock>] ClockService Clock { get; }
 }
 ```

 ```csharp
 builder.Services.AddSpecks<Program>(opts =>
 {
     opts.AddConfiguration<ICoreServices>();
 });
 ```

 **Centralized declarations.**

 ---

 ## 🎛 Options

 ```csharp
 [SpeckyConfiguration("Prod")]
 public interface IProdServices
 {
     [Singleton] ProdOnlyService Service { get; }
 }
 ```

 ```csharp
 builder.Services.AddSpecks<Program>(opts =>
 {
     opts.AddConfiguration<IProdServices>();
     opts.AddOption("Prod");
 });
 ```

 **Selective loading.**

 ---

 ## 🏭 Generated

 ```csharp
 builder.Services.AddTripleG3SpeckyGenerated();
 ```

 or:

 ```csharp
 builder.Services.AddTripleG3Specky();
 ```

**Compile-time registrations. Preferred.**

This is the main event.

 ---

 ## 🔄 Scan

 ```csharp
 builder.Services.AddSpecks<Program>(opts =>
 {
     opts.AddAssembly<Program>();
     opts.AddAssemblies(typeof(SharedMarker).Assembly);
 });
 ```

 **Runtime discovery.**

 ---

 ## 🧪 Minimal

 ```csharp
 using TripleG3.Specky;

 [Singleton<IClock>]
 public sealed class Clock : IClock;

 builder.Services.AddTripleG3Specky();
 ```

 **Small. Sharp. Done.**

 ---

 ## 🚫 Errors

 ### Assignability

 ```text
 cannot be assigned
 ```

Implementation does not satisfy the service contract.

The generator/runtime validation will complain before your container has the chance to do interpretive dance.

 ### Void

 ```text
 methods cannot return void
 ```

 Attributed configuration methods must return a real implementation type.

 ### Missing config

 ```text
 expected to inject with configurations but none was found
 ```

 `UseConfigurationsOnly = true` found nothing usable.

 ### Generic mismatch

 ```text
 generic arity does not match
 ```

Your open generic service and implementation definitions do not line up.

This usually means arity mismatch or a contract the implementation does not actually satisfy.

 ---

 ## 📌 Limits

- open generic support is intentionally conservative
- source generation is first-pass, but already useful and preferred
- compile-time diagnostics exist, but richer analyzer coverage can still improve
- runtime scanning still exists because sometimes reality is rude

 ---

 ## 🛣 Roadmap

 - more polished open generic ergonomics
 - optional deeper AOT-focused workflow improvements

 ---

 ## 🤝 Contributing

 Small, sharp PRs welcome.

 If it removes boilerplate, improves correctness, or saves future-you from reading a 200-line `Program.cs`, it is probably in scope.

 ---

 ## 📄 License

 See `LICENSE`.

 ---

 **TripleG3.Specky**

 Because typing the same DI registration 47 times is not a personality trait.
