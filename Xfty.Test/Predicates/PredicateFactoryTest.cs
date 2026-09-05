using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Test.Predicates;

/// <summary>
/// Proves the <see cref="PredicateFactory"/> combinator facade wires
/// AllOf/AnyOf/Negate to the right implementation.
///
/// The Apex original also proved a combinator tree driving
/// XFTY_FlavouredLookupKey (the "strategic" example from
/// docs/extend/provider-variants.md) here; that belongs once the lookup/
/// module is ported (see csharp-port-idea.md) and isn't repeated as a TODO
/// per class - tracked centrally there.
/// </summary>
public class PredicateFactoryTest
{
    [Fact]
    public void AllOf_WhenAMemberIsNotSatisfied_ReturnsFalse() =>
        AssertIsSatisfiedBy(
            PredicateFactory.AllOf(new List<IRecordPredicate>
            {
                FieldPredicateFactory.EqualTo(Field.Of<Account>(x => x.Industry), "Technology")
            }),
            new Account { Industry = "Retail" }, false);

    [Fact]
    public void AnyOf_WhenAMemberIsSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            PredicateFactory.AnyOf(new List<IRecordPredicate>
            {
                FieldPredicateFactory.EqualTo(Field.Of<Account>(x => x.Industry), "Technology")
            }),
            new Account { Industry = "Technology" }, true);

    [Fact]
    public void Negate_WhenTheInnerPredicateIsNotSatisfied_ReturnsTrue() =>
        AssertIsSatisfiedBy(
            PredicateFactory.Negate(FieldPredicateFactory.EqualTo(Field.Of<Account>(x => x.Type), "Prospect")),
            new Account { Type = "Customer" }, true);

    private static void AssertIsSatisfiedBy(IRecordPredicate predicate, Account? record, bool expectedResult)
    {
        // Arrange - the caller supplies the facade-built predicate and the record

        // Act
        bool actualResult = predicate.IsSatisfiedBy(record);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
