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
        HashSet<PropertyInfo> pendingContextAwareValues = [.. this._template.ContextAwareByField.Keys];
        this._template.OrderedValueFields()
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
        bool hasExpression = this._template.ContextAwareByField.TryGetValue(
            field,
            out IContextAwareExpression? expression
        );
        bool nothingToFill = !hasExpression || !FieldState.IsUnset(field, record);
        if (nothingToFill)
        {
            return;
        }

        field.SetValue(record, expression!.Get(scoped));
    }
}