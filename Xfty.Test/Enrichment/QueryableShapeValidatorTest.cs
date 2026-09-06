using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Enrichment;

namespace Net.NowhereAtAll.Xfty.Test.Enrichment;

/// <summary>Proves QueryableShapeValidator - the queryable-shape rail on an InjectConfig. No persistence.</summary>
public class QueryableShapeValidatorTest
{
    [Fact]
    public void Validate_WhenTheConfigStaysWithinTheDefaultLimits_Passes()
    {
        // Arrange
        InjectConfig config = InjectConfig.Everything().ParentDepth(5).ChildDepth(1);

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - no throw
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenParentDepthExceedsTheDefaultLimit_Throws()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllParents().ParentDepth(6);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert
        Assert.Contains("AllowDeeperGraph", thrown.Message);
    }

    [Fact]
    public void Validate_WhenChildDepthExceedsOneWithoutAllowDeeperGraph_Throws()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(2);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert - the message points at the escape hatch
        Assert.Contains("AllowDeeperGraph", thrown.Message);
    }

    [Fact]
    public void Validate_WhenChildDepthExceedsOneWithAllowDeeperGraph_Passes()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(3).AllowDeeperGraph();

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - nested subqueries are allowed once the ceiling is lifted
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenAnInjectParentPathIsLongerThanTheDefaultLimit_Throws()
    {
        // Arrange
        List<PropertyInfo> sixHops = [
            Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.ParentId),
            Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.ParentId), Field.Of<Account>(x => x.ParentId),
        ];
        InjectConfig config = InjectConfig.Nothing().InjectParent(sixHops);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert
        Assert.NotNull(thrown);
    }

    [Fact]
    public void Validate_WhenAnInjectChildValuePathIsDeeperThanChildDepth_Throws()
    {
        // Arrange - a 2-child-hop path, but childDepth is still the default 1
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(
            [Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject)], "x");

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert - the message points at childDepth
        Assert.Contains("childDepth", thrown.Message);
    }

    [Fact]
    public void Validate_WhenAnInjectChildValuePathFitsWithinChildDepth_Passes()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing()
            .ChildDepth(2)
            .AllowDeeperGraph()
            .InjectChildValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Case>(x => x.ContactId), Field.Of<Case>(x => x.Subject)], "x");

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - the path fits once childDepth allows two child levels
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenAllowDeeperGraphIsSet_AllowsAnOverDeepConfig()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllParents().ParentDepth(20).AllowDeeperGraph();

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - no throw once the ceiling is lifted
        Assert.Null(thrown);
    }
}
