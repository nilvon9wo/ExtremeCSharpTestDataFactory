namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// Fabricates the placeholder identifier a record receives under
/// <see cref="Core.InsertMode.Mock"/>, in place of a real persistence
/// round-trip. The built-in <see cref="DefaultMockIdGenerator"/> covers
/// <c>string</c>, <c>int</c>, <c>long</c>, and <see cref="Guid"/> Ids; supply
/// your own for any other Id type, or for a project-specific Id shape -
/// per record type via <c>MasterTemplate&lt;T&gt;.WithMockIdGenerator(...)</c>,
/// or per call via <c>RecordProvider.SetMockIdGenerator(...)</c>, which
/// overrides the template's for that call's own primary records.
/// </summary>
public interface IMockIdGenerator
{
    /// <summary>
    /// The next placeholder Id. Called once per record that needs one; keep
    /// any sequence state on the instance. The returned value must be
    /// assignable to <see cref="MockIdContext.IdField"/>.
    /// </summary>
    object NextId(MockIdContext context);
}
