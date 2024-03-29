# Specky7
Lightweight wrapper to assist injection, using attributes, using the built in DI model integrated in .NET 6 and up.

## Required at builder.Services to add Specks (may include other assemblies)

`builder.Services.AddSpecks<T>();` `T` helps Specky infer the assemlbly to use so `T` should be part of the assembly you want scanned.
If you want to scan multiple assemblies you can with options
```
serviceProvider.AddSpecks(opts =>
{
    opts.AddAssembly<App>();
    opts.AddAssembly<MyCustomPackage>();
    opts.AddAssembly<MyCustomDll>();
});
```

You may also provide interfaces (instructions later in this read me) that will be used for the types to inject.
Note: You do not need to specify an assembly if you are using configurations because the types will be infered at that time.

```
serviceProvider.AddSpecks(opts =>
{
    opts.AddConfiguration<IProdConfiguration>();
    opts.AddConfiguration<IUatConfiguration>();
    opts.AddConfiguration<IDevConfiguration>();
    opts.AddOptions("Dev");
});
```

If you want to speck out your types for auto injection you can do so by placing a speck attribute on the type.  Specky will locate the type and inject it automatically.
## Examples (If you're familiar with .NET's built in injection then the naming here should be straight forward.)

### Transient, Scoped, or Singleton attributes inject the type as the implementation. 

`[Transient]` will inject the type as transient.

`[Scoped]` will inject the type as scoped.

`[Singleton]` will inject the type as singleton.

```
[Scoped]
public class MyClass { }
```
> Is the same as:
```
public class MyClass { }

builder.Services.AddScoped<MyClass>();
```

### TransientAs, ScopedAs, or SingletonAs attributes inject the implementation as the interface provided in the attribute.

`[Transient<T>)]` will inject the service type `TransientAttribute<T>.Type` to be implemented by the `Type` the `TransientAttribute<T>` is given to. `Type` must inherit from the `TransientAttribute<T>.Type`

The same logic is applied for `[Scoped<T>]` and `[Singleton<T>]`

> Example:
```
public interface IWorker

[Scoped<IWorker>]
public class Worker : IWorker
```
> Is the same as:
```
public interface IWorker

public class Worker : IWorker

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

# Using Configurations
With Specky you can create an interface for injecting types in one or more locations.

To use configurations you will need to first make any `interface` and then add the `SpeckyConfigurationAttribute` to that `interface`.

Next add the properties or methods with the proper injection attribute.

Note: The property, field, and method names do not matter. Specky only looks for the type or return type and the attribute applied. You can also combine an existing interface with auto injection this way.
```
[SpeckyConfiguration]
interface IExampleConfiguration
{
    [Singleton] Reader reader;               //Makes Reader the service type and the implementation type as a singleton
    [Transient<ILogger>] TraceLogger logger; //Makes ILogger the service type and TraceLogger the implementation type as transient
    [Scoped<IWorker>] Worker worker;         //Makes IWorker the service type and Worker the implementation type as scoped
}
```

# Full example of using Specky configurations:
### Note you do not have to put attributes on the types themselves or modify the StartUp.cs, program.cs, or any files where you're declaring DI.

Program.cs

    using Microsoft.Extensions.Hosting;
    using Specky7;

    using IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) => services.AddSpecks<StartUp>(opts => opts.UseConfiguratons = true))
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

### The specky configuration file:   
    /* 
     * Note: Specky configurations do not get injected. 
     * The interface is used as a reference for Specky to locate and inject types.
     * You can use properties, fields, or methods. 
     */
    [SpeckyConfiguration]
    interface IExampleConfiguration
    {
        [Singleton<ILogger>] ConsoleLogger logger;
        [Singleton] StartUp startup;
        [Singleton<IWorker>] Worker worker;
    }

### Psuedo Example
```
builder.Services.AddSpecks<App>(opts =>
{
   opts.AddConfiguration<IFooConfiguration>();
   opts.AddOption("Ok");
});
```
```
[SpeckyConfiguration(Option = "Ok")]
interface IFooConfiguration
{
    [Singleton] Foo foo;
    [Scoped<IWorker>] Worker worker;
}
```
