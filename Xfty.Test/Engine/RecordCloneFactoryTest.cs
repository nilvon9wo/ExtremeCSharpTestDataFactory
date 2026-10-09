using Net.NowhereAtAll.Xfty.Engine;

namespace Net.NowhereAtAll.Xfty.Test.Engine;

/// <summary>
/// Proves <see cref="RecordCloneFactory"/> copies every writable property and
/// skips the ones it cannot write (a computed, get-only property).
/// </summary>
public class RecordCloneFactoryTest
{
    [Fact]
    public void DeepClone_WhenTheRecordHasAGetOnlyProperty_CopiesTheWritableOnesAndSkipsIt()
    {
        // Arrange
        RecordWithAComputedProperty original = new() { First = "Ada", Last = "Lovelace" };

        // Act
        RecordWithAComputedProperty clone = (RecordWithAComputedProperty)RecordCloneFactory.DeepClone(original);

        // Assert - the computed property follows from the copied ones; nothing tried to set it
        Assert.Equal("Ada Lovelace", clone.FullName);
    }
}

file sealed class RecordWithAComputedProperty
{
    public string? First { get; init; }

    public string? Last { get; init; }

    public string FullName => $"{this.First} {this.Last}";
}