using FluentAssertions;
using NSubstitute;

namespace Net.Nowhereatall.Xfty.Core.Test;

/// <summary>
/// Verifies the project scaffold (xunit + FluentAssertions + NSubstitute + the
/// Xfty.Core project reference) is wired correctly. Delete once real specs land.
/// </summary>
public class ScaffoldSmokeTest
{
    [Fact]
    public void FluentAssertionsIsWired()
    {
        var greeting = "Xfty";
        greeting.Should().Be("Xfty");
    }

    [Fact]
    public void NSubstituteIsWired()
    {
        var substituteGreeter = Substitute.For<IScaffoldGreeter>();
        substituteGreeter.Greet().Returns("Xfty");
        substituteGreeter.Greet().Should().Be("Xfty");
    }
}

public interface IScaffoldGreeter
{
    string Greet();
}
