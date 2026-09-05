using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>Proves InjectConfig - a plain fluent state carrier for bundle.Inject(field, config). No persistence.</summary>
public class InjectConfigTest
{
    [Fact]
    public void Nothing_StartsFromNeitherBreadth()
    {
        // Arrange
        // nothing to arrange

        // Act
        InjectConfig config = InjectConfig.Nothing();

        // Assert
        Assert.False(config.FromAllParents);
        Assert.False(config.FromAllChildren);
    }

    [Fact]
    public void AllParents_StartsFromParentsOnly()
    {
        // Arrange
        // nothing to arrange

        // Act
        InjectConfig config = InjectConfig.AllParents();

        // Assert
        Assert.True(config.FromAllParents);
        Assert.False(config.FromAllChildren);
    }

    [Fact]
    public void AllChildren_StartsFromChildrenOnly()
    {
        // Arrange
        // nothing to arrange

        // Act
        InjectConfig config = InjectConfig.AllChildren();

        // Assert
        Assert.False(config.FromAllParents);
        Assert.True(config.FromAllChildren);
    }

    [Fact]
    public void Everything_StartsFromBothBreadths()
    {
        // Arrange
        // nothing to arrange

        // Act
        InjectConfig config = InjectConfig.Everything();

        // Assert
        Assert.True(config.FromAllParents);
        Assert.True(config.FromAllChildren);
    }

    [Fact]
    public void Defaults_AreTheSoqlLimits()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();

        // Act
        int parent = config.ParentDepthLimit;

        // Assert
        Assert.Equal(InjectConfig.SoqlParentHops, parent);
        Assert.Equal(InjectConfig.SoqlChildDepth, config.ChildDepthLimit);
        Assert.False(config.SoqlLimitsLifted);
    }

    [Fact]
    public void InjectParent_AddsThePathAndReturnsTheConfig()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.ParentId)];

        // Act
        InjectConfig returned = config.InjectParent(path);

        // Assert - fluent
        Assert.Same(config, returned);
        Assert.Equal(path, config.IncludedParentPaths[0]);
    }

    [Fact]
    public void InjectChild_AddsTheChildField()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();

        // Act
        _ = config.InjectChild(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Contains(Field.Of<Contact>(x => x.AccountId), config.IncludedChildFields);
    }

    [Fact]
    public void ExcludeParent_AddsThePath()
    {
        // Arrange
        InjectConfig config = InjectConfig.Everything();
        List<PropertyInfo> path = [Field.Of<Case>(x => x.Origin)];

        // Act
        _ = config.ExcludeParent(path);

        // Assert
        Assert.Equal(path, config.ExcludedParentPaths[0]);
    }

    [Fact]
    public void ExcludeChild_AddsTheChildField()
    {
        // Arrange
        InjectConfig config = InjectConfig.Everything();

        // Act
        _ = config.ExcludeChild(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Contains(Field.Of<Contact>(x => x.AccountId), config.ExcludedChildFields);
    }

    [Fact]
    public void InjectValue_WithAFieldAndValue_RecordsItAgainstTheField()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();

        // Act
        _ = config.InjectValue(Field.Of<Account>(x => x.AnnualRevenue), 5000m);

        // Assert
        Assert.Equal(5000m, config.OnRecordValues[Field.Of<Account>(x => x.AnnualRevenue)]);
    }

    [Fact]
    public void InjectValue_WithAPathAndValue_AddsAnAncestorValue()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();
        List<PropertyInfo> pathToField = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)];

        // Act
        _ = config.InjectValue(pathToField, "Forced");

        // Assert
        Assert.Equal("Forced", config.AncestorValues[0].Value);
        Assert.Equal(Field.Of<Account>(x => x.Name), config.AncestorValues[0].TargetField());
    }

    [Fact]
    public void InjectChildValue_WithTwoFields_AddsAChildValueOnTheOneHopPath()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();

        // Act
        _ = config.InjectChildValue(Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), "note");

        // Assert
        Assert.Equal("note", config.ChildValues[0].Value);
        Assert.Equal(Field.Of<Contact>(x => x.Department), config.ChildValues[0].TargetField());
        Assert.Equal([Field.Of<Contact>(x => x.AccountId)], config.ChildValues[0].RelationshipPrefix());
    }

    [Fact]
    public void InjectChildValue_WithAPath_KeepsEveryHop()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing();
        List<PropertyInfo> path = [Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject)];

        // Act
        _ = config.InjectChildValue(path, "x");

        // Assert
        Assert.Equal(path, config.ChildValues[0].Path);
        Assert.Equal(Field.Of<Case>(x => x.Subject), config.ChildValues[0].TargetField());
    }

    [Fact]
    public void ParentDepth_OverridesTheLimit()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllParents();

        // Act
        _ = config.ParentDepth(2);

        // Assert
        Assert.Equal(2, config.ParentDepthLimit);
    }

    [Fact]
    public void ChildDepth_OverridesTheLimit()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllChildren();

        // Act
        _ = config.ChildDepth(3);

        // Assert
        Assert.Equal(3, config.ChildDepthLimit);
    }

    [Fact]
    public void BreakSoqlLimits_LiftsTheFlag()
    {
        // Arrange
        InjectConfig config = InjectConfig.Everything();

        // Act
        _ = config.BreakSoqlLimits();

        // Assert
        Assert.True(config.SoqlLimitsLifted);
    }
}
