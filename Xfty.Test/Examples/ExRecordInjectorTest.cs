using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Enrichment;

namespace Net.NowhereAtAll.Xfty.Test.Examples;

/// <summary>
/// Runs the exact code shown in docs/use/record-injector.md.
/// Checked by scripts/verify-doc-examples.py.
/// </summary>
public class ExRecordInjectorTest
{
    [Fact]
    public void TheShape()
    {
        // from docs/use/record-injector.md "The shape"
        List<object> contacts = [new Contact { LastName = "Smith" }];
        List<object> accounts = [new Account { Name = "Acme" }];

        List<object> enriched = RecordInjector.Inject(contacts)
            .Relationship(Field.Of<Contact>(x => x.Account), accounts)
            .Value(Field.Of<Contact>(x => x.Birthdate), new DateTime(2024, 1, 1))
            .Result();

        Assert.Equal("Acme", ((Contact)enriched[0]).Account!.Name);
        Assert.Equal(new DateTime(2024, 1, 1), ((Contact)enriched[0]).Birthdate);
    }

    [Fact]
    public void ParentRelationships()
    {
        // from docs/use/record-injector.md "Parent relationships"
        List<object> contacts = [new Contact { LastName = "Smith" }];
        List<object> accountsAligned1To1 = [new Account { Name = "Acme" }];

        List<object> enriched = RecordInjector.Inject(contacts)
            .Relationship(Field.Of<Contact>(x => x.Account), accountsAligned1To1)
            .Result();

        Assert.Equal("Acme", ((Contact)enriched[0]).Account!.Name);
    }

    [Fact]
    public void ChildCollections()
    {
        // from docs/use/record-injector.md "Child collections"
        List<object> accounts = [new Account { Name = "A" }, new Account { Name = "B" }];
        List<List<object>> contactsPerAccount =
        [
            [new Contact { LastName = "A" }, new Contact { LastName = "B" }],  // account 0
            [new Contact { LastName = "C" }],                                 // account 1
        ];

        List<object> enriched = RecordInjector.Inject(accounts)
            .ChildRelationship(Field.Of<Account>(x => x.Contacts), contactsPerAccount)
            .Result();

        Assert.Equal(2, ((Account)enriched[0]).Contacts!.Count);
        Assert.Equal("C", ((Account)enriched[1]).Contacts![0].LastName);
    }

    [Fact]
    public void NestedCollections()
    {
        // from docs/use/record-injector.md "Nested collections"
        List<object> contactsWithCases = RecordInjector.Inject([new Contact { LastName = "Owner" }])
            .ChildRelationship(Field.Of<Contact>(x => x.Cases), [[new Case { Subject = "x" }]])
            .Result();

        List<object> accounts = [new Account { Name = "Acme" }];
        List<object> enriched = RecordInjector.Inject(accounts)
            .ChildRelationship(Field.Of<Account>(x => x.Contacts), [contactsWithCases])
            .Result();

        Assert.Equal("x", ((Account)enriched[0]).Contacts![0].Cases![0].Subject);
    }

    [Fact]
    public void ForcedValues()
    {
        // from docs/use/record-injector.md "Forced values"
        List<object> accounts = [new Account { Name = "A" }, new Account { Name = "B" }];

        List<object> enriched = RecordInjector.Inject(accounts)
            .Value(Field.Of<Account>(x => x.Industry), "Aerospace")            // same on every row
            .ValuePerRow(Field.Of<Account>(x => x.AnnualRevenue), [100m, 250m])
            .Result();

        Assert.All(enriched.Cast<Account>(), account => Assert.Equal("Aerospace", account.Industry));
        Assert.Equal([100m, 250m], enriched.Cast<Account>().Select(a => a.AnnualRevenue));
    }
}
