namespace Net.NowhereAtAll.Xfty.Internal;

/// <summary>
/// Stands in for <c>System.Random.Shared</c> (.NET 6+), which netstandard2.0
/// doesn't have - <see cref="Random.Shared"/> directly on net8.0/net10.0, a
/// per-thread <see cref="Random"/> instance on netstandard2.0 (matching
/// Random.Shared's actual thread-safety guarantee - plain <see cref="Random"/>
/// itself isn't thread-safe on the older runtimes netstandard2.0 targets, not
/// just its call syntax). One name, used unconditionally everywhere else in
/// this codebase - the #if lives here, once, not at every call site.
///
/// Split into its own file (separate from the netstandard2.0-only
/// DictionaryCompatExtensions/EnumerableCompatExtensions in
/// CollectionCompatExtensions.cs) so another package with the same gap can
/// link this one file directly via `&lt;Compile Include&gt;` - see
/// Xfty.VectorDatabases.csproj - without also pulling in polyfills it
/// doesn't use. One physical file, compiled again into each assembly that
/// links it; still internal to each, so no cross-assembly coupling.
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