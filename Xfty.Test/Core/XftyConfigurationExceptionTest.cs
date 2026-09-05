using Net.Nowhereatall.Xfty.Core;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves the framework's base exception type is a constructable Exception
/// subclass. It is thrown and caught across the suite; this pins the hierarchy.
/// </summary>
public class XftyConfigurationExceptionTest
{
    [Fact]
    public void Constructor_WhenGivenAMessage_ProducesACatchableExceptionSubclassCarryingIt()
    {
        // Arrange
        // nothing to arrange

        // Act
        static void Act() => throw new XftyConfigurationException("boom");
        XftyConfigurationException thrown = Assert.Throws<XftyConfigurationException>(Act);

        // Assert
        _ = Assert.IsType<XftyConfigurationException>(thrown);
        Assert.Equal("boom", thrown.Message);
    }
}
