#if NETSTANDARD2_0
namespace Net.NowhereAtAll.Xfty.Internal;

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

/// <summary>
/// See <see cref="DictionaryCompatExtensions"/> - same reasoning, for <c>IEnumerable&lt;T&gt;.ToHashSet()</c>.
/// </summary>
internal static class EnumerableCompatExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => [.. source];
}
#endif