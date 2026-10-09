using System.Collections;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Core.MasterTemplates;
using Net.NowhereAtAll.Xfty.Demo;

namespace Net.NowhereAtAll.Xfty.Test.Core;

/// <summary>
/// Proves <see cref="OrderedFieldMap{TValue}"/>'s non-generic enumeration
/// (what a plain <c>foreach</c> over an <see cref="IEnumerable"/> uses) keeps
/// insertion order, like the generic one MasterTemplate itself relies on.
/// </summary>
public class OrderedFieldMapTest
{
    [Fact]
    public void GetEnumerator_NonGeneric_YieldsEntriesInInsertionOrder()
    {
        // Arrange
        PropertyInfo name = Field.Of<Account>(x => x.Name);
        PropertyInfo industry = Field.Of<Account>(x => x.Industry);
        OrderedFieldMap<string> map = new();
        map.Set(name, "first");
        map.Set(industry, "second");
        IEnumerable untyped = map;

        // Act
        List<object> entries = [.. untyped.Cast<object>()];

        // Assert
        Assert.Equal(
            [
                new KeyValuePair<PropertyInfo, string>(name, "first"),
                new KeyValuePair<PropertyInfo, string>(industry, "second"),
            ],
            entries
        );
    }
}