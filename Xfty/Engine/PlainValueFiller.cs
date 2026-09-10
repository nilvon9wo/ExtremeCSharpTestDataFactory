using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Values;
namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>Fills the plain (non-context-aware) default values on a clone of the test's template.</summary>
public static class PlainValueFiller
{
    public static object CloneAndCompletePlainValues(MasterTemplate template, object testTemplate)
    {
        object record = RecordCloneFactory.DeepClone(testTemplate);
        List<PropertyInfo> plainFields = [.. template.DefaultByField.Keys];
        plainFields.ForEach(field => FillPlainValue(template, record, field));
        return record;
    }

    public static List<object> CloneAndCompletePlainValues(MasterTemplate template, List<object> testTemplates) =>
        [.. testTemplates.Select(testTemplate => CloneAndCompletePlainValues(template, testTemplate))];

    private static void FillPlainValue(MasterTemplate template, object record, PropertyInfo field)
    {
        if (!FieldState.IsUnset(field, record))
        {
            return;
        }

        field.SetValue(record, template.DefaultByField[field].Get());
    }
}