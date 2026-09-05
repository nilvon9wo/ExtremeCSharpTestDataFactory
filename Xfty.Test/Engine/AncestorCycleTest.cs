using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Engine;

/// <summary>
/// Proves ancestor-cycle detection in AncestorGenerator: a self-referential
/// relationship generates one level, a deeper same-key chain throws, and
/// AllowAncestorCycles() suppresses the guard. Mock mode - no persistence.
/// </summary>
public class AncestorCycleTest
{
    private static IProviderLookup SelfReferringLookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider> { [LookupKey.Get(typeof(Contact))] = new SelfReferringContactProvider() });

    [Fact]
    public void SupplyBundle_WithOneLevelOfSelfReference_StopsTheChainOnItsOwn()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), SelfReferringLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .IncludeOptional(Field.Of<Contact>(x => x.ReportsToId))
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Sanity Check
        Assert.NotNull(bundle.GetBundle(Field.Of<Contact>(x => x.ReportsToId))); // the manager Contact was generated

        // Assert - the manager Contact does not get its own manager, the chain stops
        Assert.Null(bundle.GetBundle(Field.Of<Contact>(x => x.ReportsToId))!.GetBundle(Field.Of<Contact>(x => x.ReportsToId)));
    }

    [Fact]
    public void SupplyBundle_WhenAForcedPathRepeatsTheSameRelationshipKey_Throws()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), SelfReferringLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .IncludeOptional([Field.Of<Contact>(x => x.ReportsToId), Field.Of<Contact>(x => x.ReportsToId)])
            .SetInsertMode(InsertMode.Mock);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(provider.SupplyBundle);

        // Assert - a deeper same-key chain must throw
        Assert.Contains("cycle", thrown.Message);
        Assert.Contains("ReportsToId", thrown.Message);
    }

    [Fact]
    public void SupplyBundle_WhenAllowAncestorCyclesIsSet_BuildsTheForcedDeepChainThenStops()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Contact), SelfReferringLookup())
            .SetInclusivity(InsertInclusivity.Required)
            .IncludeOptional([Field.Of<Contact>(x => x.ReportsToId), Field.Of<Contact>(x => x.ReportsToId)])
            .AllowAncestorCycles()
            .SetInsertMode(InsertMode.Mock);

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Bundle levelTwo = bundle.GetBundle(Field.Of<Contact>(x => x.ReportsToId))!.GetBundle(Field.Of<Contact>(x => x.ReportsToId))!;
        Assert.NotNull(levelTwo); // the two-hop forced path built a two-deep chain
        Assert.Null(levelTwo.GetBundle(Field.Of<Contact>(x => x.ReportsToId))); // and then it stops
    }
}

file sealed class SelfReferringContactProvider : IRecordProvider
{
    private MasterTemplate _template { get; } = new MasterTemplate(Field.Of<Contact>(x => x.Id))
        .Put(Field.Of<Contact>(x => x.LastName), new IncrementingStringExpression("Mgr"))
        .PutOptional(Field.Of<Contact>(x => x.ReportsToId), new DefaultRelationship(new Contact()));

    public PropertyInfo PrimaryTargetField => Field.Of<Contact>(x => x.Id);

    public MasterTemplate MasterTemplate => this._template;

    public Bundle CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this._template, templateRecords);
}
