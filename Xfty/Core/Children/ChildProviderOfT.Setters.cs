using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Core.Children;

/// <summary>ChildProvider&lt;TChild&gt; - child-collection settings (quantity, insert mode, inclusivity, variant, grandchildren).</summary>
public sealed partial class ChildProvider<TChild>
{
    public ChildProvider<TChild> SetQuantity(int quantity)
    {
        _ = this._inner.SetQuantity(quantity);
        return this;
    }

    public ChildProvider<TChild> SetInsertMode(InsertMode insertMode)
    {
        _ = this._inner.SetInsertMode(insertMode);
        return this;
    }

    public ChildProvider<TChild> SetInclusivity(InsertInclusivity inclusivity)
    {
        _ = this._inner.SetInclusivity(inclusivity);
        return this;
    }

    public ChildProvider<TChild> WithVariant(ILookupKey variantKey)
    {
        _ = this._inner.WithVariant(variantKey);
        return this;
    }

    public ChildProvider<TChild> With(ChildProvider? grandchildProvider)
    {
        _ = this._inner.With(grandchildProvider);
        return this;
    }
}