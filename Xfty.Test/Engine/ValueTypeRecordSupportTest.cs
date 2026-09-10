using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Enrichment;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves the engine builds and fills record types whose shape isn't a plain
/// class / <c>record</c> body with writable <c>init</c> properties: a
/// <c>record struct</c>, a <c>readonly record struct</c>, a positional
/// <c>record class</c>, and a positional <c>record struct</c>.
///
/// Two mechanics carry this. <c>BlankInstances.Of</c> builds the blank to fill
/// from the public parameterless constructor when there is one and from an
/// uninitialized instance otherwise (a positional <c>record class</c> has only
/// its primary constructor). <c>PropertyInfo.SetValue</c> then populates every
/// field - it bypasses <c>init</c>, and it mutates a <em>boxed</em> value type
/// in place, so the same reflection passes that fill a <c>record class</c> fill
/// these too.
///
/// The value-type-field limitation this once had (a non-nullable value-type
/// field never received its configured default) is fixed and proven in
/// <see cref="NonNullableValueFieldTest"/>.
/// </summary>
public class ValueTypeRecordSupportTest
{
    private const string DefaultLabel = "Unlabelled";

    // record struct -------------------------------------------------------

    [Fact]
    public void DeepClone_ForARecordStruct_CopiesEveryProperty()
    {
        // Arrange
        object original = new GeoPoint { Id = "p-1", Label = "Origin", Region = "North" };

        // Act
        object clone = RecordCloneFactory.DeepClone(original);

        // Assert
        GeoPoint clonedPoint = (GeoPoint)clone;
        Assert.Equal("p-1", clonedPoint.Id);
        Assert.Equal("Origin", clonedPoint.Label);
        Assert.Equal("North", clonedPoint.Region);
    }

    [Fact]
    public void AddId_ForABoxedRecordStruct_WritesThroughTheInitOnlyProperty()
    {
        // Arrange
        object point = new GeoPoint { Label = "Unsaved" };

        // Act
        _ = IdMocker.AddId(point, Field.Of<GeoPoint>(x => x.Id));

        // Assert - the same box was mutated in place, exactly as for a record class
        Assert.StartsWith("mock-", ((GeoPoint)point).Id);
    }

    [Fact]
    public void Result_GraftingAClassParentOntoARecordStructChild_IsReadableBack()
    {
        // Arrange
        List<object> points = [new GeoPoint { Id = "p-1" }];
        List<object> owners = [new PointOwner { Name = "Cartography Dept" }];

        // Act
        List<object> grafted = RecordInjector.Inject(points)
            .Relationship(Field.Of<GeoPoint>(x => x.Owner), owners)
            .Result();

        // Assert
        Assert.Equal("Cartography Dept", ((GeoPoint)grafted[0]).Owner!.Name);
    }

