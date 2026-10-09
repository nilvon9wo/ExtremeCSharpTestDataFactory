using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.Children;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves <see cref="ChildProvider{TChild}"/> - the typed wrapper - mirrors <see cref="RecordProvider{TRecord}"/>'s own
/// pattern.
/// </summary>
public class ChildProviderOfTTest
{
    private static IProviderLocating Lookup() =>
        ProviderLookups.Of(new Dictionary<IRecordIdentifying, IRecordProviding>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new ContactDataProvider(),
        });

    [Fact]
    [SuppressMessage("Performance", "HLQ005:Avoid Single() and SingleOrDefault()", Justification = "<Pending>")]
    public async Task ObjectInitializer_RoutesEachValueByRuntimeType()
    {
        // Arrange - mirrors RecordProvider<TRecord>'s own indexer syntax
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider<Contact>(x => x.AccountId)
            {
                [x => x.FirstName] = "Escalated",
            });

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

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
    public async Task With_AcceptsATypedChildProviderThroughTheImplicitConversion()
    {
        // Arrange - RecordProvider.With(ChildProvider) should accept ChildProvider<TChild> directly
        RecordProvider<Account> provider = new RecordProvider<Account>(Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider<Contact>(x => x.AccountId).SetQuantity(2));

        // Act
        Account result = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result.Id);
    }

    [Fact]
    public async Task Constructor_WithATemplate_AppliesItToEveryChild()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .With(new ChildProvider<Contact>(x => x.AccountId, new Contact { Department = "Templated" }));

        // Act
        Bundle bundle = await provider.SupplyBundle().ConfigureAwait(true);

        // Assert
        Contact child = (Contact)bundle.GetChildList<Contact>(x => x.AccountId)[0];
        Assert.Equal("Templated", child.Department);
    }

    [Fact]
    public void RelationshipField_IsTheFieldNamedByTheLambda()
    {
        // Arrange
        ChildProvider<Contact> typed = new(x => x.AccountId);

        // Act
        PropertyInfo relationshipField = typed.RelationshipField;

        // Assert
        Assert.Equal(Field.Of<Contact>(x => x.AccountId), relationshipField);
    }

    [Fact]
    public void ChildType_IsTheTypeArgument()
    {
        // Arrange
        ChildProvider<Contact> typed = new(x => x.AccountId);

        // Act
        Type childType = typed.ChildType;

        // Assert
        Assert.Equal(typeof(Contact), childType);
    }

    [Fact]
    public void SetInsertMode_ForwardsToTheInnerChildProvider()
    {
        // Arrange
        ChildProvider<Contact> typed = new ChildProvider<Contact>(x => x.AccountId).SetInsertMode(InsertMode.Never);
        ChildProvider plain = typed;

        // Act
        InsertMode effective = plain.EffectiveInsertMode(InsertMode.Deferred);

        // Assert
        Assert.Equal(InsertMode.Never, effective);
    }
}