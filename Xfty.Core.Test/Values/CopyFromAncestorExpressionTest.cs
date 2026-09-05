using Net.Nowhereatall.Xfty.Core.Demo;
using Net.Nowhereatall.Xfty.Core.Lookup;
using Net.Nowhereatall.Xfty.Core.Values;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Core.Test.Values;

/// <summary>
/// Proves <see cref="CopyFromAncestorExpression"/> by building the
/// <see cref="GenerationContext"/>/<see cref="Bundle"/> directly - the Apex
/// original also proved this driven through a Provider; that needs the
/// child-collection/ancestor-generation engine already exercised end-to-end
/// in RecordProviderIntegrationTest, so isn't repeated here.
/// </summary>
public class CopyFromAncestorExpressionTest
{
    private static readonly IProviderLookup Lookup = Substitute.For<IProviderLookup>();

    [Fact]
    public void Get_WhenHandedABaseContextWithNoAncestors_IsNull()
    {
        // Arrange
        GenerationContext baseContext = new(Lookup, InsertMode.Mock, InsertInclusivity.None);
        CopyFromAncestorExpression expression = new(
            Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.Name)));

        // Act
        object? value = expression.Get(baseContext);

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void Get_TakesAFieldFromTheGeneratedParent()
    {
        // Arrange - a Contact row whose AccountId sub-bundle holds one generated Account
        Bundle accountBundle = new();
        accountBundle.PutPrimaries(Field.Of<Account>(nameof(Account.Id)), [new Account { Name = "Wired Parent" }]);
        Bundle contactBundle = new();
        _ = contactBundle.Put(Field.Of<Contact>(nameof(Contact.AccountId)), accountBundle);
        _ = contactBundle.Put(Field.Of<Contact>(nameof(Contact.AccountId)), accountBundle.PrimaryRecords()!);
        GenerationContext context = new GenerationContext(Lookup, InsertMode.Mock, InsertInclusivity.Required)
            .ForRecord(new Contact(), contactBundle, 0);
        CopyFromAncestorExpression expression = new(
            Field.Of<Contact>(nameof(Contact.AccountId)), Field.Of<Account>(nameof(Account.Name)));

        // Act
        object? value = expression.Get(context);

        // Assert
        Assert.Equal("Wired Parent", value);
    }

    [Fact]
    public void Constructor_WhenAPathStepIsNull_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => new CopyFromAncestorExpression(null!, Field.Of<Account>(nameof(Account.Name))));

        // Assert
        Assert.Contains("cannot be null", thrown.Message);
    }

    [Fact]
    public void Constructor_WhenThePathIsTooShort_Throws()
    {
        // Arrange - nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => new CopyFromAncestorExpression([Field.Of<Account>(nameof(Account.Name))]));

        // Assert
        Assert.Contains("at least one relationship field", thrown.Message);
    }
}
