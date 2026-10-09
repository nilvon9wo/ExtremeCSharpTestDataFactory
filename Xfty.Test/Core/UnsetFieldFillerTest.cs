using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Core.RecordProviders;
using Net.NowhereAtAll.Xfty.Demo;
using Net.NowhereAtAll.Xfty.Engine;
using Net.NowhereAtAll.Xfty.Lookup;
using Net.NowhereAtAll.Xfty.Values;
using NSubstitute;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves the IUnsetFieldFilling hook RecordProvider/RecordFactory expose:
/// which fields count as "unset" (see also MasterTemplateTest.IsConfigured),
/// when the hook fires, and that it reaches generated ancestors too. The
/// bundled AutoFixture-backed implementation is proven separately, in
/// Xfty.AutoFixture.Test - this file uses a recording test double instead,
/// to keep the "what does core actually guarantee" contract independent of
/// any one filler implementation.
/// </summary>
public class UnsetFieldFillerTest
{
    private static IProviderLocating Lookup() =>
        ProviderLookups.Of(new Dictionary<IRecordIdentifying, IRecordProviding>
        {
            [LookupKey.Get<Account>()] = new AccountDataProvider(),
            [LookupKey.Get<Contact>()] = new ContactDataProvider(),
        });

    [Fact]
    public async Task Supply_WithNoFillerConfigured_LeavesUnconfiguredFieldsAtTheirDefault()
    {
        // Arrange
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock);

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert - AccountDataProvider's Master Template never puts NumberOfEmployees
        Assert.Null(result.NumberOfEmployees);
    }

    [Fact]
    public async Task Supply_WithAFillerConfigured_FillsOnlyFieldsTheMasterTemplateNeverConfigured()
    {
        // Arrange
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        List<string> fieldNames = [.. filler.FieldNamesSeen];
        Assert.Contains(nameof(Account.NumberOfEmployees), fieldNames); // never Put(...)
        Assert.Contains(nameof(Account.AnnualRevenue), fieldNames); // never Put(...)
        Assert.DoesNotContain(nameof(Account.Id), fieldNames); // the primary target field
        Assert.DoesNotContain(nameof(Account.Name), fieldNames); // Put(...) by AccountDataProvider
        Assert.DoesNotContain(nameof(Account.Industry), fieldNames); // Put(...) by AccountDataProvider
    }

    [Fact]
    public async Task Supply_TheFilledValue_SurvivesIntoTheReturnedRecord()
    {
        // Arrange
        SettingFiller filler = new(nameof(Account.NumberOfEmployees), 42);
        RecordProvider provider = new RecordProvider(typeof(Account), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        Account result = (Account)await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Equal(42, result.NumberOfEmployees);
    }

    [Fact]
    public async Task Supply_ForARequiredRelationship_AppliesTheSameFillerToTheGeneratedAncestorToo()
    {
        // Arrange - Contact requires an Account; the filler should see both records' own unset fields
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.Contains(typeof(Contact), filler.RecordTypesSeen);
        Assert.Contains(typeof(Account), filler.RecordTypesSeen);
    }

    [Fact]
    public async Task Supply_ARelationshipsOwnScalarField_IsNeverTreatedAsUnset()
    {
        // Arrange - AccountId is a required relationship field, not a "nothing touched it" field
        RecordingFiller filler = new();
        RecordProvider provider = new RecordProvider(typeof(Contact), Lookup())
            .SetInsertMode(InsertMode.Mock)
            .SetInclusivity(InsertInclusivity.Required)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        Assert.DoesNotContain(nameof(Contact.AccountId), filler.FieldNamesSeen);
    }

    // Test doubles -----------------------------------------------------

    private sealed class RecordingFiller : IUnsetFieldFilling
    {
        public List<Type> RecordTypesSeen { get; } = [];

        public List<string> FieldNamesSeen { get; } = [];

        public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
        {
            this.RecordTypesSeen.Add(record.GetType());
            this.FieldNamesSeen.AddRange(unsetFields.Select(field => field.Name));
        }
    }

    private sealed class SettingFiller(string fieldName, object? value) : IUnsetFieldFilling
    {
        public void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields)
        {
            PropertyInfo? field = unsetFields.SingleOrDefault(candidate => candidate.Name == fieldName);
            field?.SetValue(record, value);
        }
    }

    [Fact]
    public async Task Supply_WhenTheMasterTemplateConfiguresEveryWritableField_NeverCallsTheFiller()
    {
        // Arrange - Tag has only its key and one Put(...) field: nothing left unset
        IUnsetFieldFilling filler = Substitute.For<IUnsetFieldFilling>();
        IProviderLocating tagLookup = ProviderLookups.Of(new Dictionary<IRecordIdentifying, IRecordProviding>
        {
            [LookupKey.Get<FullyConfiguredTag>()] = new FullyConfiguredTagProvider(),
        });
        RecordProvider provider = new RecordProvider(typeof(FullyConfiguredTag), tagLookup)
            .SetInsertMode(InsertMode.Mock)
            .SetUnsetFieldFiller(filler);

        // Act
        _ = await provider.Supply().ConfigureAwait(true);

        // Assert
        filler.DidNotReceive().Fill(Arg.Any<object>(), Arg.Any<IReadOnlyCollection<PropertyInfo>>());
    }
}
file sealed class FullyConfiguredTag
{
    public string? Id { get; set; }

    public string? Label { get; set; }
}

file sealed class FullyConfiguredTagProvider : IRecordProviding
{
    public MasterTemplate MasterTemplate { get; } = new MasterTemplate(Field.Of<FullyConfiguredTag>(x => x.Id))
        .Put<FullyConfiguredTag>(x => x.Label, new LiteralExpression("tag"));

    public PropertyInfo PrimaryTargetField => Field.Of<FullyConfiguredTag>(x => x.Id);

    public Task<Bundle> CreateBundle(GenerationContext context, List<object> templateRecords) =>
        RecordFactory.CreateBundle(context, this.MasterTemplate, templateRecords);
}