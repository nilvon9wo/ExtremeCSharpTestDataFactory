using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Demo;
using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Test.Core;

/// <summary>
/// Proves the IUnsetFieldFiller hook RecordProvider/RecordFactory expose:
/// which fields count as "unset" (see also MasterTemplateTest.IsConfigured),
/// when the hook fires, and that it reaches generated ancestors too. The
/// bundled AutoFixture-backed implementation is proven separately, in
/// Xfty.AutoFixture.Test - this file uses a recording test double instead,
/// to keep the "what does core actually guarantee" contract independent of
/// any one filler implementation.
/// </summary>
public class UnsetFieldFillerTest
{
    private static IProviderLookup Lookup() =>
        ProviderLookups.Of(new Dictionary<ILookupKey, IRecordProvider>
        {
            [LookupKey.Get(typeof(Account))] = new AccountDataProvider(),
            [LookupKey.Get(typeof(Contact))] = new ContactDataProvider(),
        });

    [Fact]
    public void Supply_WithNoFillerConfigured_LeavesUnconfiguredFieldsAtTheirDefault()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock);

        // Act
        Account result = (Account)provider.Supply();

        // Assert - AccountDataProvider's Master Template never puts NumberOfEmployees
        Assert.Null(result.NumberOfEmployees);
    }

    [Fact]
    public void Supply_WithAFillerConfigured_FillsOnlyFieldsTheMasterTemplateNeverConfigured()
    {
        // Arrange
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = provider.Supply();

        // Assert
        List<string> fieldNames = [.. filler.FieldNamesSeen];
        Assert.Contains(nameof(Account.NumberOfEmployees), fieldNames); // never Put(...)
        Assert.Contains(nameof(Account.AnnualRevenue), fieldNames); // never Put(...)
        Assert.DoesNotContain(nameof(Account.Id), fieldNames); // the primary target field
        Assert.DoesNotContain(nameof(Account.Name), fieldNames); // Put(...) by AccountDataProvider
        Assert.DoesNotContain(nameof(Account.Industry), fieldNames); // Put(...) by AccountDataProvider
    }

    [Fact]
    public void Supply_TheFilledValue_SurvivesIntoTheReturnedRecord()
    {
        // Arrange
        SettingFiller filler = new(nameof(Account.NumberOfEmployees), 42);
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)provider.Supply();

        // Assert
        Assert.Equal(42, result.NumberOfEmployees);
    }

    [Fact]
    public void Supply_ForARequiredRelationship_AppliesTheSameFillerToTheGeneratedAncestorToo()
    {
        // Arrange - Contact requires an Account; the filler should see both records' own unset fields
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = provider.Supply();

        // Assert
        Assert.Contains(typeof(Contact), filler.RecordTypesSeen);
        Assert.Contains(typeof(Account), filler.RecordTypesSeen);
    }

    [Fact]
    public void Supply_ARelationshipsOwnScalarField_IsNeverTreatedAsUnset()
    {
        // Arrange - AccountId is a required relationship field, not a "nothing touched it" field
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = provider.Supply();

        // Assert
        Assert.DoesNotContain(nameof(Contact.AccountId), filler.FieldNamesSeen);
    }

    // Test doubles -----------------------------------------------------

    private sealed class RecordingFiller : IUnsetFieldFiller
    {
        public List<Type> RecordTypesSeen { get; } = [];

        public List<string> FieldNamesSeen { get; } = [];

        public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
        {
            this.RecordTypesSeen.Add(record.GetType());
            this.FieldNamesSeen.AddRange(unsetFields.Select(field => field.Name));
        }
    }

    private sealed class SettingFiller(string fieldName, object? value) : IUnsetFieldFiller
    {
        public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
        {
            PropertyInfo? field = unsetFields.SingleOrDefault(candidate => candidate.Name == fieldName);
            field?.SetValue(record, value);
        }
    }
}
