using System.Reflection;
using System.Text.RegularExpressions;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Persistence;
using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Test.Persistence;

/// <summary>
/// The torture test: ten levels of required ancestors, no two primary keys
/// named alike, three different key CLR types (<c>string</c>, <c>long</c>,
/// <c>int</c>, <c>Guid</c>), and the string keys each carrying a
/// project-specific shape - a type prefix, a datestamp, a zero-padded
/// sequence, a value read off the record. Proves the whole chain: XFTY reads
/// every key from the Provider's <c>PrimaryTargetField</c>, mocks each in its
/// own type/shape via that type's <see cref="IMockIdGenerator"/>, and wires
/// each level's foreign key from its parent's real key - `Mock` mode, no
/// database, over `RecordFactory` and the depth-batched shared-ancestor
/// resolver alike.
/// </summary>
public class DeepMixedKeyHierarchyTest : IDisposable
{
    // This class's lookups know only its own file-local types, so it can't tolerate a stray
    // shared ancestor of any other type in the process-static registry - and its own shared-ancestor
    // case would poison later tests just the same. Start and end each test with a clean registry
    // (the same pattern as SharedAncestorResetTest).
    public DeepMixedKeyHierarchyTest() => SharedAncestor.ResetAllForTesting();

    public void Dispose() => SharedAncestor.ResetAllForTesting();

