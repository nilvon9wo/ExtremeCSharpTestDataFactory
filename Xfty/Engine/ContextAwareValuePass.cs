using System.Reflection;
using Net.NowhereAtAll.Xfty.Core;
using Net.NowhereAtAll.Xfty.Core.Bundles;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Values;
namespace Net.NowhereAtAll.Xfty.Engine;

/// <summary>
/// The second value pass: the context-aware expressions, run once the plain values, ancestors and lookups are all in
/// place.
/// </summary>
public sealed class ContextAwareValuePass(Bundle bundle, GenerationContext context, MasterTemplate template)
{
    private readonly Bundle _bundle = bundle;
    private readonly GenerationContext _context = context;
    private readonly MasterTemplate _template = template;

    public void Complete()
    {
        if (this._template.ContextAwareByField.Count == 0)
        {
            return;
        }

        List<object> records = this._bundle.PrimaryRecords()!;
        records
            .Select((record, row) => (record, row))
            .ToList()
            .ForEach(each => this.CompleteRow(each.record, each.row));
    }

    private void CompleteRow(object record, int row)
    {
        GenerationContext rowContext = this._context.ForRecord(record, this._bundle, row);
        List<PropertyInfo> contextAwareFields = [.. this._template.ContextAwareByField.Keys];
        HashSet<PropertyInfo> pendingContextAwareValues = [.. contextAwareFields];
        contextAwareFields
            .ForEach(field => this.CompleteFieldAndUnmark(record, rowContext, field, pendingContextAwareValues));
    }

    private void CompleteFieldAndUnmark(
        object record,
        GenerationContext rowContext,
        PropertyInfo field,
        HashSet<PropertyInfo> pendingContextAwareValues)
    {
        GenerationContext scoped = rowContext.ForValueField(field, pendingContextAwareValues);
        this.CompleteField(record, scoped, field);
        _ = pendingContextAwareValues.Remove(field);
    }

    private void CompleteField(object record, GenerationContext scoped, PropertyInfo field)
    {
        if (!FieldState.IsUnset(field, record))
        {
            return;
        }

        IContextAwareExpression expression = this._template.ContextAwareByField[field];
        field.SetValue(record, expression.Get(scoped));
    }
}