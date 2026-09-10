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
/// One POCO (<c>Node</c>), eight Provider variants of it keyed by a
/// discriminator field, and a mock-Id generator that differs per variant -
/// some the built-in <c>"mock-N"</c>, some a variant-specific shape (a
/// prefix, a datestamp, a zero-padded counter, a value read off the record).
/// A single <c>Supply</c> walks an eight-generation ancestor chain where each
/// generation resolves to a different variant, and each gets its variant's Id
/// shape - proving the generator is picked per resolved Provider, not per
/// type.
/// </summary>
public class FlavouredMockIdHierarchyTest
{
    private static IProviderLookup ChainLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get<Node>()] = new RootNodeProvider(),                    // plain key - default Id gen
            [KindKey("alpha")] = new AlphaNodeProvider(),                        // custom: "ALPHA-N"
            [KindKey("beta")] = new BetaNodeProvider(),                          // discriminated, but still default Id gen
            [KindKey("gamma")] = new GammaNodeProvider(),                        // custom: reads Region off the record
            [KindKey("delta")] = new DeltaNodeProvider(),                        // default Id gen
            [KindKey("epsilon")] = new EpsilonNodeProvider(),                    // custom: datestamped
            [KindKey("zeta")] = new ZetaNodeProvider(),                          // custom: zero-padded
            [KindKey("terminal")] = new TerminalNodeProvider(),                  // custom: "end-N", chain stops here
        });

    private static ILookupKey KindKey(string kind) => DiscriminatorLookupKey.Get<Node>(x => x.Kind, kind);

    [Fact]
    public async Task SupplyBundle_ForAnEightGenerationChainOfOneType_GivesEachGenerationItsOwnVariantsIdShape()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Node), ChainLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Sanity Check - walk one Node per generation down the ParentRef chain
        Node root = (Node)bundle.GetList<Node>(x => x.NodeRef)![0];
        Node alpha = (Node)NextDown(bundle, out Bundle alphaBundle);
        Node beta = (Node)NextDown(alphaBundle, out Bundle betaBundle);
        Node gamma = (Node)NextDown(betaBundle, out Bundle gammaBundle);
        Node delta = (Node)NextDown(gammaBundle, out Bundle deltaBundle);
        Node epsilon = (Node)NextDown(deltaBundle, out Bundle epsilonBundle);
        Node zeta = (Node)NextDown(epsilonBundle, out Bundle zetaBundle);
        Node terminal = (Node)NextDown(zetaBundle, out _);

        // Assert - each generation's Id is shaped by its own variant's generator
        Assert.Matches("^mock-[0-9]+$", root.NodeRef!);        // plain key -> DefaultMockIdGenerator
        Assert.Matches("^ALPHA-[0-9]+$", alpha.NodeRef!);
        Assert.Matches("^mock-[0-9]+$", beta.NodeRef!);        // discriminated variant, no custom generator
        Assert.Matches("^g:EMEA:[0-9]+$", gamma.NodeRef!);     // built from the record's own Region
        Assert.Matches("^mock-[0-9]+$", delta.NodeRef!);
        Assert.Matches(@"^EPS-[0-9]{8}-[0-9]+$", epsilon.NodeRef!);
        Assert.Matches("^z_[0-9]{6}$", zeta.NodeRef!);
        Assert.Matches("^end-[0-9]+$", terminal.NodeRef!);

        // Assert - each generation resolved to the variant its parent's template asked for
        Assert.Equal("alpha", alpha.Kind);
        Assert.Equal("terminal", terminal.Kind);

        // Assert - every foreign key points at its parent's real key
        Assert.Equal(alpha.NodeRef, root.ParentRef);
        Assert.Equal(beta.NodeRef, alpha.ParentRef);
        Assert.Equal(gamma.NodeRef, beta.ParentRef);
        Assert.Equal(delta.NodeRef, gamma.ParentRef);
        Assert.Equal(epsilon.NodeRef, delta.ParentRef);
        Assert.Equal(zeta.NodeRef, epsilon.ParentRef);
        Assert.Equal(terminal.NodeRef, zeta.ParentRef);
        Assert.Null(terminal.ParentRef); // chain stops
    }

    [Fact]
    public async Task Supply_ForTheSameTypeUnderDifferentOverrideTemplates_UsesEachMatchedVariantsGenerator()
    {
        // Arrange - same POCO, two calls, each override template steering a different variant
        RecordProvider alphaCall = new RecordProvider(new Node { Kind = "alpha" }, ChainLookup())
            .SetInclusivity(InsertInclusivity.None)
            .SetInsertMode(InsertMode.Mock);
        RecordProvider zetaCall = new RecordProvider(new Node { Kind = "zeta" }, ChainLookup())
            .SetInclusivity(InsertInclusivity.None)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Node fromAlpha = (Node)await alphaCall.Supply().ConfigureAwait(true);
        Node fromZeta = (Node)await zetaCall.Supply().ConfigureAwait(true);

        // Assert
        Assert.Matches("^ALPHA-[0-9]+$", fromAlpha.NodeRef!);
        Assert.Matches("^z_[0-9]{6}$", fromZeta.NodeRef!);
    }

    // Helpers -----------------------------------------------------------

    private static object NextDown(Bundle bundle, out Bundle childBundle)
    {
        childBundle = bundle.GetBundle<Node>(x => x.ParentRef)!;
        return childBundle.GetList<Node>(x => x.NodeRef)![0];
    }
}

