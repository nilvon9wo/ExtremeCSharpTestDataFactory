using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Enrichment;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Enrichment;

/// <summary>Proves ForcedValues - placing the config's forced scalars on the injector for a position. In-memory; the injector round-trip is exercised, no persistence.</summary>
public class ForcedValuesTest
{
    [Fact]
    public void ApplyRecordValues_AppliesOnRecordScalars()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing().InjectValue(Field.Of<Contact>(x => x.Birthdate), new DateTime(2020, 1, 1));
        RecordInjector injector = RecordInjector.Inject([new Contact { LastName = "X" }]);

        // Act
        new ForcedValues(config).ApplyRecordValues(injector, 1);

        // Assert
        Assert.Equal(new DateTime(2020, 1, 1), ((Contact)injector.Result()[0]).Birthdate);
    }

    [Fact]
    public void ApplyAncestorValues_AtAMatchingAncestorPosition_AppliesTheValue()
    {
        // Arrange - InjectValue(path) targets the record at path's relationship prefix
        List<System.Reflection.PropertyInfo> pathToField = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)];
        InjectConfig config = InjectConfig.Nothing().InjectValue(pathToField, 5000m);
        RecordInjector injector = RecordInjector.Inject([new Account { Name = "A" }]);

        // Act
        new ForcedValues(config).ApplyAncestorValues(injector, [Field.Of<Contact>(x => x.AccountId)], 1);

        // Assert
        Assert.Equal(5000m, ((Account)injector.Result()[0]).AnnualRevenue);
    }

    [Fact]
    public void ApplyAncestorValues_AtANonMatchingPosition_AppliesNothing()
    {
        // Arrange
        List<System.Reflection.PropertyInfo> pathToField = [Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)];
        InjectConfig config = InjectConfig.Nothing().InjectValue(pathToField, 5000m);
        RecordInjector injector = RecordInjector.Inject([new Account { Name = "A" }]);

        // Act
        new ForcedValues(config).ApplyAncestorValues(injector, [Field.Of<Contact>(x => x.ReportsToId)], 1);

        // Assert
        Assert.Null(((Account)injector.Result()[0]).AnnualRevenue);
    }

    [Fact]
    public void ApplyChildValues_AtAMatchingChildPosition_AppliesTheValueToEveryRow()
    {
        // Arrange - InjectChildValue(childField, leafField, literal) - every child gets it
        InjectConfig config = InjectConfig.Nothing()
            .InjectChildValue(Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), "shared");
        RecordInjector injector = RecordInjector.Inject([new Contact { LastName = "A" }, new Contact { LastName = "B" }]);

        // Act
        new ForcedValues(config).ApplyChildValues(injector, [Field.Of<Contact>(x => x.AccountId)], 2);

        // Assert
        List<object> enriched = injector.Result();
        Assert.Equal("shared", ((Contact)enriched[0]).Department);
        Assert.Equal("shared", ((Contact)enriched[1]).Department);
    }

    [Fact]
    public void ApplyChildValues_WithAnExpression_ResolvesItFreshPerChild()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), new IncrementingStringExpression("n"));
        RecordInjector injector = RecordInjector.Inject([new Contact { LastName = "A" }, new Contact { LastName = "B" }]);

        // Act
        new ForcedValues(config).ApplyChildValues(injector, [Field.Of<Contact>(x => x.AccountId)], 2);

        // Assert
        List<object> enriched = injector.Result();
        Assert.Equal("n 1", ((Contact)enriched[0]).Department);
        Assert.Equal("n 2", ((Contact)enriched[1]).Department); // each child gets its own resolution
    }

    [Fact]
    public void ApplyChildValues_WithAPerRowList_AppliesOneValuePerChild()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing().InjectChildValue(
            Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), new List<object?> { "first", "second" });
        RecordInjector injector = RecordInjector.Inject([new Contact { LastName = "A" }, new Contact { LastName = "B" }]);

        // Act
        new ForcedValues(config).ApplyChildValues(injector, [Field.Of<Contact>(x => x.AccountId)], 2);

        // Assert
        List<object> enriched = injector.Result();
        Assert.Equal("first", ((Contact)enriched[0]).Department);
        Assert.Equal("second", ((Contact)enriched[1]).Department);
    }

    [Fact]
    public void AssertEveryPathWasReached_WhenAnAncestorValueNeverMatched_Throws()
    {
        // Arrange - the path was never visited (ApplyAncestorValues never called for it)
        InjectConfig config = InjectConfig.Nothing()
            .InjectValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.AnnualRevenue)], 5000m);
        ForcedValues forcedValues = new(config);

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(forcedValues.AssertEveryPathWasReached);

        // Assert - the error names the unreached path
        Assert.Contains("InjectValue", thrown.Message);
    }

    [Fact]
    public void AssertEveryPathWasReached_WhenEveryPathMatched_DoesNotThrow()
    {
        // Arrange
        InjectConfig config = InjectConfig.Nothing()
            .InjectChildValue(Field.Of<Contact>(x => x.AccountId), Field.Of<Contact>(x => x.Department), "x");
        RecordInjector injector = RecordInjector.Inject([new Contact { LastName = "A" }]);
        ForcedValues forcedValues = new(config);
        forcedValues.ApplyChildValues(injector, [Field.Of<Contact>(x => x.AccountId)], 1);

        // Act
        Exception? thrown = Record.Exception(forcedValues.AssertEveryPathWasReached);

        // Assert - no exception, every path was reached
        Assert.Null(thrown);
    }
}
