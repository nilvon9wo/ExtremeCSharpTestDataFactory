using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves DeferredValueQueue - collecting one pending value per primary row
/// per deferred field. Pure in-memory, no database access. The strategy reference is
/// stored verbatim and never called here, so the tests pass null for it.
/// </summary>
public class DeferredValueQueueTest
{
    [Fact]
    public void Entries_WhenNothingWasAdded_IsEmpty()
    {
        // Arrange
        DeferredValueQueue queue = new();

        // Act
        List<BundleDeferredEntry> entries = queue.Entries();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void AddForEachRow_Always_QueuesOneEntryPerRowPerField()
    {
        // Arrange
        DeferredValueQueue queue = new();
        Dictionary<PropertyInfo, IDeferredExpression> byField = new()
        {
            [Field.Of<Account>(x => x.Name)] = null!,
            [Field.Of<Account>(x => x.Site)] = null!,
        };

        // Act
        queue.AddForEachRow(3, byField);

        // Assert - 3 rows x 2 fields
        Assert.Equal(6, queue.Entries().Count);
    }

    [Fact]
    public void AddForEachRow_WhenRowCountIsZero_QueuesNothing()
    {
        // Arrange
        DeferredValueQueue queue = new();
        Dictionary<PropertyInfo, IDeferredExpression> byField = new() { [Field.Of<Account>(x => x.Name)] = null! };

        // Act
        queue.AddForEachRow(0, byField);

        // Assert
        Assert.Empty(queue.Entries());
    }

    [Fact]
    public void AddForEachRow_Always_RecordsTheRowAndFieldOnEachEntry()
    {
        // Arrange
        DeferredValueQueue queue = new();
        Dictionary<PropertyInfo, IDeferredExpression> byField = new() { [Field.Of<Account>(x => x.Name)] = null! };

        // Act
        queue.AddForEachRow(2, byField);

        // Assert
        List<BundleDeferredEntry> entries = queue.Entries();
        Assert.Equal(0, entries[0].PrimaryRow);
        Assert.Equal(1, entries[1].PrimaryRow);
        Assert.Equal(Field.Of<Account>(x => x.Name), entries[1].Field);
    }
}
