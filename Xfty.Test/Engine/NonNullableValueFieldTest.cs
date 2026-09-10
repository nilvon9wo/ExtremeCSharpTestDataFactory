using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves a non-nullable value-type field (<c>int</c>, <c>bool</c>,
/// <c>Guid</c>, an <c>enum</c>, <c>DateTime</c>) now receives its configured
/// value from every value pass - a plain default, a context-aware value, and
/// a wired foreign key.
///
/// The engine's "may I fill this field?" test used to be
/// <c>field.GetValue(record) is not null</c>, which is always true for a
/// boxed <c>0</c> / <c>false</c> / empty <c>Guid</c>, so the configured value
/// was silently skipped. <c>FieldState.IsUnset</c> now compares against the
/// value a freshly-built instance would hold, so those fields fill like a
/// reference-typed one. This was never struct-specific - it bit any
/// non-nullable value-type field on a class or record too.
///
/// The residual - an override template can't force such a field to its empty
/// value over a Provider default, exactly as for a reference type - is proven
/// both ways, with both workarounds, in <see cref="ForcingAnEmptyFieldValueTest"/>.
/// </summary>
public class NonNullableValueFieldTest
{
    private const int DefaultQuantity = 7;
    private static readonly Guid DefaultSerial = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime DefaultMadeOn = new(2020, 6, 1);

    // Plain defaults, one value-type family per test ----------------------

    [Fact]
    public async Task Supply_ForAnIntFieldWithAConfiguredDefault_AppliesIt()
    {
        // Arrange
        RecordProvider provider = WidgetProvider();

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultQuantity, widget.Quantity);
    }

    [Fact]
    public async Task Supply_ForABoolFieldWithAConfiguredDefault_AppliesIt()
    {
        // Arrange
        RecordProvider provider = WidgetProvider();

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.True(widget.IsActive);
    }

    [Fact]
    public async Task Supply_ForAGuidFieldWithAConfiguredDefault_AppliesIt()
    {
        // Arrange
        RecordProvider provider = WidgetProvider();

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultSerial, widget.Serial);
    }

    [Fact]
    public async Task Supply_ForAnEnumFieldWithAConfiguredNonZeroDefault_AppliesIt()
    {
        // Arrange
        RecordProvider provider = WidgetProvider();

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(WidgetKind.Premium, widget.Kind);
    }

    [Fact]
    public async Task Supply_ForADateTimeFieldWithAConfiguredDefault_AppliesIt()
    {
        // Arrange
        RecordProvider provider = WidgetProvider();

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultMadeOn, widget.MadeOn);
    }

    // The same fix, reached through a record class and a record struct ----

    [Fact]
    public async Task Supply_ForARecordClassIntField_AppliesTheConfiguredDefault()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Crate), CrateLookup()).SetInsertMode(InsertMode.Never);

        // Act
        Crate crate = (Crate)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultQuantity, crate.Units);
    }

    [Fact]
    public async Task Supply_ForARecordStructDoubleField_AppliesTheConfiguredDefault()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Parcel), ParcelLookup()).SetInsertMode(InsertMode.Never);

        // Act
        Parcel parcel = (Parcel)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(1.5, parcel.WeightKg);
    }

    // A nullable value type still behaves like a reference type ----------

    [Fact]
    public async Task Supply_ForANullNullableValueTypeField_AppliesTheConfiguredDefault()
    {
        // Arrange - Count is int?, left null on the template; the provider defaults it to 9
        RecordProvider provider = new RecordProvider(typeof(Tote), ToteLookup()).SetInsertMode(InsertMode.Never);

        // Act
        Tote generated = (Tote)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(9, generated.Count);
    }

    [Fact]
    public async Task Supply_WhenANullableValueTypeFieldIsPresetToZero_KeepsIt()
    {
        // Arrange - unlike a non-nullable int, an explicit 0 in an int? is distinguishable from unset
        RecordProvider provider = new RecordProvider(typeof(Tote), ToteLookup())
            .SetOverrideTemplate(new Tote { Count = 0 })
            .SetInsertMode(InsertMode.Never);

        // Act
        Tote generated = (Tote)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(0, generated.Count);
    }

    // Override templates -------------------------------------------------

    [Fact]
    public async Task Supply_WhenAnOverrideTemplatePresetsAnIntFieldToANonDefaultValue_KeepsThePreset()
    {
        // Arrange
        RecordProvider provider = WidgetProvider().SetOverrideTemplate(new Widget { Quantity = 3 });

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert - a non-default preset is distinguishable from unset, so it wins
        Assert.Equal(3, widget.Quantity);
    }

    [Fact]
    public async Task Supply_WhenAnOverrideTemplatePresetsAnIntFieldToZero_TheProviderDefaultStillWins()
    {
        // Arrange - the template asks for 0, which reflection cannot tell apart from "never set"
        RecordProvider provider = WidgetProvider().SetOverrideTemplate(new Widget { Quantity = 0 });

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert - the irreducible gap: use the fluent [x => x.Quantity] = 0 form to pin the type default
        Assert.Equal(DefaultQuantity, widget.Quantity);
    }

    // Context-aware value and wired foreign key -------------------------

    [Fact]
    public async Task Supply_ForAContextAwareValueIntoAnIntField_AppliesIt()
    {
        // Arrange - ReorderLevel is context-aware, copying the plain Quantity sibling
        RecordProvider provider = WidgetProvider()
            .Put<Widget>(x => x.ReorderLevel, CopyFromSiblingExpression.From<Widget>(x => x.Quantity));

        // Act
        Widget widget = (Widget)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultQuantity, widget.ReorderLevel);
    }

    [Fact]
    public async Task Supply_ForAForeignKeyCopiedIntoANonNullableValueTypeField_WiresIt()
    {
        // Arrange - Crate.DepotCode (int) is wired from the required Depot parent's Code
        RecordProvider provider = new RecordProvider(typeof(Crate), DepotChainLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Never);

        // Act
        Crate crate = (Crate)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(Depot.FixedCode, crate.DepotCode);
    }

    // Helpers ---------------------------------------------------------

    private static RecordProvider WidgetProvider() =>
        new RecordProvider(typeof(Widget), WidgetLookup()).SetInsertMode(InsertMode.Never);

    private static IProviderLookup WidgetLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<Widget>()] = new WidgetProviderImpl() });

    private static IProviderLookup CrateLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<Crate>()] = new PlainCrateProvider() });

    private static IProviderLookup ParcelLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<Parcel>()] = new ParcelProvider() });

    private static IProviderLookup DepotChainLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Crate>()] = new DepotCrateProvider(),
            [LookupKey.Get<Depot>()] = new DepotProvider(),
        });

    private static IProviderLookup ToteLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<Tote>()] = new ToteProvider() });
}

