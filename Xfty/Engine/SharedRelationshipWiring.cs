using System.Reflection;
using Net.Nowhereatall.Xfty.Core;
using Net.Nowhereatall.Xfty.Engine;
using Net.Nowhereatall.Xfty.Relationships;
namespace Net.Nowhereatall.Xfty.Engine;

/// <summary>Wires a shared ancestor into a bundle: one record stands in for every child at its field, resolved once per test, then repeated quantity times.</summary>
public sealed class SharedRelationshipWiring(GenerationContext context, ISharedRelationship shared)
{
    private readonly GenerationContext context = context;
    private readonly ISharedRelationship shared = shared;

    public void Wire(Bundle bundle, PropertyInfo field, int quantity)
    {
        object? record = this.shared.ResolveSharedRecord(this.context);
        this.AssertSavedConsistently();
        List<object> children = Repeat(record!, quantity);
        _ = bundle.Put(field, children);
        this.PlaceResolvedBundle(bundle, field);
    }

    private void AssertSavedConsistently()
    {
        bool safe = this.context.InsertMode != InsertMode.Now || this.shared.IsResolvedRecordPersisted;
        if (safe)
        {
            return;
        }

        throw new XftyConfigurationException(
            $"Shared ancestor \"{this.Name()}\" was resolved without being inserted, but this NOW run would carry "
            + $"its Id onto inserted records. Use a consistent insert mode across the test, or register a saved "
            + $"record with SharedAncestor.Put(\"{this.Name()}\", record).");
    }

    private static List<object> Repeat(object record, int times) =>
        [.. Enumerable.Repeat(record, times)];

    private void PlaceResolvedBundle(Bundle bundle, PropertyInfo field) =>
        bundle.Put(field, this.shared.GetResolvedBundle());

    private string Name() => this.shared.SharedName;
}
