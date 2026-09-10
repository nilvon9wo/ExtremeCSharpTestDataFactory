using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Relationships;

/// <summary>SharedAncestor - registering a shared record (Put*).</summary>
public sealed partial class SharedAncestor
{
    /// <summary>
    /// Register record, disambiguating a fixed already-saved value from an
    /// override template. Because there is no Provider in scope yet to ask
    /// for the real key field, it checks a property named <c>Id</c>; a keyed
    /// record whose key is named otherwise still resolves correctly if you
    /// register it with <see cref="PutAsTemplate"/> (the resolver then
    /// notices the key is set and uses the record as-is), but for a fixed
    /// value that must be treated as pre-saved *before* resolution - a
    /// cycle break, or reading <see cref="GetId"/> straight away - call
    /// <see cref="PutAsValue"/> explicitly.
    /// </summary>
    public static SharedAncestorProvider Put(string name, object? record) =>
        IdOf(record) is not null ? PutAsValue(name, record!) : PutAsTemplate(name, record);

    /// <summary>Register an override template; the shared record is generated from it in the pre-phase.</summary>
    public static SharedAncestorProvider PutAsTemplate(string name, object? template) => Get(name).Provider().WithTemplate(template);

    /// <summary>
    /// Register a record the test built itself; used exactly as-is, no
    /// generation. Its "already persisted?" flag is a best guess from an
    /// <c>Id</c>-named property until the ancestor is first referenced in a
    /// Supply*() call, at which point the Provider's real key field corrects
    /// it (<see cref="Resolution"/>).
    /// </summary>
    public static SharedAncestorProvider PutAsValue(string name, object record)
    {
        SharedAncestor ancestor = Get(name);
        ancestor.resolvedRecord = record;
        ancestor.resolvedBundle = null;
        ancestor._resolvedRecordIsPersisted = IdOf(record) is not null;
        return ancestor.Provider();
    }

    /// <summary>Register just the Provider variant that generates the shared record.</summary>
    public static SharedAncestorProvider Put(string name, ILookupKey variantKey) => Get(name).Provider().FromVariant(variantKey);

    /// <summary>Put(name, record) (same Id-disambiguation), applied only if name is not registered yet.</summary>
    public static SharedAncestorProvider PutIfAbsent(string name, object? record)
    {
        SharedAncestor ancestor = Get(name);
        return ancestor.IsUnregistered() ? Put(name, record) : ancestor.Provider();
    }

    /// <summary>As PutIfAbsent(string,object), pinning the variant instead of a template.</summary>
    public static SharedAncestorProvider PutIfAbsent(string name, ILookupKey variantKey)
    {
        SharedAncestor ancestor = Get(name);
        return ancestor.IsUnregistered() ? Put(name, variantKey) : ancestor.Provider();
    }

    private SharedAncestorProvider Provider() => this.source ??= new SharedAncestorProvider(this);
}