    private static IProviderLookup DeepChainLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<L0Order>()] = new L0OrderProvider(),
            [LookupKey.Get<L1Batch>()] = new L1BatchProvider(),
            [LookupKey.Get<L2Shipment>()] = new L2ShipmentProvider(),
            [LookupKey.Get<L3Carton>()] = new L3CartonProvider(),
            [LookupKey.Get<L4Pallet>()] = new L4PalletProvider(),
            [LookupKey.Get<L5Depot>()] = new L5DepotProvider(),
            [LookupKey.Get<L6Manifest>()] = new L6ManifestProvider(),
            [LookupKey.Get<L7Route>()] = new L7RouteProvider(),
            [LookupKey.Get<L8Region>()] = new L8RegionProvider(),
            [LookupKey.Get<L9Tenant>()] = new L9TenantProvider(),
        });

    [Fact]
    public async Task SupplyBundle_ForATenDeepChainOfMixedKeys_MocksEachInItsOwnShapeAndWiresEveryForeignKey()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(L0Order), DeepChainLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Sanity Check - the whole spine was generated
        L0Order l0 = (L0Order)bundle.GetList<L0Order>(x => x.OrderRef)![0];
        L1Batch l1 = (L1Batch)bundle.GetList<L0Order>(x => x.BatchNo)![0];
        Bundle l1Bundle = bundle.GetBundle<L0Order>(x => x.BatchNo)!;
        L2Shipment l2 = (L2Shipment)l1Bundle.GetList<L1Batch>(x => x.ShipmentToken)![0];
        Bundle l2Bundle = l1Bundle.GetBundle<L1Batch>(x => x.ShipmentToken)!;
        L3Carton l3 = (L3Carton)l2Bundle.GetList<L2Shipment>(x => x.CartonGuid)![0];
        Bundle l3Bundle = l2Bundle.GetBundle<L2Shipment>(x => x.CartonGuid)!;
        L4Pallet l4 = (L4Pallet)l3Bundle.GetList<L3Carton>(x => x.PalletSeq)![0];
        Bundle l4Bundle = l3Bundle.GetBundle<L3Carton>(x => x.PalletSeq)!;
        L5Depot l5 = (L5Depot)l4Bundle.GetList<L4Pallet>(x => x.DepotCode)![0];
        Bundle l5Bundle = l4Bundle.GetBundle<L4Pallet>(x => x.DepotCode)!;
        L6Manifest l6 = (L6Manifest)l5Bundle.GetList<L5Depot>(x => x.ManifestDocId)![0];
        Bundle l6Bundle = l5Bundle.GetBundle<L5Depot>(x => x.ManifestDocId)!;
        L7Route l7 = (L7Route)l6Bundle.GetList<L6Manifest>(x => x.RouteTicket)![0];
        Bundle l7Bundle = l6Bundle.GetBundle<L6Manifest>(x => x.RouteTicket)!;
        L8Region l8 = (L8Region)l7Bundle.GetList<L7Route>(x => x.RegionSlug)![0];
        Bundle l8Bundle = l7Bundle.GetBundle<L7Route>(x => x.RegionSlug)!;
        L9Tenant l9 = (L9Tenant)l8Bundle.GetList<L8Region>(x => x.TenantUuid)![0];

        // Assert - each key mocked in its own CLR type and project-specific shape
        Assert.Matches("^ORD-[0-9]+$", l0.OrderRef!);
        Assert.True(l1.LineNo > 0);
        Assert.Matches(@"^SHIP\|[0-9]+\|[0-9]+$", l2.ShipmentKey!);
        Assert.NotEqual(Guid.Empty, l3.CartonId);
        Assert.True(l4.PalletNumber > 0);
        Assert.StartsWith("DEPOT-EMEA-", l5.DepotId!); // prefix taken off the record's own Region field
        Assert.Matches(@"^MAN-[0-9]{8}-[0-9]+$", l6.ManifestId!);
        Assert.True(l7.TicketNo > 0);
        Assert.Matches("^rgn_[0-9]{6}$", l8.Slug!);
        Assert.NotEqual(Guid.Empty, l9.Uuid);

        // Assert - every foreign key points at its parent's real (non-"Id") primary key
        Assert.Equal(l1.LineNo, l0.BatchNo);
        Assert.Equal(l2.ShipmentKey, l1.ShipmentToken);
        Assert.Equal(l3.CartonId, l2.CartonGuid);
        Assert.Equal(l4.PalletNumber, l3.PalletSeq);
        Assert.Equal(l5.DepotId, l4.DepotCode);
        Assert.Equal(l6.ManifestId, l5.ManifestDocId);
        Assert.Equal(l7.TicketNo, l6.RouteTicket);
        Assert.Equal(l8.Slug, l7.RegionSlug);
        Assert.Equal(l9.Uuid, l8.TenantUuid);
    }

    [Fact]
    public async Task SharedAncestor_MidChainWithANonIdKey_ResolvesOnceAndGetIdReadsThatKey()
    {
        // Arrange - a shared L5Depot (key: DepotId, a "DEPOT-<region>-N" string) two L4 rows converge on
        const string name = "deep-mixed-key-shared-depot";
        _ = SharedAncestor.PutAsTemplate(name, new L5Depot { Region = "EMEA" });

        // Act
        List<object> pallets = await new RecordProvider(typeof(L4Pallet), DeepChainLookup())
            .PutRequired<L4Pallet>(x => x.DepotCode, SharedAncestor.Get(name))
            .SetQuantityPerTemplate(2)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock)
            .SupplyList().ConfigureAwait(true);

        // Assert
        object sharedDepotId = SharedAncestor.GetId(name);
        Assert.StartsWith("DEPOT-EMEA-", (string)sharedDepotId);
        Assert.All(pallets.Cast<L4Pallet>(), pallet => Assert.Equal(sharedDepotId, pallet.DepotCode));
    }

    [Fact]
    public async Task Supply_WhenAKeyIsPresetOnTheOverrideTemplate_MockModeKeepsItRatherThanRegenerating()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(L2Shipment), DeepChainLookup())
            .SetOverrideTemplate(new L2Shipment { ShipmentKey = "SHIP|preset|preset" })
            .SetInclusivity(InsertInclusivity.None)
            .SetInsertMode(InsertMode.Mock);

        // Act
        L2Shipment generated = (L2Shipment)await provider.Supply().ConfigureAwait(true);

        // Assert - a preset primary key survives Mock mode
        Assert.Equal("SHIP|preset|preset", generated.ShipmentKey);
    }
}

// The ten levels - every key named differently, four CLR types, string shapes all over ----------------

file sealed record L0Order    // string key, simple prefix
{
    public string? OrderRef { get; init; }

    public long BatchNo { get; init; }
}

file sealed record L1Batch    // long key (built-in)
{
    public long LineNo { get; init; }

    public string? ShipmentToken { get; init; }
}

file sealed record L2Shipment // string key: SHIP|<unix>|<seq>
{
    public string? ShipmentKey { get; init; }

    public Guid CartonGuid { get; init; }
}

file sealed record L3Carton   // Guid key (built-in)
{
    public Guid CartonId { get; init; }

    public int PalletSeq { get; init; }
}

file sealed record L4Pallet   // int key (built-in)
{
    public int PalletNumber { get; init; }

    public string? DepotCode { get; init; }
}

file sealed record L5Depot    // string key: DEPOT-<Region>-<seq>, prefix read off the record
{
    public string? DepotId { get; init; }

    public string? Region { get; init; }

    public string? ManifestDocId { get; init; }
}

file sealed record L6Manifest // string key: MAN-<yyyyMMdd>-<seq>
{
    public string? ManifestId { get; init; }

    public long RouteTicket { get; init; }
}

