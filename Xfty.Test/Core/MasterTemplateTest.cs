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
        MasterTemplate template = new(Field.Of<Account>(x => x.Id));

        // Assert
        Assert.Equal(Field.Of<Account>(x => x.Id), template.PrimaryTargetField);
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
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new LiteralExpression("Doe"));

        // Assert
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Contact>(x => x.LastName)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Put_ForAContextAwareExpression_RoutesItToTheContextAwareMap(bool passAsObject)
    {
        // Arrange
        IContextAwareExpression contextAware = CopyFromSiblingExpression.From<Contact>(x => x.FirstName);
        MasterTemplate template = new(Field.Of<Contact>(x => x.Id));

        // Act
        _ = passAsObject
            ? template.Put<Contact>(x => x.LastName, (object)contextAware)
            : template.Put<Contact>(x => x.LastName, contextAware);

        // Assert
        Assert.Equal(contextAware, template.ContextAwareByField[Field.Of<Contact>(x => x.LastName)]);
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(x => x.LastName))); // not in the plain-value map
        Assert.Contains(Field.Of<Contact>(x => x.LastName), template.OrderedValueFields()); // still in the ordered value fields
    }

    [Fact]
    public void Put_WhenReplacingAPlainFieldWithAContextAwareExpression_MovesItBetweenMaps()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new LiteralExpression("plain"))
            .Put<Account>(x => x.Name, CopyFromSiblingExpression.From<Account>(x => x.AccountNumber));

        // Assert
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Name))); // left the plain-value map
        Assert.True(template.ContextAwareByField.ContainsKey(Field.Of<Account>(x => x.Name))); // landed in the context-aware map
    }

    [Fact]
    public void Put_ForABareLiteral_WrapsItAsAnExactValue()
    {
        // Arrange
        // nothing to arrange

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Type, "Customer");

        // Assert
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Type)));
        Assert.Equal("Customer", template.DefaultByField[Field.Of<Account>(x => x.Type)].Get());
    }

    [Fact]
    public void Put_ForAnExistingExpressionPassedAsObject_DoesNotDoubleWrapIt()
    {
        // Arrange
        IValueExpression expression = new IncrementingStringExpression("Acct");

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, (object)expression);

        // Assert
        Assert.Same(expression, template.DefaultByField[Field.Of<Account>(x => x.Name)]);
    }

    [Fact]
    public void Put_WhenGivenARelationshipAsObject_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(() =>
            new MasterTemplate(Field.Of<Contact>(x => x.Id))
                .Put<Contact>(x => x.AccountId, (object)new DefaultRelationship(new Account())));

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
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .PutRequired<Contact>(x => x.AccountId, required);

        // Assert
        Assert.Same(required, template.RequiredRelationshipByField[Field.Of<Contact>(x => x.AccountId)]);
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(x => x.AccountId)));
    }

    [Fact]
    public void PutOptional_RoutesTheRelationshipToTheOptionalMap()
    {
        // Arrange
        DefaultRelationship optional = new(new Contact());

        // Act
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .PutOptional<Contact>(x => x.ReportsToId, optional);

        // Assert
        Assert.Same(optional, template.OptionalRelationshipByField[Field.Of<Contact>(x => x.ReportsToId)]);
    }

    // Remove(field) -------------------------------------------

    [Fact]
    public void Remove_ClearsTheFieldFromEveryMapAndFromTheOrderedFields()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Contact>(x => x.Id))
            .Put<Contact>(x => x.LastName, new LiteralExpression("Doe"))
            .PutRequired<Contact>(x => x.AccountId, new DefaultRelationship(new Account()));

        // Act
        _ = template.Remove(Field.Of<Contact>(x => x.LastName));
        _ = template.Remove(Field.Of<Contact>(x => x.AccountId));

        // Assert
        Assert.False(template.DefaultByField.ContainsKey(Field.Of<Contact>(x => x.LastName)));
        Assert.False(template.RequiredRelationshipByField.ContainsKey(Field.Of<Contact>(x => x.AccountId)));
        Assert.DoesNotContain(Field.Of<Contact>(x => x.LastName), template.OrderedValueFields());
    }

    // OrderedValueFields() -----------------------------------

    [Fact]
    public void OrderedValueFields_FollowsPutOrder()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new LiteralExpression("n"))
            .Put<Account>(x => x.Industry, new LiteralExpression("i"))
            .Put<Account>(x => x.Type, new LiteralExpression("t"));

        // Act
        List<System.Reflection.PropertyInfo> ordered = template.OrderedValueFields();

        // Assert
        Assert.Equal(
            [Field.Of<Account>(x => x.Name), Field.Of<Account>(x => x.Industry), Field.Of<Account>(x => x.Type)],
            ordered);
    }

    [Fact]
    public void OrderedValueFields_AfterRemoveThenRePut_KeepsTheFieldInItsOriginalPlace()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new LiteralExpression("n"))
            .Put<Account>(x => x.Industry, new LiteralExpression("i"))
            .Put<Account>(x => x.Type, new LiteralExpression("t"));
        _ = template.Remove(Field.Of<Account>(x => x.Industry));

        // Act
        _ = template.Put<Account>(x => x.Name, new LiteralExpression("n2"));

        // Assert
        Assert.Equal([Field.Of<Account>(x => x.Name), Field.Of<Account>(x => x.Type)], template.OrderedValueFields());
    }

    // Copy() -------------------------------------------------

    [Fact]
    public void Copy_ReflectsItsOwnEdits()
    {
        // Arrange
        MasterTemplate original = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new LiteralExpression("Original"));

        // Act
        MasterTemplate copy = original.Copy();
        _ = copy.Put<Account>(x => x.Industry, new LiteralExpression("Tech"));
        _ = copy.Remove(Field.Of<Account>(x => x.Name));

        // Assert
        Assert.True(copy.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Industry)));
        Assert.False(copy.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Name)));
        Assert.Equal(Field.Of<Account>(x => x.Id), copy.PrimaryTargetField);
    }

    [Fact]
    public void Copy_LeavesTheOriginalUntouchedWhenTheCopyIsEdited()
    {
        // Arrange
        MasterTemplate original = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, new LiteralExpression("Original"));

        // Act
        MasterTemplate copy = original.Copy();
        _ = copy.Put<Account>(x => x.Industry, new LiteralExpression("Tech"));
        _ = copy.Remove(Field.Of<Account>(x => x.Name));

        // Assert
        Assert.False(original.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Industry)));
        Assert.True(original.DefaultByField.ContainsKey(Field.Of<Account>(x => x.Name)));
    }

    [Fact]
    public void Copy_CarriesAContextAwareEntry()
    {
        // Arrange
        MasterTemplate template = new MasterTemplate(Field.Of<Account>(x => x.Id))
            .Put<Account>(x => x.Name, CopyFromSiblingExpression.From<Account>(x => x.AccountNumber));

        // Act
        MasterTemplate copy = template.Copy();

        // Assert
        Assert.True(copy.ContextAwareByField.ContainsKey(Field.Of<Account>(x => x.Name)));
    }
}
