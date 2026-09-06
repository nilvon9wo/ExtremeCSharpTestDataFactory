using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Net.NowhereAtAll.Xfty.Values;

namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>MasterTemplate - the untyped Put(field, object) overload, routed to the right typed overload by runtime type.</summary>
public sealed partial class MasterTemplate
{
    /// <summary>
    /// Convenience overload, routed by runtime type: a relationship is
    /// rejected (its requiredness has to be stated via PutRequired/PutOptional);
    /// anything else is treated as an exact literal.
    /// </summary>
    public MasterTemplate Put(PropertyInfo field, object? value) =>
        value switch
        {
            IDeferredExpression deferred => this.Put(field, deferred),
            IContextAwareExpression contextAware => this.Put(field, contextAware),
            IValueExpression valueExpression => this.Put(field, valueExpression),
            IDefaultRelationship => throw RelationshipsNeedPutRequiredOrOptional(),
            _ => this.Put(field, new LiteralExpression(value)),
        };

    private static XftyConfigurationException RelationshipsNeedPutRequiredOrOptional() =>
        new("Relationships must be added with PutRequired(...) or PutOptional(...), not Put(...).");
}
