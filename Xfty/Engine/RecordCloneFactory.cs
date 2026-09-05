using System.Reflection;

namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>
/// Creates full-fidelity in-memory copies of a record via reflection over
/// every property. Apex's original relied on SObject.clone(...) (keep the Id,
/// timestamps, autonumbers, populated child relationships); a plain C# POCO
/// has no such built-in, so this copies every property directly instead -
/// the same guarantee (a copy stands in for the original everywhere), a
/// different mechanism.
/// </summary>
public static class RecordCloneFactory
{
    public static object DeepClone(object record)
    {
        object clone = Activator.CreateInstance(record.GetType())!;
        record.GetType().GetProperties().ToList().ForEach(property => CopyProperty(property, record, clone));
        return clone;
    }

    public static List<object> DeepClones(object record, int quantity) =>
        Enumerable.Range(0, quantity).Select(_ => DeepClone(record)).ToList();

    private static void CopyProperty(PropertyInfo property, object source, object destination)
    {
        if (property.CanWrite)
        {
            property.SetValue(destination, property.GetValue(source));
        }
    }
}
