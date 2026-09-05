using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>Proves QueryableShapeValidator - the SOQL-shape rail on an InjectConfig. No persistence.</summary>
public class QueryableShapeValidatorTest
{
    [Fact]
    public void Validate_WhenTheConfigStaysWithinTheSoqlLimits_Passes()
    {
        // Arrange
        InjectConfig config = InjectConfig.Everything().ParentDepth(5).ChildDepth(1);

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - no throw
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenParentDepthExceedsTheSoqlLimit_Throws()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllParents().ParentDepth(6);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert
        Assert.Contains("BreakSoqlLimits", thrown.Message);
    }

    [Fact]
    public void Validate_WhenChildDepthExceedsOneWithoutBreakSoqlLimits_Throws()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(2);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => QueryableShapeValidator.Validate(config));

        // Assert - the message points at the escape hatch
        Assert.Contains("BreakSoqlLimits", thrown.Message);
    }

    [Fact]
    public void Validate_WhenChildDepthExceedsOneWithBreakSoqlLimits_Passes()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllChildren().ChildDepth(3).BreakSoqlLimits();

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - nested subqueries are allowed once the ceiling is lifted
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenAnInjectParentPathIsLongerThanTheSoqlLimit_Throws()
    {
        // Arrange
        List<PropertyInfo> sixHops = [
            Field.Of<Account>(nameof(Account.ParentId)), Field.Of<Account>(nameof(Account.ParentId)), Field.Of<Account>(nameof(Account.ParentId)),
            Field.Of<Account>(nameof(Account.ParentId)), Field.Of<Account>(nameof(Account.ParentId)), Field.Of<Account>(nameof(Account.ParentId)),
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
            [Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Case>(nameof(Case.ContactId)), Field.Of<Case>(nameof(Case.Subject))], "x");

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
            .BreakSoqlLimits()
            .InjectChildValue([Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Case>(nameof(Case.ContactId)), Field.Of<Case>(nameof(Case.Subject))], "x");

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - the path fits once childDepth allows two child levels
        Assert.Null(thrown);
    }

    [Fact]
    public void Validate_WhenBreakSoqlLimitsIsSet_AllowsAnOverDeepConfig()
    {
        // Arrange
        InjectConfig config = InjectConfig.AllParents().ParentDepth(20).BreakSoqlLimits();

        // Act
        Exception? thrown = Record.Exception(() => QueryableShapeValidator.Validate(config));

        // Assert - no throw once the ceiling is lifted
        Assert.Null(thrown);
    }
}
