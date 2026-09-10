#if NETSTANDARD2_0
using System.Runtime.Serialization;
#else
using System.Runtime.CompilerServices;
#endif

namespace Net.NowhereAtAll.Xfty.Internal;

/// <summary>
/// Builds a blank instance of a record type for the reflection value passes to
/// fill. XFTY never has constructor arguments to offer - it discovers values
/// field-by-field - so it needs an instance first and populates it afterward.
///
/// The public parameterless constructor is used when there is one (it runs
/// field initializers and any parameterless-constructor logic the type
/// relies on). A type without one - a positional <c>record class Foo(...)</c>,
/// whose only constructor is its primary constructor - falls back to an
/// uninitialized instance; every field is set by reflection immediately after
/// either way, so nothing is left unassigned that XFTY would otherwise assign.
/// Value types always have a parameterless constructor, so they never reach
/// the fallback.
/// </summary>
internal static class BlankInstances
{
    public static object Of(Type recordType) =>
        HasPublicParameterlessConstructor(recordType)
            ? Activator.CreateInstance(recordType)!
            : Uninitialized(recordType);

    private static bool HasPublicParameterlessConstructor(Type recordType) =>
        recordType.IsValueType || recordType.GetConstructor(Type.EmptyTypes) is not null;

    private static object Uninitialized(Type recordType) =>
#if NETSTANDARD2_0
        FormatterServices.GetUninitializedObject(recordType);
#else
        RuntimeHelpers.GetUninitializedObject(recordType);
#endif
}