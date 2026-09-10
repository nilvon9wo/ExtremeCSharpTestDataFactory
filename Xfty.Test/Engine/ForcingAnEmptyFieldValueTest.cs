using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// The one thing an <em>override template</em> can't say: "make this field
/// its empty value" - <c>0</c> / <c>false</c> / <c>default</c> for a value
/// type, <c>null</c> for a reference type or <c>Nullable&lt;T&gt;</c> - when
/// the Provider has a default for it. Reflection can't tell "I set it to the
/// empty value on purpose" from "I never touched it", so the Provider default
/// wins either way.
///
/// The fix is on the per-call provider, which <em>is</em> tracked:
/// <c>provider[x =&gt; x.Field] = value</c> (or <c>.Put(...)</c>) forces the
/// empty value, or <c>RemoveFromMasterTemplate(x =&gt; x.Field)</c> drops the
/// Provider default for that call. A Provider author's own option is to not
/// default a field tests routinely need to blank.
/// </summary>
public class ForcingAnEmptyFieldValueTest
{
    // The limitation --------------------------------------------------

    [Fact]
    public async Task Supply_WhenAnOverrideTemplateSetsAReferenceFieldToNull_TheProviderDefaultStillWins()
    {
        // Arrange - Label is null on the template; the Provider defaults it to "stock"
        RecordProvider provider = new RecordProvider(typeof(Crate), CrateLookup())
            .SetOverrideTemplate(new Crate { Reference = "c-1", Label = null })
            .SetInsertMode(InsertMode.Never);

        // Act
        Crate crate = (Crate)await provider.Supply().ConfigureAwait(true);

        // Assert - null is indistinguishable from unset, so the default fills it
        Assert.Equal("stock", crate.Label);
    }

    [Fact]
    public async Task Supply_WhenAnOverrideTemplateSetsAValueTypeFieldToDefault_TheProviderDefaultStillWins()
    {
        // Arrange - Count is 0 on the template; the Provider defaults it to 5
        RecordProvider provider = new RecordProvider(typeof(Crate), CrateLookup())
            .SetOverrideTemplate(new Crate { Reference = "c-1", Count = 0 })
            .SetInsertMode(InsertMode.Never);

        // Act
        Crate crate = (Crate)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(5, crate.Count);
    }

    // Fix A: force it through the tracked per-call configuration -----

    [Fact]
    public async Task Supply_WhenTheCallConfiguresTheReferenceFieldToNull_KeepsItNull()
    {
        // Arrange - the tracked fluent form, unlike an override template, wins
        RecordProvider<Crate> provider = new(CrateLookup())
        {
            [x => x.Label] = null,
        };

        // Act
        Crate crate = await provider.SetInsertMode(InsertMode.Never).Supply().ConfigureAwait(true);

        // Assert
        Assert.Null(crate.Label);
    }

    [Fact]
    public async Task Supply_WhenTheCallConfiguresTheValueTypeFieldToItsDefault_KeepsThatDefault()
    {
        // Arrange
        RecordProvider<Crate> provider = new(CrateLookup())
        {
            [x => x.Count] = 0,
        };

        // Act
        Crate crate = await provider.SetInsertMode(InsertMode.Never).Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(0, crate.Count);
    }

    // Fix B: drop the Provider default for this call ----------------

    [Fact]
    public async Task Supply_WhenTheCallRemovesTheFieldsFromTheMasterTemplate_LeavesThemAtTheEmptyValue()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Crate), CrateLookup())
            .SetInsertMode(InsertMode.Never)
            .RemoveFromMasterTemplate(Field.Of<Crate>(x => x.Label))
            .RemoveFromMasterTemplate(Field.Of<Crate>(x => x.Count));

        // Act
        Crate crate = (Crate)await provider.Supply().ConfigureAwait(true);

        // Assert - no default to apply, so both keep their empty value
        Assert.Null(crate.Label);
        Assert.Equal(0, crate.Count);
    }

    // Helper -------------------------------------------------------

    private static IProviderLookup CrateLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<Crate>()] = new CrateProvider() });
}

file sealed record Crate
{
    public string? Reference { get; init; }

    public string? Label { get; init; }

    public int Count { get; init; }
}

file sealed class CrateProvider : IRecordProvider
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate<Crate>(x => x.Reference)
        .Put(x => x.Label, new LiteralExpression("stock"))
        .Put(x => x.Count, new LiteralExpression(5));

    public PropertyInfo PrimaryTargetField => this.MasterTemplate.PrimaryTargetField;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}