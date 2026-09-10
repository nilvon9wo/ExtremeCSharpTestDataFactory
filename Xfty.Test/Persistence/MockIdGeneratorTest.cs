using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Persistence;

/// <summary>
/// Proves the pluggable mock-Id path: <see cref="DefaultMockIdGenerator"/>
/// renders the sequence as the Id field's own type (so <c>InsertMode.Mock</c>
/// works for a non-string primary key), a <c>MasterTemplate&lt;T&gt;</c>
/// declares a project-specific generator per record type, and
/// <c>RecordProvider.SetMockIdGenerator</c> overrides it per call for the
/// primary only - a generated ancestor keeps its own.
/// </summary>
public class MockIdGeneratorTest
{
    // DefaultMockIdGenerator - the built-in Id shapes -------------------

    [Fact]
    public void NextId_ForAStringIdField_ProducesAMockNString()
    {
        // Arrange
        MockIdContext context = ContextFor<StringKeyed>(x => x.Id, new StringKeyed());

        // Act
        object id = DefaultMockIdGenerator.Instance.NextId(context);

        // Assert
        Assert.StartsWith("mock-", Assert.IsType<string>(id));
    }

    [Fact]
    public void NextId_ForAnIntIdField_ProducesAnInt()
    {
        // Arrange
        MockIdContext context = ContextFor<IntKeyed>(x => x.Id, new IntKeyed());

        // Act
        object id = DefaultMockIdGenerator.Instance.NextId(context);

        // Assert
        Assert.True(Assert.IsType<int>(id) > 0);
    }

    [Fact]
    public void NextId_ForALongIdField_ProducesALong()
    {
        // Arrange
        MockIdContext context = ContextFor<LongKeyed>(x => x.Id, new LongKeyed());

        // Act
        object id = DefaultMockIdGenerator.Instance.NextId(context);

        // Assert
        Assert.True(Assert.IsType<long>(id) > 0L);
    }

    [Fact]
    public void NextId_ForANullableIntIdField_ProducesAnInt()
    {
        // Arrange
        MockIdContext context = ContextFor<NullableIntKeyed>(x => x.Id, new NullableIntKeyed());

        // Act
        object id = DefaultMockIdGenerator.Instance.NextId(context);

        // Assert - Nullable<int> is unwrapped to int
        Assert.True(Assert.IsType<int>(id) > 0);
    }

    [Fact]
    public void NextId_ForAGuidIdField_ProducesANonEmptyGuid()
    {
        // Arrange
        MockIdContext context = ContextFor<GuidKeyed>(x => x.Id, new GuidKeyed());

        // Act
        object id = DefaultMockIdGenerator.Instance.NextId(context);

        // Assert
        Assert.NotEqual(Guid.Empty, Assert.IsType<Guid>(id));
    }

    [Fact]
    public void NextId_ForAnIdTypeWithNoBuiltInSupport_ThrowsNamingTheTypeAndTheRecord()
    {
        // Arrange
        MockIdContext context = ContextFor<ByteKeyed>(x => x.Id, new ByteKeyed());

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => DefaultMockIdGenerator.Instance.NextId(context));

