using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Relationships;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>Proves PathTargetValue - the value half of a PathValue, one of five kinds. ApplyTo lands on a master template (in-memory); no DML/SOQL.</summary>
public class PathTargetValueTest
{
    [Fact]
    public void OfLiteral_IsNotARelationshipAndAppliesTheLiteral()
    {
        // Arrange
        PathTargetValue value = PathTargetValue.OfLiteral("Acme");
        MasterTemplate template = new(Field.Of<Account>(nameof(Account.Id)));

        // Act
        value.ApplyTo(template, Field.Of<Account>(nameof(Account.Name)));

        // Assert
        Assert.False(value.IsRelationship);
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Name))));
    }

    [Fact]
    public void OfExpression_AppliesTheExpression()
    {
        // Arrange
        PathTargetValue value = PathTargetValue.OfExpression(new LiteralExpression("X"));
        MasterTemplate template = new(Field.Of<Account>(nameof(Account.Id)));

        // Act
        value.ApplyTo(template, Field.Of<Account>(nameof(Account.Name)));

        // Assert
        Assert.True(template.DefaultByField.ContainsKey(Field.Of<Account>(nameof(Account.Name))));
    }

    [Fact]
    public void OfRequiredRelationship_IsARelationship()
    {
        // Arrange
        PathTargetValue value = PathTargetValue.OfRequiredRelationship(new DefaultRelationship(new Account()));

        // Act
        bool isRelationship = value.IsRelationship;

        // Assert
        Assert.True(isRelationship);
        Assert.False(value.IsSharedRelationship);
    }

    [Fact]
    public void OfOptionalRelationship_AppliesAsAnOptionalRelationship()
    {
        // Arrange
        PathTargetValue value = PathTargetValue.OfOptionalRelationship(new DefaultRelationship(new Account()));
        MasterTemplate template = new(Field.Of<Contact>(nameof(Contact.Id)));

        // Act
        value.ApplyTo(template, Field.Of<Contact>(nameof(Contact.AccountId)));

        // Assert
        Assert.True(template.OptionalRelationshipByField.ContainsKey(Field.Of<Contact>(nameof(Contact.AccountId))));
    }

    [Fact]
    public void OfContextAware_AppliesAsAContextAwareExpression()
    {
        // Arrange
        PathTargetValue value = PathTargetValue.OfContextAware(new CopyFromSiblingExpression(Field.Of<Account>(nameof(Account.Name))));
        MasterTemplate template = new(Field.Of<Account>(nameof(Account.Id)));

        // Act
        value.ApplyTo(template, Field.Of<Account>(nameof(Account.Site)));

        // Assert
        Assert.True(template.ContextAwareByField.ContainsKey(Field.Of<Account>(nameof(Account.Site))));
    }
}
