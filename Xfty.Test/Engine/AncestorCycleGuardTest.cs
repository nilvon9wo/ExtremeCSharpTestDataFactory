using Net.Nowhereatall.Xfty.Engine;

namespace Net.Nowhereatall.Xfty.Test.Engine;

/// <summary>Proves AncestorCycleGuard - the key-chain tracking that stops an infinite A -> A -> A ... ancestor cycle. Pure in-memory state, no DML/SOQL.</summary>
public class AncestorCycleGuardTest
{
    [Fact]
    public void WouldCycleOn_ForAnEmptyGuard_ReturnsFalse()
    {
        // Arrange
        AncestorCycleGuard guard = new(cyclesAllowed: false);

        // Act
        bool cycles = guard.WouldCycleOn("Account");

        // Assert
        Assert.False(cycles);
    }

    [Fact]
    public void WouldCycleOn_WhenTheKeyIsAlreadyInProgress_ReturnsTrue()
    {
        // Arrange
        AncestorCycleGuard guard = new AncestorCycleGuard(cyclesAllowed: false).DescendingInto("Account");

        // Act
        bool cycles = guard.WouldCycleOn("Account");

        // Assert - the same key one level up is a cycle
        Assert.True(cycles);
    }

    [Fact]
    public void WouldCycleOn_ForADifferentKeyThanThoseInProgress_ReturnsFalse()
    {
        // Arrange
        AncestorCycleGuard guard = new AncestorCycleGuard(cyclesAllowed: false).DescendingInto("Account");

        // Act
        bool cycles = guard.WouldCycleOn("Contact");

        // Assert
        Assert.False(cycles);
    }

    [Fact]
    public void DescendingInto_AccumulatesTheKeyChain()
    {
        // Arrange
        AncestorCycleGuard parent = new AncestorCycleGuard(cyclesAllowed: false).DescendingInto("Account");

        // Act
        AncestorCycleGuard child = parent.DescendingInto("Contact");

        // Assert
        Assert.True(child.WouldCycleOn("Account"));
        Assert.True(child.WouldCycleOn("Contact"));
    }

    [Fact]
    public void DescendingInto_DoesNotMutateTheParentGuard()
    {
        // Arrange
        AncestorCycleGuard parent = new AncestorCycleGuard(cyclesAllowed: false).DescendingInto("Account");

        // Act
        _ = parent.DescendingInto("Contact");

        // Assert - the parent guard is unchanged
        Assert.False(parent.WouldCycleOn("Contact"));
    }

    [Fact]
    public void WouldCycleOn_WhenCyclesAreAllowed_ReturnsFalseEvenForAKeyInProgress()
    {
        // Arrange
        AncestorCycleGuard guard = new AncestorCycleGuard(cyclesAllowed: true).DescendingInto("Account");

        // Act
        bool cycles = guard.WouldCycleOn("Account");

        // Assert - AllowAncestorCycles() lets the repeat through
        Assert.False(cycles);
    }
}
