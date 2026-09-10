using Net.NowhereAtAll.Xfty.Relationships;

namespace Net.NowhereAtAll.Xfty.Core.MasterTemplates;

/// <summary>
/// A relationship configured on a <see cref="MasterTemplate"/>, together with
/// whether it is <see cref="IsRequired"/> (generated at every inclusivity) or
/// optional (generated only when the call asks for it via inclusivity or
/// <c>IncludeOptional</c>). A field carries one relationship, never both
/// requirednesses.
/// </summary>
internal sealed record RelationshipConfig(IDefaultRelationship Relationship, bool IsRequired);