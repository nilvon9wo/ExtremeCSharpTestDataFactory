using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.NetStandardCompat.Test;

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
    [SuppressMessage("Performance", "HLQ005:Avoid Single() and SingleOrDefault()", Justification = "<Pending>")]
    public void DefaultProviderLookup_KeysFor_MatchesTheGivenRecordsRegisteredKey()
    {
        // Arrange
        DefaultProviderLookup lookup = new();

        // Act
        ISet<Lookup.ILookupKey> keys = lookup.KeysFor(new Account());

        // Assert - ToHashSet's polyfill produced a real set, correctly populated with every registered key,
        // for ProviderLookups.KeysFor to then filter down to the one matching Account
        _ = Assert.Single(keys);
        Assert.Contains(Lookup.LookupKey.Get<Account>(), keys);
    }

    // BlankInstances.Of -> FormatterServices.GetUninitializedObject (netstandard2.0 branch) --

    [Fact]
    public async Task Supply_ForARecordTypeWithNoParameterlessConstructor_BuildsAndFillsItViaTheDownlevelUninitializedObjectPath()
    {
        // Arrange - Voucher has only a parameterized constructor, so BlankInstances.Of must fall back
        // to FormatterServices.GetUninitializedObject, the netstandard2.0-only branch #if'd out on net8.0+
        IProviderLookup lookup = ProviderLookups.Of(new Dictionary<Lookup.ILookupKey, IRecordProvider>
        {
            [Lookup.LookupKey.Get<Voucher>()] = new VoucherProvider(),
        });

        // Act
        Voucher generated = (Voucher)await new RecordProvider(typeof(Voucher), lookup)
            .SetInsertMode(InsertMode.Mock)
            .Supply().ConfigureAwait(true);

        // Assert - the uninitialized instance was populated by the normal reflection value passes
        Assert.Equal("Gift", generated.Kind);
        Assert.StartsWith("mock-", generated.Code!);
    }
}

file sealed class Voucher
{
    // Only a parameterized constructor - so there is no public parameterless one for Activator to use.
    public Voucher(string kind) => this.Kind = kind;

    public string? Code { get; set; }

    public string? Kind { get; set; }
}

file sealed class VoucherProvider : IRecordProvider
{
    private MasterTemplate template { get; } = new MasterTemplate<Voucher>(x => x.Code)
        .Put(x => x.Kind, new LiteralExpression("Gift"));

    public PropertyInfo PrimaryTargetField => this.template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.template, templateRecords);
}