    [Fact]
    public async Task SupplyBundle_ForARecordStructPrimaryInMockMode_FillsDefaultsAndMocksAnId()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(GeoPoint), GeoPointLookup())
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        GeoPoint generated = (GeoPoint)bundle.GetList<GeoPoint>(x => x.Id)![0];
        Assert.Equal(DefaultLabel, generated.Label);
        Assert.StartsWith("mock-", generated.Id);
    }

    [Fact]
    public async Task SupplyBundle_ForARecordStructChildWithARequiredClassParent_WiresTheForeignKey()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(GeoPoint), RelatedGeoPointLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        GeoPoint child = (GeoPoint)bundle.GetList<GeoPoint>(x => x.Id)![0];
        PointOwner parent = (PointOwner)bundle.GetList<GeoPoint>(x => x.OwnerId)![0];
        Assert.Equal(parent.Id, child.OwnerId);
    }

    // readonly record struct --------------------------------------------

    [Fact]
    public async Task Supply_ForAReadonlyRecordStructPrimary_FillsItThroughThePositionalInitAccessors()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(FrozenPoint), FrozenPointLookup())
            .SetInsertMode(InsertMode.Mock);

        // Act
        FrozenPoint generated = (FrozenPoint)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultLabel, generated.Label);
        Assert.StartsWith("mock-", generated.Id);
    }

    // positional record class ------------------------------------------

    [Fact]
    public void DeepClone_ForAPositionalRecordClass_BuildsAnUninitializedInstanceThenCopiesEveryProperty()
    {
        // Arrange - a positional record class has no parameterless constructor
        object original = new PositionalContact("pc-1", "Origin");

        // Act
        object clone = RecordCloneFactory.DeepClone(original);

        // Assert
        PositionalContact clonedContact = (PositionalContact)clone;
        Assert.Equal("pc-1", clonedContact.Id);
        Assert.Equal("Origin", clonedContact.Label);
    }

    [Fact]
    public async Task Supply_ForAPositionalRecordClassPrimary_GeneratesItEndToEnd()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(PositionalContact), PositionalContactLookup())
            .SetInsertMode(InsertMode.Mock);

        // Act
        PositionalContact generated = (PositionalContact)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(DefaultLabel, generated.Label);
        Assert.StartsWith("mock-", generated.Id);
    }

    [Fact]
    public async Task Supply_ForAPositionalRecordClassPrimary_KeepsAnOverrideTemplateValue()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(PositionalContact), PositionalContactLookup())
            .SetOverrideTemplate(new PositionalContact(null, "Explicit"))
            .SetInsertMode(InsertMode.Never);

        // Act
        PositionalContact generated = (PositionalContact)await provider.Supply().ConfigureAwait(true);

        // Assert - the override template's Label survives; the unset Id stays null under Never
        Assert.Equal("Explicit", generated.Label);
        Assert.Null(generated.Id);
    }

    // positional record struct ----------------------------------------

    [Fact]
    public void DeepClone_ForAPositionalRecordStruct_CopiesEveryProperty()
    {
        // Arrange
        object original = new PositionalReading("pr-1", "steady");

        // Act
        object clone = RecordCloneFactory.DeepClone(original);

        // Assert
        PositionalReading clonedReading = (PositionalReading)clone;
        Assert.Equal("pr-1", clonedReading.Id);
        Assert.Equal("steady", clonedReading.Note);
    }

    // Helpers -----------------------------------------------------------

    private static IProviderLookup GeoPointLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<GeoPoint>()] = new GeoPointProvider() });

    private static IProviderLookup FrozenPointLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<FrozenPoint>()] = new FrozenPointProvider() });

    private static IProviderLookup PositionalContactLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get<PositionalContact>()] = new PositionalContactProvider() });

    private static IProviderLookup RelatedGeoPointLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<GeoPoint>()] = new RelatedGeoPointProvider(),
            [LookupKey.Get<PointOwner>()] = new PointOwnerProvider(),
        });
}

file record struct GeoPoint
{
    public string? Id { get; init; }

    public string? Label { get; init; }

    public string? Region { get; init; }

    public string? OwnerId { get; init; }

    public PointOwner? Owner { get; init; }
}

file sealed class PointOwner
{
    public string? Id { get; init; }

    public string? Name { get; init; }
}

file readonly record struct FrozenPoint(string? Id, string? Label);

file sealed record PositionalContact(string? Id, string? Label);

file record struct PositionalReading(string? Id, string? Note);

file abstract class ValueTypeProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class GeoPointProvider : ValueTypeProviderBase
{
    public GeoPointProvider() =>
        this.Template = new MasterTemplate<GeoPoint>(x => x.Id)
            .Put(x => x.Label, new LiteralExpression("Unlabelled"));
}

file sealed class RelatedGeoPointProvider : ValueTypeProviderBase
{
    public RelatedGeoPointProvider() =>
        this.Template = new MasterTemplate<GeoPoint>(x => x.Id)
            .Put(x => x.Label, new LiteralExpression("Unlabelled"))
            .PutRequired(x => x.OwnerId, new DefaultRelationship(new PointOwner()));
}

file sealed class PointOwnerProvider : ValueTypeProviderBase
{
    public PointOwnerProvider() =>
        this.Template = new MasterTemplate<PointOwner>(x => x.Id)
            .Put(x => x.Name, new LiteralExpression("Owner"));
}

file sealed class FrozenPointProvider : ValueTypeProviderBase
{
    public FrozenPointProvider() =>
        this.Template = new MasterTemplate<FrozenPoint>(x => x.Id)
            .Put(x => x.Label, new LiteralExpression("Unlabelled"));
}

file sealed class PositionalContactProvider : ValueTypeProviderBase
{
    public PositionalContactProvider() =>
        this.Template = new MasterTemplate<PositionalContact>(x => x.Id)
            .Put(x => x.Label, new LiteralExpression("Unlabelled"));
}
