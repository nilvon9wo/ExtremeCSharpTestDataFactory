using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>
/// Proves EnrichmentSelection - "does this config want the ancestor/child/
/// inverse here?". Pure in-memory, no DML/SOQL. Parameterised: the [Theory]
/// methods are data rows; the runners hold the AAA.
/// </summary>
public class EnrichmentSelectionTest
{
    private static List<PropertyInfo> AccountPath() => [Field.Of<Contact>(x => x.AccountId)];

    private static List<PropertyInfo> AccountParent() => [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)];

    private static List<PropertyInfo> Owner() => [Field.Of<Contact>(x => x.ReportsToId)];

    [Fact]
    public void WantsAncestor_FromNothing_IsFalse() => AssertWantsAncestor(InjectConfig.Nothing(), AccountPath(), false);

    [Fact]
    public void WantsAncestor_FromAllParents_IsTrueForAnyUpwardPath() => AssertWantsAncestor(InjectConfig.AllParents(), AccountParent(), true);

    [Fact]
    public void WantsAncestor_FromAllParentsWhenTheWalkHasTurnedDownward_IsTrue() => AssertWantsAncestor(InjectConfig.AllParents(), null, true);

    [Fact]
    public void WantsAncestor_FromNothingWhenTheWalkHasTurnedDownward_IsFalse() =>
        AssertWantsAncestor(InjectConfig.Nothing().InjectParent(AccountPath()), null, false);

    [Fact]
    public void WantsAncestor_ForAPrefixOfAnInjectParentLeaf_IsTrue() =>
        AssertWantsAncestor(InjectConfig.Nothing().InjectParent(AccountParent()), AccountPath(), true);

    [Fact]
    public void WantsAncestor_ForAnInjectParentLeafItself_IsTrue() =>
        AssertWantsAncestor(InjectConfig.Nothing().InjectParent(AccountParent()), AccountParent(), true);

    [Fact]
    public void WantsAncestor_ForAnUnrelatedHop_IsFalse() =>
        AssertWantsAncestor(InjectConfig.Nothing().InjectParent(AccountParent()), Owner(), false);

    [Fact]
    public void WantsAncestor_WhenAPrefixIsExcluded_IsFalse() =>
        AssertWantsAncestor(InjectConfig.AllParents().ExcludeParent(AccountPath()), AccountParent(), false);

    private static void AssertWantsAncestor(InjectConfig config, List<PropertyInfo>? pathFromEntry, bool expected)
    {
        // Arrange
        EnrichmentSelection selection = new(config);

        // Act
        bool wanted = selection.WantsAncestor(pathFromEntry);

        // Assert
        Assert.Equal(expected, wanted);
    }

    [Fact]
    public void WantsInverse_FromAllChildren_IsTrue() => AssertWantsInverse(InjectConfig.AllChildren(), true);

    [Fact]
    public void WantsInverse_FromNothing_IsFalse() => AssertWantsInverse(InjectConfig.Nothing(), false);

    [Fact]
    public void WantsInverse_WhenTheRelationshipIsExcluded_IsFalse() =>
        AssertWantsInverse(InjectConfig.AllChildren().ExcludeChild(Field.Of<Contact>(x => x.AccountId)), false);

    private static void AssertWantsInverse(InjectConfig config, bool expected)
    {
        // Arrange
        EnrichmentSelection selection = new(config);

        // Act
        bool wanted = selection.WantsInverse(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal(expected, wanted);
    }

    private static List<PropertyInfo> RootPath() => [];

    private static List<PropertyInfo> OneChildHop() => [Field.Of<Contact>(x => x.AccountId)];

    [Fact]
    public void ChildFieldsOn_FromAllChildren_IncludesThePresentField() => AssertChildFieldsOnContains(InjectConfig.AllChildren(), RootPath(), true);

    [Fact]
    public void ChildFieldsOn_WhenExcluded_DropsTheField() =>
        AssertChildFieldsOnContains(InjectConfig.AllChildren().ExcludeChild(Field.Of<Contact>(x => x.AccountId)), RootPath(), false);

    [Fact]
    public void ChildFieldsOn_ForAnInjectChildFieldAtTheRoot_IncludesIt() =>
        AssertChildFieldsOnContains(InjectConfig.Nothing().InjectChild(Field.Of<Contact>(x => x.AccountId)), RootPath(), true);

    [Fact]
    public void ChildFieldsOn_ForAnInjectChildFieldNotAtTheRoot_ExcludesIt() =>
        AssertChildFieldsOnContains(InjectConfig.Nothing().InjectChild(Field.Of<Contact>(x => x.AccountId)), OneChildHop(), false);

    [Fact]
    public void ChildFieldsOn_ForAnInjectChildValuePathAtTheRoot_IncludesItsFirstHop() =>
        AssertChildFieldsOnContains(
            InjectConfig.Nothing().InjectChildValue(Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), "x"),
            RootPath(),
            true);

    private static void AssertChildFieldsOnContains(InjectConfig config, List<PropertyInfo> childPathHere, bool expectedToContain)
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account()]);
        _ = bundle.PutChild(Field.Of<Contact>(x => x.AccountId), new Bundle(), []);
        EnrichmentSelection selection = new(config);

        // Act
        HashSet<PropertyInfo> fields = selection.ChildFieldsOn(bundle, childPathHere);

        // Assert
        Assert.Equal(expectedToContain, fields.Contains(Field.Of<Contact>(x => x.AccountId)));
    }
}
