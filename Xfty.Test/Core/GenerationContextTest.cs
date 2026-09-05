using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves GenerationContext's constructor guards/defaults and the
/// ForRelated()/ForRecord()/ForValueField() derivation transforms. Pure
/// in-memory state, no persistence.
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

    // ForRelated() --------------------------------------------------

    [Fact]
    public void ForRelated_WhenTheModeIsRelatedOnly_BecomesNow()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.RelatedOnly, InsertInclusivity.Required);

        // Act
        GenerationContext related = baseContext.ForRelated();

        // Assert
        Assert.Equal(InsertMode.Now, related.InsertMode); // ancestors of a RelatedOnly run are inserted
        Assert.Equal(InsertInclusivity.Required, related.Inclusivity); // inclusivity is unchanged
    }

    [Fact]
    public void ForRelated_WhenTheInclusivityIsPreventCascade_BecomesNone()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.Mock, InsertInclusivity.PreventCascade);

        // Act
        GenerationContext related = baseContext.ForRelated();

        // Assert
        Assert.Equal(InsertInclusivity.None, related.Inclusivity); // the cascade stops one level down
        Assert.Equal(InsertMode.Mock, related.InsertMode); // insert mode is unchanged
    }

    [Fact]
    public void ForRelated_ForAnyOtherModeAndInclusivity_CarriesThemThrough()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.Now, InsertInclusivity.All);

        // Act
        GenerationContext related = baseContext.ForRelated();

        // Assert
        Assert.Equal(InsertMode.Now, related.InsertMode);
        Assert.Equal(InsertInclusivity.All, related.Inclusivity);
        Assert.Equal(Lookup, related.ProviderLookup); // the Provider Lookup is always carried through
    }

    [Fact]
    public void ForRelated_ClearsAnyPerRecordState()
    {
        // Arrange
        GenerationContext scoped = Context(InsertMode.Mock, InsertInclusivity.All).ForRecord(new Account(), new Bundle(), 0);

        // Act
        GenerationContext related = scoped.ForRelated();

        // Assert
        Assert.Null(related.RecordBeingBuilt); // descending into ancestors drops the current record
        Assert.Equal(-1, related.RowIndex);
    }

    // ForRecord() --------------------------------------------------

    [Fact]
    public void ForRecord_ScopesToOneRecordAndKeepsTheRunSettings()
    {
        // Arrange
        Account record = new() { Name = "ctx" };
        Bundle bundle = new();

        // Act
        GenerationContext scoped = Context(InsertMode.Now, InsertInclusivity.Required).ForRecord(record, bundle, 2);

        // Assert
        Assert.Equal(record, scoped.RecordBeingBuilt);
        Assert.Same(bundle, scoped.BundleSoFar);
        Assert.Equal(2, scoped.RowIndex);
        Assert.Equal(InsertMode.Now, scoped.InsertMode); // the run settings are unchanged
        Assert.Equal(InsertInclusivity.Required, scoped.Inclusivity);
        Assert.Equal(Lookup, scoped.ProviderLookup);
    }

    // WithForcedRelationshipPaths() -----------------------------

    [Fact]
    public void WithForcedRelationshipPaths_WhenGivenNull_TolerantlyKeepsAnEmptyList()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.Mock, InsertInclusivity.None);

        // Act
        GenerationContext context = baseContext.WithForcedRelationshipPaths(null);

        // Assert
        Assert.NotNull(context.ForcedRelationshipPaths);
        Assert.Empty(context.ForcedRelationshipPaths);
    }

    // ForBatchedInsert() - the flag is carried through every derivation --

    [Fact]
    public void ForBatchedInsert_OnAPlainContext_TheFlagIsFalse()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.Never, InsertInclusivity.All);

        // Act
        bool pending = baseContext.BatchedInsertPending;

        // Assert
        Assert.False(pending); // a plain run is not a batched-insert run
    }

    [Fact]
    public void ForBatchedInsert_SetsTheFlag()
    {
        // Arrange
        GenerationContext baseContext = Context(InsertMode.Never, InsertInclusivity.All);

        // Act
        bool pending = baseContext.ForBatchedInsert().BatchedInsertPending;

        // Assert
        Assert.True(pending);
    }

    [Fact]
    public void WithForcedRelationshipPaths_CarriesTheBatchedInsertFlagThrough()
    {
        // Arrange
        GenerationContext batched = BatchedContext();

        // Act
        GenerationContext derived = batched.WithForcedRelationshipPaths(null);

        // Assert
        Assert.True(derived.BatchedInsertPending);
    }

    [Fact]
    public void ForRelated_CarriesTheBatchedInsertFlagDownToAncestors()
    {
        // Arrange
        GenerationContext batched = BatchedContext();

        // Act
        GenerationContext derived = batched.ForRelated();

        // Assert
        Assert.True(derived.BatchedInsertPending);
    }

    [Fact]
    public void ForRecord_CarriesTheBatchedInsertFlagIntoTheValuePass()
    {
        // Arrange
        GenerationContext batched = BatchedContext();

        // Act
        GenerationContext derived = batched.ForRecord(new Account(), new Bundle(), 0);

        // Assert
        Assert.True(derived.BatchedInsertPending);
    }

    private static GenerationContext BatchedContext() => Context(InsertMode.Never, InsertInclusivity.All).ForBatchedInsert();
}
