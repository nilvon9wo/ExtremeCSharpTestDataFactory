namespace Net.Nowhereatall.Xfty.Persistence;

/// <summary>A record paired with its position in the list or pass that is working on it.</summary>
public sealed class IndexedRecord(int index, object record)
{
    public int Index { get; } = index;

    public object Record { get; } = record;
}
