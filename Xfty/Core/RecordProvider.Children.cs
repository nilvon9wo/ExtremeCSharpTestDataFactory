using System.Reflection;

namespace Net.Nowhereatall.Xfty.Core;

/// <summary>RecordProvider - downward generation (child collections), delegated to <see cref="RecordProviderChildConfig"/>.</summary>
public sealed partial class RecordProvider
{
    /// <summary>Add a fully-configured child collection. Repeatable.</summary>
    public RecordProvider With(ChildProvider childProvider)
    {
        this.childConfig.Add(childProvider);
        return this;
    }

    /// <summary>Shortcut: countPerParent children on childRelationshipField, everything else defaulted.</summary>
    public RecordProvider WithChildren(PropertyInfo childRelationshipField, int countPerParent) =>
        this.With(new ChildProvider(childRelationshipField).SetQuantity(countPerParent));

    /// <summary>Shortcut: one child on childRelationshipField.</summary>
    public RecordProvider WithChild(PropertyInfo childRelationshipField) =>
        this.With(new ChildProvider(childRelationshipField));
}
