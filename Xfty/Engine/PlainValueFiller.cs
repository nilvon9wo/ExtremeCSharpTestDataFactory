using System.Reflection;
using Net.Nowhereatall.Xfty.Values;

using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>Fills the plain (non-context-aware) default values on a clone of the test's template.</summary>
public static class PlainValueFiller
{
    public static object CloneAndCompletePlainValues(MasterTemplate template, object testTemplate)
    {
        object record = RecordCloneFactory.DeepClone(testTemplate);
        template.OrderedValueFields().ForEach(field => FillPlainValue(template, record, field));
        return record;
    }

    public static List<object> CloneAndCompletePlainValues(MasterTemplate template, List<object> testTemplates) =>
        testTemplates.Select(testTemplate => CloneAndCompletePlainValues(template, testTemplate)).ToList();

    private static void FillPlainValue(MasterTemplate template, object record, PropertyInfo field)
    {
        bool nothingToFill = !template.DefaultByField.TryGetValue(field, out IValueExpression? strategy)
            || field.GetValue(record) is not null;
        if (nothingToFill)
        {
            return;
        }

        field.SetValue(record, strategy!.Get());
    }
}
