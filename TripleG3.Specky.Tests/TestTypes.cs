using Microsoft.Extensions.DependencyInjection;
using TripleG3.Specky;

namespace TripleG3.Specky.Tests;
internal class A_Foo : IFooId, IFooTime
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}

[Speck<IFooTime>]
[Scoped<IFooId>]
internal class B_Foo : IFooId, IFooTime
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
}
internal class A_FooId : IFooId
{
    public int Id { get; set; }
}
internal class B_FooId : IFooId
{
    public int Id { get; set; }
}

[Speck]
internal class A_FooTime : IFooId
{
    public int Id { get; set; }
}

[Speck(ServiceLifetime.Transient)]
internal class B_FooTime : IFooId
{
    public int Id { get; set; }
}

// Keyed test types
[SingletonKeyed<IFooTime>("TimeKey1")]
internal class Keyed_Time_Singleton : IFooTime
{
    public DateTime Time { get; set; }
}

[ScopedKeyed<IFooId>("IdKeyScoped")]
internal class Keyed_Id_Scoped : IFooId
{
    public int Id { get; set; }
}

[TransientKeyed<IFooId>("IdKeyTransient")]
internal class Keyed_Id_Transient : IFooId
{
    public int Id { get; set; }
}