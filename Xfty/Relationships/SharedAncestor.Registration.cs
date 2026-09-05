using Net.Nowhereatall.Xfty.Lookup;

namespace Net.Nowhereatall.Xfty.Relationships;

/// <summary>SharedAncestor - registering a shared record (Put*).</summary>
public sealed partial class SharedAncestor
{
    /// <summary>Register record. Disambiguates by Id: with one, a fixed value; without, an override template.</summary>
    public static SharedAncestorProvider Put(string name, object? record) =>
        IdOf(record) is not null ? PutAsValue(name, record!) : PutAsTemplate(name, record);

    /// <summary>Register an override template; the shared record is generated from it in the pre-phase.</summary>
    public static SharedAncestorProvider PutAsTemplate(string name, object? template) => Get(name).Provider().WithTemplate(template);

    /// <summary>Register a record the test built itself; used as-is.</summary>
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
