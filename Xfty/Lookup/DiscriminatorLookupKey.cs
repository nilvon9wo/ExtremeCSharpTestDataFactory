using System.Linq.Expressions;
using System.Reflection;
using Net.Nowhereatall.Xfty.Predicates;

namespace Net.Nowhereatall.Xfty.Lookup;

/// <summary>
/// Selects a Provider variant by one property's value on the record - the
/// direct analog of a single-table "record type" discriminator: Entity
/// Framework Core's Table-Per-Hierarchy discriminator column, or any
/// hand-rolled enum/string flag on one table distinguishing several kinds of
/// the same entity.
///
/// A thin, named convenience over <see cref="FlavouredLookupKey"/>'s general
/// predicate mechanism, which already does the actual matching -
/// <c>DiscriminatorLookupKey.Get&lt;Account&gt;(x => x.AccountType, "Person")</c>
/// reads better than spelling out <see cref="FieldPredicateFactory.EqualTo"/>
/// by hand for the common "one column, one value" case. Safe to call more
/// than once for the same (type, field, value) - later calls return the same
/// flyweight without re-adding the predicate. See <c>Xfty.EntityFrameworkCore</c>
/// for deriving these automatically from a <c>DbContext</c>'s configured
/// discriminator.
/// </summary>
public static class DiscriminatorLookupKey
{
    private static readonly HashSet<string> ConfiguredHashKeys = [];

    public static FlavouredLookupKey Get<TRecord>(Expression<Func<TRecord, object?>> discriminatorField, object? value)
    {
        PropertyInfo field = Field.Of(discriminatorField);
        FlavouredLookupKey key = FlavouredLookupKey.Get(typeof(TRecord), $"{field.Name}={value}");
        if (ConfiguredHashKeys.Add(key.HashKey))
        {
            _ = key.Matching(FieldPredicateFactory.EqualTo(field, value));
        }

        return key;
    }
}
