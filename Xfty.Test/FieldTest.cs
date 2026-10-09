using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.Test;

/// <summary>
/// Proves <see cref="Field"/> - resolving a property by lambda (unwrapping the
/// boxing conversion a value-typed property needs) or by name, and rejecting
/// anything that is not a simple property access.
/// </summary>
public class FieldTest
{
    private const string NotASimplePropertyAccess = "not a simple property access";

    [Fact]
    public void Of_ByLambda_ForAReferenceTypedProperty_ReturnsIt()
    {
        // Arrange
        // nothing to arrange

        // Act
        PropertyInfo field = Field.Of<Account>(x => x.Name);

        // Assert
        Assert.Equal(typeof(Account).GetProperty(nameof(Account.Name)), field);
    }

    [Fact]
    public void Of_ByLambda_ForAValueTypedProperty_UnwrapsTheBoxingConversion()
    {
        // Arrange
        // nothing to arrange

        // Act
        PropertyInfo field = Field.Of<Account>(x => x.NumberOfEmployees);

        // Assert
        Assert.Equal(typeof(Account).GetProperty(nameof(Account.NumberOfEmployees)), field);
    }

    [Fact]
    public void Of_ByLambda_WhenTheSelectorCallsAMethod_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => Field.Of<Account>(x => x.ToString())
        );

        // Assert
        Assert.Contains(NotASimplePropertyAccess, thrown.Message);
    }

    [Fact]
    public void Of_ByLambda_WhenTheSelectorConvertsAComputedValue_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => Field.Of<Account>(x => x.NumberOfEmployees + 1)
        );

        // Assert
        Assert.Contains(NotASimplePropertyAccess, thrown.Message);
    }

    [Fact]
    public void Of_ByLambda_WhenTheSelectorReadsAField_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => Field.Of<(string Label, int Count)>(x => x.Label)
        );

        // Assert
        Assert.Contains(NotASimplePropertyAccess, thrown.Message);
    }

    [Fact]
    public void Of_ByName_ReturnsTheNamedProperty()
    {
        // Arrange
        // nothing to arrange

        // Act
        PropertyInfo field = Field.Of<Account>(nameof(Account.Industry));

        // Assert
        Assert.Equal(typeof(Account).GetProperty(nameof(Account.Industry)), field);
    }

    [Fact]
    public void Of_ByName_WhenNoSuchPropertyExists_Throws()
    {
        // Arrange
        // nothing to arrange

        // Act
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(
            () => Field.Of<Account>("NoSuchField")
        );

        // Assert
        Assert.Contains("has no field named 'NoSuchField'", thrown.Message);
    }
}