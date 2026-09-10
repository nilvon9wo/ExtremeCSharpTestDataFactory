using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Persistence;

/// <summary>
/// Proves nothing in the persistence path assumes the primary-key property is
/// called <c>Id</c> - it comes from the Provider's
/// <see cref="MasterTemplate.PrimaryTargetField"/>, through the bundle, all
/// the way down to the depth-batched inserter. A project whose keys are
/// <c>LedgerId</c> / <c>EntryId</c> (named after the table) works the same as
/// one using <c>Id</c>.
/// </summary>
public class NonIdPrimaryKeyTest : IDisposable
{
    // These tests register shared ancestors of file-local types no other test's lookup knows, and
    // use lookups that can't tolerate a stray shared ancestor of any other type either. Start and
    // end each test with a clean process-static registry (the same pattern as SharedAncestorResetTest).
    public NonIdPrimaryKeyTest() => SharedAncestor.ResetAllForTesting();

    public void Dispose() => SharedAncestor.ResetAllForTesting();

    // RecordFactory / InsertMode.Mock --------------------------------------

    [Fact]
    public async Task Supply_ForARecordWhosePrimaryKeyIsNotNamedId_MocksThatField()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Ledger), LookupOf(new LedgerProvider()))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Ledger generated = (Ledger)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.StartsWith("mock-", generated.LedgerId);
    }

    [Fact]
    public async Task SupplyBundle_WhenARelationshipHasNoExplicitRelatedField_WiresTheChildFromTheParentsOwnPrimaryKey()
    {
        // Arrange - Entry.LedgerRef relates to Ledger, whose key is LedgerId, not Id
        RecordProvider provider = new RecordProvider(typeof(Entry), RelatedLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Entry child = (Entry)bundle.GetList<Entry>(x => x.EntryId)![0];
        Ledger parent = (Ledger)bundle.GetList<Entry>(x => x.LedgerRef)![0];
        Assert.Equal(parent.LedgerId, child.LedgerRef);
        Assert.NotNull(child.LedgerRef);
    }

    // Depth-batched / deferred path --------------------------------------

    [Fact]
    public async Task ResolveAll_ForAHandBuiltBundleWithANonIdPrimaryKey_MocksThatField()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Ledger>(x => x.LedgerId), [new Ledger(), new Ledger()]);
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock).ConfigureAwait(true);

        // Assert
        Assert.StartsWith("mock-", ((Ledger)bundle.PrimaryRecords()![1]).LedgerId);
    }

    [Fact]
    public async Task ResolveAll_WhenTheBundleCarriesAMockIdGenerator_TheDepthBatchedPassHonoursIt()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Ledger>(x => x.LedgerId), [new Ledger()], new LedgerIdGenerator());
        DeferredInsertBuffer buffer = new();
        buffer.Add(bundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock).ConfigureAwait(true);

        // Assert - the custom generator's shape, not "mock-N"
        Assert.StartsWith("LDG-", ((Ledger)bundle.PrimaryRecords()![0]).LedgerId!);
    }

    [Fact]
    public async Task ResolveAll_ForAParentAndChildWithNonIdKeys_PointsTheChildAtTheParentsNonIdKey()
    {
        // Arrange
        Bundle parentBundle = new();
        parentBundle.PutPrimaries(Field.Of<Ledger>(x => x.LedgerId), [new Ledger()]);
        Bundle childBundle = new();
        childBundle.PutPrimaries(Field.Of<Entry>(x => x.EntryId), [new Entry()]);
        _ = childBundle.Put(Field.Of<Entry>(x => x.LedgerRef), parentBundle);
        DeferredInsertBuffer buffer = new();
        buffer.Add(childBundle);

        // Act
        await buffer.ResolveAll(InsertMode.Mock).ConfigureAwait(true);

        // Assert
        Ledger parent = (Ledger)parentBundle.PrimaryRecords()![0];
        Entry child = (Entry)childBundle.PrimaryRecords()![0];
        Assert.Equal(parent.LedgerId, child.LedgerRef);
        Assert.NotNull(child.LedgerRef);
    }

    // Shared ancestor -----------------------------------------------

    [Fact]
    public async Task SharedAncestor_ForATypeWhosePrimaryKeyIsNotNamedId_ResolvesAndReportsThatKey()
    {
        // Arrange - never-reused name; PutAsTemplate is explicit, so the "Id"-property heuristic never runs
        const string name = "non-id-pk-test-shared-ledger";
        _ = SharedAncestor.PutAsTemplate(name, new Ledger { Name = "Shared" });

        // Act
        List<object> entries = await new RecordProvider(typeof(Entry), RelatedLookup())
            .PutRequired<Entry>(x => x.LedgerRef, SharedAncestor.Get(name))
            .SetQuantityPerTemplate(3)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyList().ConfigureAwait(true);

        // Assert - every Entry resolves to the one shared Ledger's LedgerId, and GetId reads that same field
        object sharedId = SharedAncestor.GetId(name);
        Assert.StartsWith("mock-", (string)sharedId);
        Assert.All(entries.Cast<Entry>(), entry => Assert.Equal(sharedId, entry.LedgerRef));
    }

    [Fact]
    public async Task SharedAncestor_PutAsTemplateWithANonIdKeyAlreadySet_IsUsedAsIsRatherThanRegenerated()
    {
        // Arrange - the registered Ledger already carries its LedgerId; the resolver should keep it
        const string name = "non-id-pk-test-preformed-ledger";
        _ = SharedAncestor.PutAsTemplate(name, new Ledger { LedgerId = "LGR-EXISTING-42", Name = "Real" });

        // Act
        Entry entry = await new RecordProvider<Entry>(RelatedLookup())
            .PutRequired(x => x.LedgerRef, SharedAncestor.Get(name))
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .Supply().ConfigureAwait(true);

        // Assert - the preset key survived and GetId reports it (no "mock-" regeneration)
        Assert.Equal("LGR-EXISTING-42", entry.LedgerRef);
        Assert.Equal("LGR-EXISTING-42", SharedAncestor.GetId(name));
    }

    // Helpers ---------------------------------------------------------

    private static IProviderLookup LookupOf(IRecordProvider provider) =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get(provider.PrimaryTargetField.DeclaringType!)] = provider });

    private static IProviderLookup RelatedLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Entry>()] = new EntryProvider(),
            [LookupKey.Get<Ledger>()] = new LedgerProvider(),
        });
}

file sealed record Ledger
{
    public string? LedgerId { get; init; }

    public string? Name { get; init; }
}

file sealed record Entry
{
    public string? EntryId { get; init; }

    public string? LedgerRef { get; init; }
}

file sealed class LedgerIdGenerator : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"LDG-{++this._count}";
}

file abstract class NonIdProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class LedgerProvider : NonIdProviderBase
{
    public LedgerProvider() =>
        this.Template = new MasterTemplate<Ledger>(x => x.LedgerId)
            .Put(x => x.Name, new LiteralExpression("Main"));
}

file sealed class EntryProvider : NonIdProviderBase
{
    public EntryProvider() =>
        this.Template = new MasterTemplate<Entry>(x => x.EntryId)
            .PutRequired(x => x.LedgerRef, new DefaultRelationship(new Ledger()));
}