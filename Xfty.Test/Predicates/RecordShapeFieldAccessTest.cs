using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves reflection-based field access (<see cref="Field"/>, the
/// predicates) is genuinely indifferent to how a record type declares its
/// properties: <see cref="Account"/>'s plain <c>init</c>-only class
/// properties and <see cref="Contact"/>'s compiler-generated <c>record
/// class</c> properties both work the same way, with no special-casing.
/// </summary>
public class RecordShapeFieldAccessTest
{
    [Fact]
    public void IsSatisfiedBy_AgainstAnInitOnlyClassProperty_ReadsItCorrectly()
    {
        // Arrange
        Account account = new() { Industry = "Technology" };
        IRecordPredicate predicate = FieldPredicateFactory.EqualTo<Account>(x => x.Industry, "Technology");

        // Act
        bool actualResult = predicate.IsSatisfiedBy(account);

        // Assert
        Assert.True(actualResult);
    }

    [Fact]
    public void IsSatisfiedBy_AgainstARecordClassProperty_ReadsItCorrectly()
    {
        // Arrange
        Contact contact = new() { FirstName = "Ada", LastName = "Lovelace" };
        IRecordPredicate predicate = FieldPredicateFactory.EqualTo<Contact>(x => x.LastName, "Lovelace");

        // Act
        bool actualResult = predicate.IsSatisfiedBy(contact);

        // Assert
        Assert.True(actualResult);
    }
}
