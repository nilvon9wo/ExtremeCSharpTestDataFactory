using global::AutoFixture;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.AutoFixture;

/// <summary>
/// Points an <see cref="IFixture"/> at XFTY: <c>fixture.Create&lt;T&gt;()</c>/
/// <c>fixture.Create&lt;List&lt;T&gt;&gt;()</c> for any T with a Provider
/// registered in lookup return a real XFTY-generated record (required
/// relationships resolved, shared ancestors deduplicated) instead of
/// AutoFixture's own auto-property population; every other type is
/// unaffected and still comes from AutoFixture as usual.
///
/// <code>
/// IFixture fixture = new Fixture().Customize(new XftyCustomization(lookup));
/// Contact contact = fixture.Create&lt;Contact&gt;();
/// </code>
///
/// insertMode defaults to <see cref="InsertMode.Mock"/> - in-memory Ids, no
/// real DML - matching AutoFixture's own scope. Pass a different mode (with
/// <see cref="RecordProvider.SetPersistenceGateway"/> configured on the
/// Providers involved, where relevant) for a customization that should
/// insert for real.
///
/// inclusivity defaults to <see cref="InsertInclusivity.Required"/>, not
/// RecordProvider's own default of None - matching AutoFixture's own
/// philosophy (populate everything; a test overrides only what it cares
/// about) rather than XFTY's (declare everything you want generated).
/// Without this, fixture.Create&lt;Contact&gt;() would leave AccountId null,
/// surprising anyone used to AutoFixture always producing a complete object.
/// </summary>
public sealed class XftyCustomization(
    IProviderLookup lookup,
    InsertMode insertMode = InsertMode.Mock,
    InsertInclusivity inclusivity = InsertInclusivity.Required) : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customizations.Insert(0, new XftySpecimenBuilder(lookup, insertMode, inclusivity));
}