file sealed record L7Route    // long key (built-in)
{
    public long TicketNo { get; init; }

    public string? RegionSlug { get; init; }
}

file sealed record L8Region   // string key: rgn_<000000>
{
    public string? Slug { get; init; }

    public Guid TenantUuid { get; init; }
}

file sealed record L9Tenant   // Guid key (built-in)
{
    public Guid Uuid { get; init; }
}

file sealed class PrefixSequenceIdGenerator(string prefix) : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"{prefix}{++this._count}";
}

file sealed class ShipmentKeyGenerator : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"SHIP|{1_700_000_000 + this._count}|{++this._count}";
}

/// <summary>Reads the record's own Region field to build the id - the "any weird requirement" case.</summary>
file sealed class DepotIdGenerator : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context)
    {
        string region = (string?)context.RecordType.GetProperty("Region")!.GetValue(context.Record) ?? "XX";
        return $"DEPOT-{region}-{++this._count}";
    }
}

file sealed class DatestampedIdGenerator(string prefix) : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"{prefix}-{new DateTime(2024, 3, 9):yyyyMMdd}-{++this._count}";
}

file sealed class ZeroPaddedSlugGenerator : IMockIdGenerator
{
    private int _count;

    public object NextId(MockIdContext context) => $"rgn_{++this._count:D6}";
}

file abstract class DeepChainProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);
}

file sealed class L0OrderProvider : DeepChainProviderBase
{
    public L0OrderProvider() =>
        this.Template = new MasterTemplate<L0Order>(x => x.OrderRef)
            .WithMockIdGenerator(new PrefixSequenceIdGenerator("ORD-"))
            .PutRequired(x => x.BatchNo, new DefaultRelationship(new L1Batch()));
}

file sealed class L1BatchProvider : DeepChainProviderBase
{
    public L1BatchProvider() =>
        this.Template = new MasterTemplate<L1Batch>(x => x.LineNo)
            .PutRequired(x => x.ShipmentToken, new DefaultRelationship(new L2Shipment()));
}

file sealed class L2ShipmentProvider : DeepChainProviderBase
{
    public L2ShipmentProvider() =>
        this.Template = new MasterTemplate<L2Shipment>(x => x.ShipmentKey)
            .WithMockIdGenerator(new ShipmentKeyGenerator())
            .PutRequired(x => x.CartonGuid, new DefaultRelationship(new L3Carton()));
}

file sealed class L3CartonProvider : DeepChainProviderBase
{
    public L3CartonProvider() =>
        this.Template = new MasterTemplate<L3Carton>(x => x.CartonId)
            .PutRequired(x => x.PalletSeq, new DefaultRelationship(new L4Pallet()));
}

file sealed class L4PalletProvider : DeepChainProviderBase
{
    public L4PalletProvider() =>
        this.Template = new MasterTemplate<L4Pallet>(x => x.PalletNumber)
            .PutRequired(x => x.DepotCode, new DefaultRelationship(new L5Depot()));
}

file sealed class L5DepotProvider : DeepChainProviderBase
{
    public L5DepotProvider() =>
        this.Template = new MasterTemplate<L5Depot>(x => x.DepotId)
            .WithMockIdGenerator(new DepotIdGenerator())
            .Put(x => x.Region, new Net.NowhereAtAll.Xfty.Values.LiteralExpression("EMEA"))
            .PutRequired(x => x.ManifestDocId, new DefaultRelationship(new L6Manifest()));
}

file sealed class L6ManifestProvider : DeepChainProviderBase
{
    public L6ManifestProvider() =>
        this.Template = new MasterTemplate<L6Manifest>(x => x.ManifestId)
            .WithMockIdGenerator(new DatestampedIdGenerator("MAN"))
            .PutRequired(x => x.RouteTicket, new DefaultRelationship(new L7Route()));
}

file sealed class L7RouteProvider : DeepChainProviderBase
{
    public L7RouteProvider() =>
        this.Template = new MasterTemplate<L7Route>(x => x.TicketNo)
            .PutRequired(x => x.RegionSlug, new DefaultRelationship(new L8Region()));
}

file sealed class L8RegionProvider : DeepChainProviderBase
{
    public L8RegionProvider() =>
        this.Template = new MasterTemplate<L8Region>(x => x.Slug)
            .WithMockIdGenerator(new ZeroPaddedSlugGenerator())
            .PutRequired(x => x.TenantUuid, new DefaultRelationship(new L9Tenant()));
}

file sealed class L9TenantProvider : DeepChainProviderBase
{
    public L9TenantProvider() =>
        this.Template = new MasterTemplate<L9Tenant>(x => x.Uuid);
}