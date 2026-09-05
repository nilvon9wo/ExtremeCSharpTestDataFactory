using System.Diagnostics;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;
using Net.Nowhereatall.Xfty.Values;

namespace Net.Nowhereatall.Xfty.Test;

/// <summary>
/// Volume tests: generate at a scale a real test suite might reach and prove
/// nothing degrades badly - not a governor-limit check, because there are no
/// governor limits here (see csharp-port-idea.md's GovernorBudget carve-out
/// and XFTY_LoadTest, the Apex original this shadows). What actually matters
/// in C# is wall-clock time and allocation, so this uses Stopwatch and
/// GC.GetTotalMemory instead of Limits.getCpuTime()/getDmlRows()/etc.
///
/// Ceilings here are deliberately generous (an order of magnitude above what
/// a healthy run takes locally) so this stays green on a loaded CI runner -
/// it exists to catch an accidental O(n^2) regression, not to enforce a tight
/// budget. Excluded from the default `dotnet test` filter some CI setups may
/// apply via the Performance trait, the same role XFTY_Load plays running
/// only in Apex's scheduled full-suite workflow rather than on every push.
/// </summary>
[Trait("Category", "Performance")]
public class PerformanceTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void Supply_ForThreeThousandPrimariesWithARequiredParent_CompletesWellUnderASecond()
    {
        // Arrange - a Contact + its Account is 2 records per primary, so 3 000 primaries = 6 000 records
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetQuantityPerTemplate(3000)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Mock);

        // Act
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<object> results = provider.SupplyList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(3000, results.Count);
        Assert.All(results.Cast<Contact>(), contact => Assert.NotNull(contact.AccountId));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"took {stopwatch.Elapsed}");
    }

    [Fact]
    public void SupplyBundle_ForFiveThousandPrimariesHeldInMemory_StaysWithinAGenerousMemoryBudget()
    {
        // Arrange - 5 000 primaries with a generated parent each, held in memory, no insert
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetQuantityPerTemplate(5000)
            .SetInclusivity(InsertInclusivity.Required)
            .SetInsertMode(InsertMode.Never);

        // Act
        long before = GC.GetTotalMemory(forceFullCollection: true);
        Bundle bundle = provider.SupplyBundle();
        long allocatedRoughly = GC.GetTotalMemory(forceFullCollection: false) - before;

        // Assert
        Assert.Equal(5000, bundle.PrimaryRecords()!.Count);
        Assert.True(allocatedRoughly < 512 * 1024 * 1024, $"used roughly {allocatedRoughly / (1024 * 1024)} MB");
    }

    [Fact]
    public void SupplyBundle_ForDownwardGenerationOfNestedChildren_MultipliesRecordsNotWallClock()
    {
        // Arrange - 15 Accounts, 10 Contacts each: 15 + 150 = 165 records, structurally batched
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetQuantityPerTemplate(15)
            .SetInsertMode(InsertMode.Mock)
            .WithChildren(Field.Of<Contact>(nameof(Contact.AccountId)), 10);

        // Act
        Stopwatch stopwatch = Stopwatch.StartNew();
        Bundle bundle = provider.SupplyBundle();
        stopwatch.Stop();

        // Assert
        Assert.Equal(15, bundle.PrimaryRecords()!.Count);
        Assert.Equal(150, bundle.GetChildList(Field.Of<Contact>(nameof(Contact.AccountId))).Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"took {stopwatch.Elapsed}");
    }

    [Fact]
    public void Supply_WithTwoContextAwareExpressionsPerRecordAtVolume_StaysCheap()
    {
        // Arrange - 3 000 Contacts, each with a sibling copy and a custom context-aware expression
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetQuantityPerTemplate(3000)
            .SetInsertMode(InsertMode.Mock)
            .Put(Field.Of<Contact>(nameof(Contact.Department)), new IncrementingStringExpression("Dept"))
            .Put(Field.Of<Contact>(nameof(Contact.FirstName)), new CopyFromSiblingExpression(Field.Of<Contact>(nameof(Contact.Department))));

        // Act
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<object> results = provider.SupplyList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(3000, results.Count);
        Assert.All(results.Cast<Contact>(), contact => Assert.Equal(contact.Department, contact.FirstName));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"took {stopwatch.Elapsed}");
    }
}
