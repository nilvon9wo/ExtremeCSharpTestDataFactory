namespace Net.Nowhereatall.Xfty.Internal;

/// <summary>
/// Stands in for <c>System.Random.Shared</c> (.NET 6+), which netstandard2.0
/// doesn't have - <see cref="Random.Shared"/> directly on net8.0/net10.0, a
/// per-thread <see cref="Random"/> instance on netstandard2.0 (matching
/// Random.Shared's actual thread-safety guarantee - plain <see cref="Random"/>
/// itself isn't thread-safe on the older runtimes netstandard2.0 targets, not
/// just its call syntax). One name, used unconditionally everywhere else in
/// this codebase - the #if lives here, once, not at every call site.
/// </summary>
internal static class SharedRandom
{
#if NETSTANDARD2_0
    // Not an auto property (IDE0032 doesn't apply): [ThreadStatic] must target
    // an actual static field, and the getter's lazy-init (??=) has no
    // auto-property equivalent - the two together are the whole point.
#pragma warning disable IDE0032
    [ThreadStatic]
    private static Random? threadInstance;
#pragma warning restore IDE0032

    public static Random Instance => threadInstance ??= new Random();
#else
    public static Random Instance => Random.Shared;
#endif
}

#if NETSTANDARD2_0
/// <summary>
/// Polyfills for netstandard2.0's own gaps against BCL members added later
/// (GetValueOrDefault/ToHashSet, both .NET Core 2.0+) - exists on
/// netstandard2.0 only, via the #if above, so it never collides with the
/// real members on net8.0/net10.0. Each polyfill preserves the same call
/// syntax the rest of the codebase already uses, so no other file needs to
/// change to support netstandard2.0.
/// </summary>
internal static class DictionaryCompatExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out TValue? value) ? value : default;
}

/// <summary>See <see cref="DictionaryCompatExtensions"/> - same reasoning, for <c>IEnumerable&lt;T&gt;.ToHashSet()</c>.</summary>
internal static class EnumerableCompatExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => [.. source];
}
#endif
