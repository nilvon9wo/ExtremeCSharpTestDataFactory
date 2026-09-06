using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>
/// An optional collaborator, set via <see cref="RecordProvider.SetUnsetFieldFiller"/>,
/// that fills in fields a Provider's Master Template never configured at
/// all - not a field XFTY resolved to null or some other default, one
/// nothing (no default value, no override template, no Put(...), no
/// relationship) ever touched.
///
/// Runs once per generated record, after every other value/relationship
/// pass has completed but before <see cref="InsertMode.Now"/> hands the
/// record to <see cref="Persistence.IPersistenceGateway"/> - late enough
/// that a filler never fights XFTY for a field XFTY actually cares about,
/// early enough that a real database's NOT NULL columns still see a value.
///
/// See Xfty.AutoFixture for the bundled AutoFixture-backed implementation;
/// this interface has no dependency on AutoFixture (or anything else) so
/// the base package never needs one.
/// </summary>
public interface IUnsetFieldFiller
{
    /// <summary>
    /// Fill in as many of unsetFields on record as this filler can/wants to
    /// - it need not fill every one. Mutates record in place; the return
    /// value is not used.
    /// </summary>
    void Fill(object record, IReadOnlyCollection<PropertyInfo> unsetFields);
}
