using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves MasterTemplate - the declarative description of how one record type is generated. Pure in-memory map manipulation, no DML/SOQL.</summary>
public class MasterTemplateTest
{
    // Constructor ---------------------------------------------------

    [Fact]
    public void Constructor_SeedsEmptyMapsAndKeepsThePrimaryTargetField()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new(Field.Of<Account>(nameof(Account.Id)));

        // Assert
        Assert.Equal(Field.Of<Account>(nameof(Account.Id)), template.PrimaryTargetField);
        Assert.Empty(template.DefaultByField);
        Assert.Empty(template.RequiredRelationshipByField);
        Assert.Empty(template.OptionalRelationshipByField);
    }

    // Put(field, valueExpression) --------------------------------

    [Fact]
    public void Put_ForAPlainValueExpression_RoutesItToTheDefaultMap()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
            .Put(Field.Of<Contact>(nameof(Contact.LastName)), new LiteralExpression("Doe"));

        // Assert
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Contact>(nameof(Contact.LastName))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Put_ForAContextAwareExpression_RoutesItToTheContextAwareMap(bool passAsObject)
    {
        // Arrange
        IContextAwareExpression contextAware = new CopyFromSiblingExpression(Field.Of<Contact>(nameof(Contact.FirstName)));
        MasterTemplate template = new(Field.Of<Contact>(nameof(Contact.Id)));

        // Act
        _ = passAsObject
            ? template.Put(Field.Of<Contact>(nameof(Contact.LastName)), (object)contextAware)
            : template.Put(Field.Of<Contact>(nameof(Contact.LastName)), contextAware);

        // Assert
        Assert.Equal(contextAware, template.ContextAwareByField[Field.Of<Contact>(nameof(Contact.LastName))]);
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(nameof(Contact.LastName)))); // not in the plain-value map
        Assert.Contains(Field.Of<Contact>(nameof(Contact.LastName)), template.OrderedValueFields()); // still in the ordered value fields
    }

    [Fact]
    public void Put_WhenReplacingAPlainFieldWithAContextAwareExpression_MovesItBetweenMaps()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("plain"))
            .Put(Field.Of<Account>(nameof(Account.Name)), new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.AccountNumber))));

        // Assert
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Name)))); // left the plain-value map
        Assert.True(template.ContextAwareByField.ContainsKey(Field.Of<Account>(nameof(Account.Name)))); // landed in the context-aware map
    }

    [Fact]
    public void Put_ForABareLiteral_WrapsItAsAnExactValue()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Type)), "Customer");

        // Assert
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Type))));
        Assert.Equal("Customer", template.DefaultByField[Field.Of<Account>(nameof(Account.Type))].Get());
    }

    [Fact]
    public void Put_ForAnExistingExpressionPassedAsObject_DoesNotDoubleWrapIt()
    {
        // Arrange
        IValueExpression expression = new IncrementingStringExpression("Acct");

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), (object)expression);

        // Assert
        Assert.Same(expression, template.DefaultByField[Field.Of<Account>(nameof(Account.Name))]);
    }

    [Fact]
    public void Put_WhenGivenARelationshipAsObject_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() =>
            new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
                .Put(Field.Of<Contact>(nameof(Contact.AccountId)), (object)new DefaultRelationship(new Account())));

        // Assert
        Assert.Contains("PutRequired", thrown.Message);
    }

    // PutRequired / PutOptional --------------------------------

    [Fact]
    public void PutRequired_RoutesTheRelationshipToTheRequiredMap()
    {
        // Arrange
        DefaultRelationship required = new(new Account());

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), required);

        // Assert
        Assert.Same(required, template.RequiredRelationshipByField[Field.Of<Contact>(nameof(Contact.AccountId))]);
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(nameof(Contact.AccountId))));
    }

    [Fact]
    public void PutOptional_RoutesTheRelationshipToTheOptionalMap()
    {
        // Arrange
        DefaultRelationship optional = new(new Contact());

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
            .PutOptional(Field.Of<Contact>(nameof(Contact.ReportsToId)), optional);

        // Assert
        Assert.Same(optional, template.OptionalRelationshipByField[Field.Of<Contact>(nameof(Contact.ReportsToId))]);
    }

    // Remove(field) -------------------------------------------

    [Fact]
    public void Remove_ClearsTheFieldFromEveryMapAndFromTheOrderedFields()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(nameof(Contact.Id)))
            .Put(Field.Of<Contact>(nameof(Contact.LastName)), new LiteralExpression("Doe"))
            .PutRequired(Field.Of<Contact>(nameof(Contact.AccountId)), new DefaultRelationship(new Account()));

        // Act
        _ = template.Remove(Field.Of<Contact>(nameof(Contact.LastName)));
        _ = template.Remove(Field.Of<Contact>(nameof(Contact.AccountId)));

        // Assert
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(nameof(Contact.LastName))));
        Assert.False(template.RequiredRelationshipByField.ContainsKey(Field.Of<Contact>(nameof(Contact.AccountId))));
        Assert.DoesNotContain(Field.Of<Contact>(nameof(Contact.LastName)), template.OrderedValueFields());
    }

    // OrderedValueFields() -----------------------------------

    [Fact]
    public void OrderedValueFields_FollowsPutOrder()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("n"))
            .Put(Field.Of<Account>(nameof(Account.Industry)), new LiteralExpression("i"))
            .Put(Field.Of<Account>(nameof(Account.Type)), new LiteralExpression("t"));

        // Act
        List<System.Reflection.PropertyInfo> ordered = template.OrderedValueFields();

        // Assert
        Assert.Equal(
            [Field.Of<Account>(nameof(Account.Name)), Field.Of<Account>(nameof(Account.Industry)), Field.Of<Account>(nameof(Account.Type))],
            ordered);
    }

    [Fact]
    public void OrderedValueFields_AfterRemoveThenRePut_KeepsTheFieldInItsOriginalPlace()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("n"))
            .Put(Field.Of<Account>(nameof(Account.Industry)), new LiteralExpression("i"))
            .Put(Field.Of<Account>(nameof(Account.Type)), new LiteralExpression("t"));
        _ = template.Remove(Field.Of<Account>(nameof(Account.Industry)));

        // Act
        _ = template.Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("n2"));

        // Assert
        Assert.Equal([Field.Of<Account>(nameof(Account.Name)), Field.Of<Account>(nameof(Account.Type))], template.OrderedValueFields());
    }

    // Copy() -------------------------------------------------

    [Fact]
    public void Copy_ReflectsItsOwnEdits()
    {
        // Arrange
        MasterTemplate original = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("Original"));

        // Act
        MasterTemplate copy = original.Copy();
        _ = copy.Put(Field.Of<Account>(nameof(Account.Industry)), new LiteralExpression("Tech"));
        _ = copy.Remove(Field.Of<Account>(nameof(Account.Name)));

        // Assert
        Assert.True(copy.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Industry))));
        Assert.False(copy.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Name))));
        Assert.Equal(Field.Of<Account>(nameof(Account.Id)), copy.PrimaryTargetField);
    }

    [Fact]
    public void Copy_LeavesTheOriginalUntouchedWhenTheCopyIsEdited()
    {
        // Arrange
        MasterTemplate original = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new LiteralExpression("Original"));

        // Act
        MasterTemplate copy = original.Copy();
        _ = copy.Put(Field.Of<Account>(nameof(Account.Industry)), new LiteralExpression("Tech"));
        _ = copy.Remove(Field.Of<Account>(nameof(Account.Name)));

        // Assert
        Assert.False(original.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Industry))));
        Assert.True(original.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Name))));
    }

    [Fact]
    public void Copy_CarriesAContextAwareEntry()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(nameof(Account.Id)))
            .Put(Field.Of<Account>(nameof(Account.Name)), new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.AccountNumber))));

        // Act
        MasterTemplate copy = template.Copy();

        // Assert
        Assert.True(copy.ContextAwareByField.ContainsKey(Field.Of<Account>(nameof(Account.Name))));
    }
}
