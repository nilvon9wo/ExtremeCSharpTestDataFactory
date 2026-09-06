namespace Net.NowhereAtAll.Xfty.Core;

/// <summary>One configured child collection on a <see cref="Bundle"/>: its bundle + which primary row each child row belongs to.</summary>
public sealed class BundleChildEntry(Bundle bundle, List<int> parentRowByChildRow)
{
    public Bundle Bundle { get; } = bundle;

    public List<int> ParentRowByChildRow { get; } = parentRowByChildRow;
}
