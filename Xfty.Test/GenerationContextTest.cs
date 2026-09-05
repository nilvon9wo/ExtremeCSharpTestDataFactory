using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Test;

/// <summary>
/// Proves <see cref="GenerationContext"/>'s constructor guards/defaults and
/// the ForRecord()/ForValueField()/SiblingValue() derivation chain - reading
/// an already-resolved sibling, and refusing a still-pending one loudly
/// rather than with a misleading null. ForRelated() and the batched-insert
/// flag wait on the ancestor-generation engine, not yet ported.
/// </summary>
public class GenerationContextTest
{
    private static readonly IProviderLookup Lookup = Substitute.For<IProviderLookup>();
    private static readonly PropertyInfo SiteField = Field.Of<Account>(nameof(Account.Site));
    private static readonly PropertyInfo TypeField = Field.Of<Account>(nameof(Account.Type));
    private static readonly PropertyInfo DescriptionField = Field.Of<Account>(nameof(Account.Description));

    private static GenerationContext Context(InsertMode? mode, InsertInclusivity? inclusivity) =>
        new(Lookup, mode, inclusivity);

    [Fact]
    public void Constructor_WhenGivenARunConfiguration_KeepsIt()
    {
        // Arrange - nothing to arrange

        // Act
        GenerationContext context = Context(InsertMode.Now, InsertInclusivity.All);

        // Assert
        Assert.Equal(Lookup, context.ProviderLookup);
        Assert.Equal(InsertMode.Now, context.InsertMode);
        Assert.Equal(InsertInclusivity.All, context.Inclusivity);
    }

    [Fact]
    public void Constructor_WhenTheProviderLookupIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => new GenerationContext(null!, InsertMode.Mock, InsertInclusivity.None));

        // Assert
        Assert.Contains("Provider Lookup", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenTheModeIsNull_DefaultsToNever()
    {
        // Arrange - nothing to arrange

        // Act
        GenerationContext context = Context(null, null);

        // Assert
        Assert.Equal(InsertMode.Never, context.InsertMode);
    }

    [Fact]
    public void Constructor_WhenTheInclusivityIsNull_DefaultsToNone()
    {
        // Arrange - nothing to arrange

        // Act
        GenerationContext context = Context(null, null);

        // Assert
        Assert.Equal(InsertInclusivity.None, context.Inclusivity);
    }

    [Fact]
    public void Constructor_HasNoPerRecordState()
    {
        // Arrange - nothing to arrange

        // Act
        GenerationContext context = Context(InsertMode.Mock, InsertInclusivity.None);

        // Assert
        Assert.Null(context.RecordBeingBuilt);
        Assert.Null(context.BundleSoFar);
        Assert.Equal(-1, context.RowIndex);
    }

    [Fact]
    public void SiblingValue_WhenTheSiblingIsAlreadyResolved_ReturnsItsValue()
    {
        // Arrange
        Account record = new() { Name = "sib", Site = "HQ" };
        GenerationContext atField = Context(InsertMode.Mock, InsertInclusivity.None)
            .ForRecord(record, new Bundle(), 0)
            .ForValueField(DescriptionField, new HashSet<PropertyInfo> { TypeField });

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
        GenerationContext atField = Context(InsertMode.Mock, InsertInclusivity.None)
            .ForRecord(record, new Bundle(), 0)
            .ForValueField(DescriptionField, new HashSet<PropertyInfo> { TypeField });

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
        GenerationContext baseContext = Context(InsertMode.Mock, InsertInclusivity.None);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() => baseContext.SiblingValue(SiteField));

        // Assert
        Assert.Contains("context-aware value is being generated", thrown.Message);
    }
}
