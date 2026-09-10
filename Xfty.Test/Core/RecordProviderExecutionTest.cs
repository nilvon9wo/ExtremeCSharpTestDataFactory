using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Persistence;
using NSubstitute;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves <see cref="RecordProviderExecution"/> - the Supply*() pipeline -
/// against hand-built <see cref="RecordProviderPlan"/>s, with no fluent API in
/// the way: the quantity/template selection, and the persistence branching for
/// each insert mode. <see cref="RecordProviderApiTest"/> covers the fluent
/// surface that produces these plans.
/// </summary>
public sealed class RecordProviderExecutionTest : IDisposable
{
    private static readonly DefaultProviderLookup Lookup = new();
    private static readonly AccountDataProvider AccountOutlet = new();

    public void Dispose()
    {
        DeferredInserter.ResetForTesting();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SupplyBundle_WithABarePlan_GeneratesTheOnePrimary()
    {
        // Arrange
        RecordProviderExecution execution = new(PlanFor(InsertMode.Mock));

        // Act
        Bundle bundle = await execution.SupplyBundle().ConfigureAwait(true);

        // Assert
        _ = Assert.Single(bundle.GetList(AccountOutlet.PrimaryTargetField)!);
    }

    [Fact]
    public async Task SupplyList_WithAQuantity_MultipliesTheBlankTemplate()
    {
        // Arrange
        RecordProviderExecution execution = new(PlanFor(InsertMode.Mock) with { QuantityPerTemplate = 4 });

        // Act
        List<object> results = await execution.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public async Task SupplyList_WithOverrideTemplates_GeneratesOnePerTemplate()
    {
        // Arrange
        List<object> templates = [new Account { Name = "One" }, new Account { Name = "Two" }];
        RecordProviderExecution execution = new(PlanFor(InsertMode.Mock) with { OverrideTemplates = templates });

        // Act
        List<object> results = await execution.SupplyList().ConfigureAwait(true);

        // Assert
        Assert.Equal(["One", "Two"], results.Cast<Account>().Select(account => account.Name));
    }

    [Fact]
    public async Task SupplyBundle_InMockMode_AssignsAPlaceholderIdWithoutAGateway()
    {
        // Arrange
        RecordProviderExecution execution = new(PlanFor(InsertMode.Mock));

        // Act
        Bundle bundle = await execution.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.NotNull(((Account)bundle.GetList(AccountOutlet.PrimaryTargetField)![0]).Id);
    }

    [Fact]
    public async Task SupplyBundle_InNeverMode_LeavesThePrimaryWithoutAnId()
    {
        // Arrange
        RecordProviderExecution execution = new(PlanFor(InsertMode.Never));

        // Act
        Bundle bundle = await execution.SupplyBundle().ConfigureAwait(true);

        // Assert
        Assert.Null(((Account)bundle.GetList(AccountOutlet.PrimaryTargetField)![0]).Id);
    }

    [Fact]
    public async Task SupplyBundle_InNowModeWithDepthBatching_InsertsThroughTheGateway()
    {
        // Arrange
        IPersistenceGateway gateway = Substitute.For<IPersistenceGateway>();
        RecordProviderPlan plan = PlanFor(InsertMode.Now) with
        {
            DepthBatched = true,
            PersistenceGateway = gateway,
        };
        RecordProviderExecution execution = new(plan);

        // Act
        _ = await execution.SupplyBundle().ConfigureAwait(true);

        // Assert
        _ = gateway.Received().Insert(Arg.Any<List<object>>(), Arg.Any<System.Reflection.PropertyInfo>());
    }

    [Fact]
    public async Task SupplyBundle_InDeferredMode_BuildsAStructuralGraphWithNoGateway()
    {
        // Arrange
        RecordProviderExecution execution = new(PlanFor(InsertMode.Deferred));

        // Act
        Bundle bundle = await execution.SupplyBundle().ConfigureAwait(true);

        // Assert - a flushable graph, nothing inserted (no gateway was needed)
        Assert.NotEmpty(DeferredInsertBuffer.Flatten(bundle).Records());
    }

    private static RecordProviderPlan PlanFor(InsertMode mode) =>
        new(
            Lookup,
            typeof(Account),
            AccountOutlet,
            mode,
            InsertInclusivity.None,
            QuantityPerTemplate: 1,
            OverrideTemplates: null,
            PersistenceGateway: null,
            UnsetFieldFiller: null,
            AncestorCyclesAllowed: false,
            ExcludePrimaryIds: false,
            DepthBatched: false,
            ForceStructuralChildGeneration: false,
            new RecordProviderTemplateConfig(AccountOutlet.MasterTemplate.Copy),
            new RecordProviderChildConfig());
}