        // Assert
        Assert.Contains("Byte", thrown.Message);
        Assert.Contains(nameof(ByteKeyed), thrown.Message);
        Assert.Contains("WithMockIdGenerator", thrown.Message);
    }

    // End to end -------------------------------------------------------

    [Fact]
    public async Task Supply_ForAnIntPrimaryKeyInMockMode_AssignsAnIntIdWithTheBuiltInGenerator()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(IntKeyed), LookupOf(new IntKeyedProvider()))
            .SetInsertMode(InsertMode.Mock);

        // Act
        IntKeyed generated = (IntKeyed)await provider.Supply().ConfigureAwait(true);

        // Assert - InsertMode.Mock no longer assumes a string Id
        Assert.True(generated.Id > 0);
    }

    [Fact]
    public async Task Supply_WhenTheMasterTemplateDeclaresAMockIdGenerator_UsesIt()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(StringKeyed), LookupOf(new AccountStyleProvider()))
            .SetInsertMode(InsertMode.Mock);

        // Act
        StringKeyed generated = (StringKeyed)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.StartsWith("ACC-", generated.Id);
    }

    [Fact]
    public async Task Supply_WhenSetMockIdGeneratorIsCalled_OverridesTheTemplatesGenerator()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(StringKeyed), LookupOf(new AccountStyleProvider()))
            .SetMockIdGenerator(new PrefixIdGenerator("PER-CALL"))
            .SetInsertMode(InsertMode.Mock);

        // Act
        StringKeyed generated = (StringKeyed)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.StartsWith("PER-CALL-", generated.Id);
    }

    [Fact]
    public async Task SupplyBundle_WhenTheCallOverridesTheGenerator_TheAncestorKeepsItsOwn()
    {
        // Arrange - the child's per-call override must not reach the generated parent
        RecordProvider provider = new RecordProvider(typeof(StringKeyed), RelatedLookup())
            .SetMockIdGenerator(new PrefixIdGenerator("CHILD"))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        StringKeyed child = (StringKeyed)bundle.GetList<StringKeyed>(x => x.Id)![0];
        StringParent parent = (StringParent)bundle.GetList<StringKeyed>(x => x.ParentId)![0];
        Assert.StartsWith("CHILD-", child.Id);
        Assert.StartsWith("PARENT-", parent.Id);
    }

    // Helpers -------------------------------------------------------

    private static MockIdContext ContextFor<TRecord>(
        System.Linq.Expressions.Expression<Func<TRecord, object?>> idField, object record) =>
        new(typeof(TRecord), Field.Of(idField), record);

    private static IProviderLookup LookupOf(IRecordProvider provider) =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get(provider.PrimaryTargetField.DeclaringType!)] = provider });

    private static IProviderLookup RelatedLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<StringKeyed>()] = new RelatedStringKeyedProvider(),
            [LookupKey.Get<StringParent>()] = new StringParentProvider(),
        });
}

file sealed record StringKeyed
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? ParentId { get; init; }
}

file sealed record IntKeyed
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

file sealed record GuidKeyed
{
    public Guid Id { get; init; }
}

file sealed record LongKeyed
{
    public long Id { get; init; }
}

file sealed record NullableIntKeyed
{
    public int? Id { get; init; }
}

file sealed record ByteKeyed
{
    public byte Id { get; init; }
}

file sealed record StringParent
{
    public string? Id { get; init; }
}

file sealed class PrefixIdGenerator(string prefix) : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"{prefix}-{++this._count}";
}

/// <summary>The shape from the discussion: a letter, a running number, a stamp - built without touching the record's own fields.</summary>
file sealed class AccountStyleIdGenerator : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"ACC-{++this._count}-{context.RecordType.Name}";
}

file abstract class MockIdProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class IntKeyedProvider : MockIdProviderBase
{
    public IntKeyedProvider() =>
        this.Template = new MasterTemplate<IntKeyed>(x => x.Id)
            .Put(x => x.Name, new LiteralExpression("row"));
}

file sealed class AccountStyleProvider : MockIdProviderBase
{
    public AccountStyleProvider() =>
        this.Template = new MasterTemplate<StringKeyed>(x => x.Id)
            .Put(x => x.Name, new LiteralExpression("row"))
            .WithMockIdGenerator(new AccountStyleIdGenerator());
}

file sealed class RelatedStringKeyedProvider : MockIdProviderBase
{
    public RelatedStringKeyedProvider() =>
        this.Template = new MasterTemplate<StringKeyed>(x => x.Id)
            .PutRequired(x => x.ParentId, new Net.NowhereAtAll.Xfty.Relationships.DefaultRelationship(new StringParent()));
}

file sealed class StringParentProvider : MockIdProviderBase
{
    public StringParentProvider() =>
        this.Template = new MasterTemplate<StringParent>(x => x.Id)
            .WithMockIdGenerator(new PrefixIdGenerator("PARENT"));
}