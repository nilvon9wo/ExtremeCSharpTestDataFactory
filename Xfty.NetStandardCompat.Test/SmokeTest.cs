using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.NetStandardCompat.Test;

/// <summary>
/// Proves the three netstandard2.0-only compatibility polyfills in
/// Xfty/Internal/NetStandardCompat.cs (GetValueOrDefault, ToHashSet,
/// SharedRandom) actually run correctly under a real down-level runtime -
/// not just compile. netstandard2.0 isn't itself runnable (a contract, not a
/// platform), so net472 - the one real, already-installed-locally runtime
/// that implements it - is what actually executes them here, through public
/// XFTY behavior that happens to depend on each one, rather than reaching
/// into the internal polyfill types directly.
/// </summary>
public class SmokeTest
{
    // UniqueStringOfLengthExpression.Get() -> Dictionary.GetValueOrDefault ----

    [Fact]
    public void UniqueStringOfLengthExpression_ProducesASequenceOfDistinctValues()
    {
        // Arrange
        UniqueStringOfLengthExpression expression = new(3);

        // Act
        object first = expression.Get();
        object second = expression.Get();
        object third = expression.Get();

        // Assert - GetValueOrDefault's polyfill correctly returns 0 the first time, then the real running count
        // (the counter's least-significant digit is the first character, so it advances left to right: AAA, BAA, CAA)
        Assert.Equal("AAA", first);
        Assert.Equal("BAA", second);
        Assert.Equal("CAA", third);
    }

    // UniqueAcrossRunsExpression.Get() -> SharedRandom.Instance ---------------

    [Fact]
    public void UniqueAcrossRunsExpression_ProducesANonEmptyToken()
    {
        // Arrange
        UniqueAcrossRunsExpression expression = new("prefix-", "-suffix");

        // Act
        object result = expression.Get();

        // Assert - SharedRandom.Instance.Next() ran without throwing and fed a real value into the token
        string text = Assert.IsType<string>(result);
        Assert.StartsWith("prefix-", text);
        Assert.EndsWith("-suffix", text);
    }

    // DefaultProviderLookup.KeysFor(...) -> IEnumerable<T>.ToHashSet() -------

    [Fact]
    public void DefaultProviderLookup_KeysFor_MatchesTheGivenRecordsRegisteredKey()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        ISet<Lookup.ILookupKey> keys = lookup.KeysFor(new Account());

        // Assert - ToHashSet's polyfill produced a real set, correctly populated with every registered key,
        // for ProviderLookups.KeysFor to then filter down to the one matching Account
        _ = Assert.Single(keys);
        Assert.Contains(Lookup.LookupKey.Get(typeof(Account)), keys);
    }
}
