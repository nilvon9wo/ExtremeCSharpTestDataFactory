namespace Net.Nowhereatall.Xfty.Core.Predicates;

/// <summary>
/// Shared plumbing for the single-field predicates: holds the field accessor
/// and reads it null-safely (a null record has no field value). Apex's
/// equivalents took a runtime <c>SObjectField</c> token and read it off an
/// untyped <c>SObject</c> via <c>record.get(field)</c>; C# has no dynamic
/// record base type, so this takes a real, statically-checked accessor
/// instead - a strict improvement, not a workaround.
/// </summary>
public abstract class FieldPredicateBase<TRecord, TValue> : IRecordPredicate<TRecord>
    where TRecord : class
{
    private readonly Func<TRecord, TValue> field;

    protected FieldPredicateBase(Func<TRecord, TValue> field)
    {
        this.field = field;
    }

    public abstract bool IsSatisfiedBy(TRecord? record);

    protected TValue? ActualValue(TRecord? record) =>
        record is null
            ? default
            : this.field(record);
}
