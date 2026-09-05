using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Persistence;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves Bundle, the container that preserves the shape of a generated object graph. Pure in-memory structure, no DML/SOQL.</summary>
public class BundleTest
{
    // Put / GetList / GetBundle -------------------------------------

    [Fact]
    public void GetList_AfterPut_ReturnsWhatWasPutAndPutIsChainable()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "One" }];
        Bundle bundle = new();

        // Act
        Bundle returned = bundle.Put<Account>(x => x.Id, accounts);

        // Assert - Put is chainable
        Assert.Same(bundle, returned);
        Assert.Equal(accounts, bundle.GetList<Account>(x => x.Id));
    }

    [Fact]
    public void GetBundle_AfterPut_ReturnsTheNestedBundle()
    {
        // Arrange
        Bundle child = new();
        Bundle parent = new();

        // Act
        _ = parent.Put<Contact>(x => x.AccountId, child);

        // Assert
        Assert.Same(child, parent.GetBundle<Contact>(x => x.AccountId));
    }

    [Fact]
    public void GetListAndGetBundle_ForTheSameField_AreStoredIndependently()
    {
        // Arrange
        List<object> accounts = [new Account { Name = "One" }];
        Bundle child = new();

        // Act
        Bundle bundle = new Bundle()
            .Put<Contact>(x => x.AccountId, accounts)
            .Put<Contact>(x => x.AccountId, child);

        // Assert
        Assert.Equal(accounts, bundle.GetList<Contact>(x => x.AccountId));
        Assert.Same(child, bundle.GetBundle<Contact>(x => x.AccountId));
    }

    [Fact]
    public void GetList_ForAMissingKey_ReturnsNull()
    {
        // Arrange
        Bundle bundle = new();

        // Act
        List<object>? missing = bundle.GetList<Account>(x => x.Id);

        // Assert
        Assert.Null(missing);
    }

    [Fact]
    public void GetBundle_ForAMissingKey_ReturnsNull()
    {
        // Arrange
        Bundle bundle = new();

        // Act
        Bundle? missing = bundle.GetBundle<Account>(x => x.Id);

        // Assert
        Assert.Null(missing);
    }

    // GetValue - delegates to AncestorPathWalker (walk behaviour proven in AncestorPathWalkerTest)

    [Fact]
    public void GetValue_WithARowIndex_WalksFromThisBundleAtThatRow()
    {
        // Arrange
        Bundle bundle = new();
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Name = "Row Zero" }, new Account { Name = "Row One" }]);

        // Act
        object? rowOneName = bundle.GetValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)], 1);

        // Assert
        Assert.Equal("Row One", rowOneName);
    }

    [Fact]
    public void GetValue_WithoutARowIndex_WalksFromRowZero()
    {
        // Arrange
        Bundle bundle = new();
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Name = "First" }]);

        // Act
        object? firstName = bundle.GetValue([Field.Of<Contact>(x => x.AccountId), Field.Of<Account>(x => x.Name)]);

        // Assert
        Assert.Equal("First", firstName);
    }

    // ChildRecordsOf - one primary's slice of the children -----------

    [Fact]
    public void ChildRecordsOf_ForOneParentRow_ReturnsOnlyThatRowsChildren()
    {
        // Arrange - 2 parents; row 0 owns children A0 and A1, row 1 owns A2
        Bundle childBundle = new();
        childBundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "A0" }, new Contact { LastName = "A1" }, new Contact { LastName = "A2" }]);
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account(), new Account()]);
        _ = bundle.PutChild(Field.Of<Contact>(x => x.AccountId), childBundle, [0, 0, 1]);

        // Act
        List<object> rowOneChildren = bundle.ChildRecordsOf(1, Field.Of<Contact>(x => x.AccountId));

        // Assert
        _ = Assert.Single(rowOneChildren);
        Assert.Equal("A2", ((Contact)rowOneChildren[0]).LastName);
    }

    [Fact]
    public void ChildRecordsOf_AcrossTwoConfigsOnTheSameField_MergesInDeclarationOrder()
    {
        // Arrange - two child configs on the same field, both with a child for parent row 0
        Bundle firstConfig = new();
        firstConfig.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "first" }]);
        Bundle secondConfig = new();
        secondConfig.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "second" }]);
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Account>(x => x.Id), [new Account()]);
        _ = bundle.PutChild(Field.Of<Contact>(x => x.AccountId), firstConfig, [0]);
        _ = bundle.PutChild(Field.Of<Contact>(x => x.AccountId), secondConfig, [0]);

        // Act
        List<object> rowZeroChildren = bundle.ChildRecordsOf(0, Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.Equal(2, rowZeroChildren.Count);
        Assert.Equal("first", ((Contact)rowZeroChildren[0]).LastName); // config declaration order
        Assert.Equal("second", ((Contact)rowZeroChildren[1]).LastName);
    }

    [Fact]
    public void ChildRecordsOf_WhenTheFieldCarriesNoChildren_ReturnsAnEmptyList()
    {
        // Arrange
        Bundle bundle = new();

        // Act
        List<object> none = bundle.ChildRecordsOf(0, Field.Of<Contact>(x => x.AccountId));

        // Assert - no children configured for that field
        Assert.Empty(none);
    }

    // PrimariesResolvingTo - the inverse of the 1:1 parent alignment -

    [Fact]
    public void PrimariesResolvingTo_WhenPrimariesShareAnAncestor_ReturnsEveryPrimaryOnThatAncestor()
    {
        // Arrange - contacts 0 and 2 resolve to account 0, contact 1 to account 1
        string accountZero = IdMocker.GenerateId();
        string accountOne = IdMocker.GenerateId();
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [
            new Contact { LastName = "C0", AccountId = accountZero },
            new Contact { LastName = "C1", AccountId = accountOne },
            new Contact { LastName = "C2", AccountId = accountZero },
        ]);
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account { Id = accountZero }, new Account { Id = accountOne }]);

        // Act
        List<object> onAccountZero = bundle.PrimariesResolvingTo(Field.Of<Contact>(x => x.AccountId), 0);

        // Assert
        Assert.Equal(2, onAccountZero.Count);
        Assert.Equal("C0", ((Contact)onAccountZero[0]).LastName);
        Assert.Equal("C2", ((Contact)onAccountZero[1]).LastName);
    }

    [Fact]
    public void PrimariesResolvingTo_WhenNoIdsExist_FallsBackToPositionalAlignment()
    {
        // Arrange - no Ids anywhere, so ancestor row N pairs with primary row N
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "C0" }, new Contact { LastName = "C1" }]);
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account(), new Account()]);

        // Act
        List<object> onRowOne = bundle.PrimariesResolvingTo(Field.Of<Contact>(x => x.AccountId), 1);

        // Assert
        _ = Assert.Single(onRowOne);
        Assert.Equal("C1", ((Contact)onRowOne[0]).LastName);
    }

    [Fact]
    public void PrimariesResolvingTo_WhenTheAncestorRowIsOutOfRange_ReturnsAnEmptyList()
    {
        // Arrange
        Bundle bundle = new();
        bundle.PutPrimaries(Field.Of<Contact>(x => x.Id), [new Contact { LastName = "C0" }]);
        _ = bundle.Put<Contact>(x => x.AccountId, [new Account()]);

        // Act
        List<object> none = bundle.PrimariesResolvingTo(Field.Of<Contact>(x => x.AccountId), 5);

        // Assert
        Assert.Empty(none);
    }
}