file enum WidgetKind
{
    Unknown = 0,
    Standard = 1,
    Premium = 2,
}

file sealed class Widget
{
    public string? Id { get; init; }

    public int Quantity { get; init; }

    public int ReorderLevel { get; init; }

    public bool IsActive { get; init; }

    public Guid Serial { get; init; }

    public WidgetKind Kind { get; init; }

    public DateTime MadeOn { get; init; }
}

file sealed record Crate
{
    public string? Id { get; init; }

    public int Units { get; init; }

    public int DepotCode { get; init; }
}

file readonly record struct Parcel
{
    public string? Id { get; init; }

    public double WeightKg { get; init; }
}

file sealed record Tote
{
    public string? Id { get; init; }

    public int? Count { get; init; }
}

file sealed class Depot
{
    public const int FixedCode = 4242;

    public string? Id { get; init; }

    public int Code { get; init; }
}

file abstract class ValueFieldProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class WidgetProviderImpl : ValueFieldProviderBase
{
    public WidgetProviderImpl() =>
        this.Template = new MasterTemplate<Widget>(x => x.Id)
            .Put(x => x.Quantity, new LiteralExpression(7))
            .Put(x => x.IsActive, new LiteralExpression(true))
            .Put(x => x.Serial, new LiteralExpression(new Guid("11111111-1111-1111-1111-111111111111")))
            .Put(x => x.Kind, new LiteralExpression(WidgetKind.Premium))
            .Put(x => x.MadeOn, new LiteralExpression(new DateTime(2020, 6, 1)));
}

file sealed class PlainCrateProvider : ValueFieldProviderBase
{
    public PlainCrateProvider() =>
        this.Template = new MasterTemplate<Crate>(x => x.Id)
            .Put(x => x.Units, new LiteralExpression(7));
}

file sealed class ParcelProvider : ValueFieldProviderBase
{
    public ParcelProvider() =>
        this.Template = new MasterTemplate<Parcel>(x => x.Id)
            .Put(x => x.WeightKg, new LiteralExpression(1.5));
}

file sealed class ToteProvider : ValueFieldProviderBase
{
    public ToteProvider() =>
        this.Template = new MasterTemplate<Tote>(x => x.Id)
            .Put(x => x.Count, new LiteralExpression(9));
}

file sealed class DepotCrateProvider : ValueFieldProviderBase
{
    public DepotCrateProvider() =>
        this.Template = new MasterTemplate<Crate>(x => x.Id)
            .PutRequired(x => x.DepotCode, new DefaultRelationship(new Depot(), Field.Of<Depot>(x => x.Code)));
}

file sealed class DepotProvider : ValueFieldProviderBase
{
    public DepotProvider() =>
        this.Template = new MasterTemplate<Depot>(x => x.Id)
            .Put(x => x.Code, new LiteralExpression(Depot.FixedCode));
}