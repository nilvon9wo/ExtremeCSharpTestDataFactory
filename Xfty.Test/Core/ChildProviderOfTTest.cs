using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves <see cref="ChildProvider{TChild}"/> - the typed wrapper - mirrors <see cref="RecordProvider{TRecord}"/>'s own pattern.</summary>
public class ChildProviderOfTTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void ObjectInitializer_RoutesEachValueByRuntimeType()
    {
        // Arrange - mirrors RecordProvider<TRecord>'s own indexer syntax
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider<Contact>(x => x.AccountId)
            {
                [x => x.FirstName] = "Escalated",
            });

        // Act
        Bundle bundle = provider.SupplyBundle();

        // Assert
        Contact child = Assert.IsType<Contact>(Assert.Single(bundle.GetChildList<Contact>(x => x.AccountId)));
        Assert.Equal("Escalated", child.FirstName);
    }

    [Fact]
    public void ConvertsImplicitlyToThePlainChildProvider()
    {
        // Arrange
        ChildProvider<Contact> typed = new(x => x.AccountId);

        // Act
        ChildProvider plain = typed;

        // Assert
        Assert.NotNull(plain);
        Assert.Equal(typeof(Contact), plain.ChildType);
    }

    [Fact]
    public void With_AcceptsATypedChildProviderThroughTheImplicitConversion()
    {
        // Arrange - RecordProvider.With(ChildProvider) should accept ChildProvider<TChild> directly
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider<Contact>(x => x.AccountId).SetQuantity(2));

        // Act
        Account result = provider.Supply();

        // Assert
        Assert.NotNull(result.Id);
    }
}
