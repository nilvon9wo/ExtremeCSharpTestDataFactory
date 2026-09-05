namespace Net.Nowhereatall.Xfty.Core;

/// <summary>One configured child collection on a <see cref="Bundle"/>: its bundle + which primary row each child row belongs to.</summary>
public sealed class BundleChildEntry
{
    public Bundle Bundle { get; }

    public List<int> ParentRowByChildRow { get; }

    public BundleChildEntry(Bundle bundle, List<int> parentRowByChildRow)
    {
        this.Bundle = bundle;
        this.ParentRowByChildRow = parentRowByChildRow;
    }
}
