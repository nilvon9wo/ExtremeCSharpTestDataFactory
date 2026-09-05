namespace Net.Nowhereatall.Xfty.Core.Persistence;

/// <summary>A record paired with its position in the list or pass that is working on it.</summary>
public sealed class IndexedRecord
{
    public int Index { get; }

    public object Record { get; }

    public IndexedRecord(int index, object record)
    {
        this.Index = index;
        this.Record = record;
    }
}