file sealed record Node
{
    public string? NodeRef { get; init; }

    public string? Kind { get; init; }

    public string? Region { get; init; }

    public string? ParentRef { get; init; }
}

file sealed class PrefixCounterIdGenerator(string prefix) : IMockIdGenerator
{
    private int count;

    public object NextId(MockIdContext context) => $"{prefix}{++this.count}";
}

file sealed class DatestampedIdGenerator(string prefix) : IMockIdGenerator
{
    private int count;

    public object NextId(MockIdContext context) => $"{prefix}-{new DateTime(2024, 5, 6):yyyyMMdd}-{++this.count}";
}

file sealed class ZeroPaddedIdGenerator : IMockIdGenerator
{
    private int count;

    public object NextId(MockIdContext context) => $"z_{++this.count:D6}";
}

file sealed class RegionScopedIdGenerator : IMockIdGenerator
{
    private int count;

    public object NextId(MockIdContext context)
    {
        string region = (string?)context.RecordType.GetProperty("Region")!.GetValue(context.Record) ?? "??";
        return $"g:{region}:{++this.count}";
    }
}

file abstract class NodeProviderBase : IRecordProvider
{
    protected MasterTemplate Template { get; set; } = null!;

    public PropertyInfo PrimaryTargetField => this.Template.PrimaryTargetField;

    public MasterTemplate MasterTemplate => this.Template;

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.Template, templateRecords);

    protected static MasterTemplate<Node> Chain(string kind, string nextKind) =>
        new MasterTemplate<Node>(x => x.NodeRef)
            .Put(x => x.Kind, new LiteralExpression(kind))
            .PutRequired(x => x.ParentRef, new DefaultRelationship(new Node { Kind = nextKind }));
}

file sealed class RootNodeProvider : NodeProviderBase
{
    public RootNodeProvider() => this.Template = Chain("root", "alpha");
}

file sealed class AlphaNodeProvider : NodeProviderBase
{
    public AlphaNodeProvider() => this.Template = Chain("alpha", "beta").WithMockIdGenerator(new PrefixCounterIdGenerator("ALPHA-"));
}

file sealed class BetaNodeProvider : NodeProviderBase
{
    public BetaNodeProvider() => this.Template = Chain("beta", "gamma");
}

file sealed class GammaNodeProvider : NodeProviderBase
{
    public GammaNodeProvider() =>
        this.Template = Chain("gamma", "delta")
            .Put(x => x.Region, new LiteralExpression("EMEA"))
            .WithMockIdGenerator(new RegionScopedIdGenerator());
}

file sealed class DeltaNodeProvider : NodeProviderBase
{
    public DeltaNodeProvider() => this.Template = Chain("delta", "epsilon");
}

file sealed class EpsilonNodeProvider : NodeProviderBase
{
    public EpsilonNodeProvider() => this.Template = Chain("epsilon", "zeta").WithMockIdGenerator(new DatestampedIdGenerator("EPS"));
}

file sealed class ZetaNodeProvider : NodeProviderBase
{
    public ZetaNodeProvider() => this.Template = Chain("zeta", "terminal").WithMockIdGenerator(new ZeroPaddedIdGenerator());
}

file sealed class TerminalNodeProvider : NodeProviderBase
{
    public TerminalNodeProvider() =>
        this.Template = new MasterTemplate<Node>(x => x.NodeRef)
            .Put(x => x.Kind, new LiteralExpression("terminal"))
            .WithMockIdGenerator(new PrefixCounterIdGenerator("end-"));
}
