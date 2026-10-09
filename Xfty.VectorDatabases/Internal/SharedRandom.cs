namespace Net.NowhereAtAll.Xfty.VectorDatabases.Internal;

/// <summary>
/// Stands in for <c>System.Random.Shared</c> (.NET 6+), which netstandard2.0
/// doesn't have - <c>Random.Shared</c> directly on net8.0/net10.0, a
/// per-thread <see cref="Random"/> instance on netstandard2.0 (matching
/// Random.Shared's actual thread-safety guarantee - plain <see cref="Random"/>
/// itself isn't thread-safe on the older runtimes netstandard2.0 targets, not
/// just its call syntax). One name, used unconditionally everywhere else in
/// this package - the #if lives here, once, not at every call site.
///
/// Lives in this package, not core Xfty: this package is its only user since
/// UniqueAcrossRunsExpression moved off it, and a type core never calls is
/// dead code there.
/// </summary>
internal static class SharedRandom
{
#if NETSTANDARD2_0
    // Not an auto property (IDE0032 doesn't apply): [ThreadStatic] must target
    // an actual static field, and the getter's lazy-init (??=) has no
    // auto-property equivalent - the two together are the whole point.
#pragma warning disable IDE0032
    [ThreadStatic]
    private static Random? s_threadInstance;
#pragma warning restore IDE0032

    public static Random Instance => s_threadInstance ??= new Random();
#else
    public static Random Instance => Random.Shared;
#endif
}