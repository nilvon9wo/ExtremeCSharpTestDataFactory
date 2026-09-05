using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Test.Persistence;

/// <summary>
/// Proves IdMocker, which fabricates a unique placeholder identifier without
/// touching a database. The mocked Id is a plain "mock-N" string with no
/// per-type structure at all - there is no database-specific Id shape to
/// validate against, and no query layer here to prove "not a real record"
/// against either.
/// </summary>
public class IdMockerTest
{
    [Fact]
    public void GenerateId_IsUniqueAcrossManyCalls()
    {
        // Arrange
        HashSet<string> generated = [];

        // Act
        for (int i = 0; i < 100; i++)
        {
            _ = generated.Add(IdMocker.GenerateId());
        }

        // Assert - every fabricated Id is distinct
        Assert.Equal(100, generated.Count);
    }

    [Fact]
    public void AddId_PopulatesTheIdFieldAndReturnsTheSameInstance()
    {
        // Arrange
        Account record = new() { Name = "Anything" };

        // Act
        object returned = IdMocker.AddId(record, Field.Of<Account>(x => x.Id));

        // Assert - AddId returns the same instance it was given
        Assert.Same(record, returned);
        Assert.NotNull(((Account)returned).Id);
    }

    [Fact]
    public void AddIds_PopulatesEveryRecordWithADistinctId()
    {
        // Arrange
        List<object> records = [new Contact { LastName = "A" }, new Contact { LastName = "B" }, new Contact { LastName = "C" }];

        // Act
        _ = IdMocker.AddIds(records);

        // Assert
        HashSet<string?> ids = [.. records.Cast<Contact>().Select(contact => contact.Id)];
        Assert.All(records.Cast<Contact>(), contact => Assert.NotNull(contact.Id));
        Assert.Equal(3, ids.Count);
    }
}
