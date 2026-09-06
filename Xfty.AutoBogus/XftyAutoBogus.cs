using global::AutoBogus;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.AutoBogus;

/// <summary>
/// The one-line form of <see cref="XftyAutoBogusOverride"/>: an
/// <see cref="IAutoFaker"/> whose <c>Generate&lt;T&gt;()</c>/<c>Generate&lt;T&gt;(count)</c>
/// for any T with a Provider registered in lookup return a real
/// XFTY-generated record instead of AutoBogus's own generation; every other
/// type is unaffected.
///
/// <code>
/// IAutoFaker faker = XftyAutoBogus.CreateFaker(lookup);
/// Contact contact = faker.Generate&lt;Contact&gt;();
/// </code>
///
/// insertMode defaults to <see cref="InsertMode.Mock"/> - in-memory Ids, no
/// real DML - matching AutoBogus's own offline scope. inclusivity defaults
/// to <see cref="InsertInclusivity.Required"/>, not <see cref="RecordProvider"/>'s
/// own default of <see cref="InsertInclusivity.None"/> - matching
/// AutoBogus's own "hand back a complete object" philosophy, the same
/// reasoning the AutoFixture pairing's own <c>XftyCustomization</c>
/// (in <c>Xfty.AutoFixture</c>) uses.
/// </summary>
public static class XftyAutoBogus
{
    public static IAutoFaker CreateFaker(
        IProviderLookup lookup,
        InsertMode insertMode = InsertMode.Mock,
        InsertInclusivity inclusivity = InsertInclusivity.Required) =>
        AutoFaker.Create(builder => builder.WithOverride(new XftyAutoBogusOverride(lookup, insertMode, inclusivity)));
}
