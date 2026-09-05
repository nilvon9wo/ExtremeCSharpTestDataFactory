using System.Reflection;
using Net.Nowhereatall.Xfty.Core.Values;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>The second value pass: the context-aware expressions, run once the plain values, ancestors and lookups are all in place.</summary>
public sealed class ContextAwareValuePass
{
    private readonly Bundle bundle;
    private readonly GenerationContext context;
    private readonly MasterTemplate template;

    public ContextAwareValuePass(Bundle bundle, GenerationContext context, MasterTemplate template)
    {
        this.bundle = bundle;
        this.context = context;
        this.template = template;
    }

    public void Complete()
    {
        if (this.template.ContextAwareByField.Count == 0)
        {
            return;
        }

        List<object> records = this.bundle.PrimaryRecords()!;
        records
            .Select((record, row) => (record, row))
            .ToList()
            .ForEach(each => this.CompleteRow(each.record, each.row));
    }

    private void CompleteRow(object record, int row)
    {
        GenerationContext rowContext = this.context.ForRecord(record, this.bundle, row);
        HashSet<PropertyInfo> pendingContextAwareValues = [.. this.template.ContextAwareByField.Keys];
        this.template.OrderedValueFields()
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
        bool nothingToFill = !this.template.ContextAwareByField.TryGetValue(field, out IContextAwareExpression? expression)
            || field.GetValue(record) is not null;
        if (nothingToFill)
        {
            return;
        }

        field.SetValue(record, expression!.Get(scoped));
    }
}
