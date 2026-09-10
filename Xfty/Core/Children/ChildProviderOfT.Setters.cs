using Net.NowhereAtAll.Xfty.Lookup;

namespace Net.NowhereAtAll.Xfty.Core.Children;

/// <summary>ChildProvider&lt;TChild&gt; - the child-collection settings (quantity, insert mode, inclusivity, variant, nested grandchildren). Each forwards to the identically-named <see cref="ChildProvider"/> method, which carries the documentation.</summary>
public sealed partial class ChildProvider<TChild>
{
    public ChildProvider<TChild> SetQuantity(int quantity) =>
        this.Forwarding(() => this.inner.SetQuantity(quantity));

    public ChildProvider<TChild> SetInsertMode(InsertMode insertMode) =>
        this.Forwarding(() => this.inner.SetInsertMode(insertMode));

    public ChildProvider<TChild> SetInclusivity(InsertInclusivity inclusivity) =>
        this.Forwarding(() => this.inner.SetInclusivity(inclusivity));

    public ChildProvider<TChild> WithVariant(ILookupKey variantKey) =>
        this.Forwarding(() => this.inner.WithVariant(variantKey));

    public ChildProvider<TChild> With(ChildProvider? grandchildProvider) =>
        this.Forwarding(() => this.inner.With(grandchildProvider));
}
