using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Demo;

namespace Net.Nowhereatall.Xfty.Core.Test;

/// <summary>
/// Proves <see cref="GenerationContext"/>'s currently-ported surface:
/// SiblingValue - reading an already-resolved sibling, and refusing a
/// still-pending one loudly rather than with a misleading null. The
/// constructor guards, forRelated()/forRecord()/withForcedRelationshipPaths()
/// derivation transforms, and the batched-insert flag all wait on fields this
/// port doesn't have yet (see GenerationContext.cs).
/// </summary>
public class GenerationContextTest
{
    private static readonly PropertyInfo SiteField = Field.Of<Account>(nameof(Account.Site));
    private static readonly PropertyInfo TypeField = Field.Of<Account>(nameof(Account.Type));
    private static readonly PropertyInfo DescriptionField = Field.Of<Account>(nameof(Account.Description));

    [Fact]
    public void SiblingValue_WhenTheSiblingIsAlreadyResolved_ReturnsItsValue()
    {
        // Arrange
        Account record = new() { Name = "sib", Site = "HQ" };
        HashSet<PropertyInfo> pending = [TypeField];
        GenerationContext atField = new(record, new ValueFieldPass(DescriptionField, pending));

        // Act
        object? siteValue = atField.SiblingValue(SiteField);

        // Assert
        Assert.Equal("HQ", siteValue);
    }

    [Fact]
    public void SiblingValue_WhenTheSiblingIsStillPending_Throws()
    {
        // Arrange
        Account record = new() { Name = "sib", Site = "HQ" };
        HashSet<PropertyInfo> pending = [TypeField];
        GenerationContext atField = new(record, new ValueFieldPass(DescriptionField, pending));

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => atField.SiblingValue(TypeField));

        // Assert - Account.Type is still pending, not a misleading null
        Assert.Contains("Type", thrown.Message);
        Assert.Contains("Description", thrown.Message);
    }

    [Fact]
    public void SiblingValue_WhenRunOutsideTheValuePass_Throws()
    {
        // Arrange
        GenerationContext baseContext = new(recordBeingBuilt: null, valueFieldPass: null);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => baseContext.SiblingValue(SiteField));

        // Assert
        Assert.Contains("context-aware value is being generated", thrown.Message);
    }
}
