using System.Reflection;
using Net.NowhereAtAll.Xfty.Relationships;
using Xunit.v3;

namespace Net.NowhereAtAll.Xfty.Xunit;

/// <summary>
/// Resets <see cref="SharedAncestor"/>'s registry before and after the
/// decorated test method - or, applied to a whole test class, before and
/// after every test method in it. A consumer never has to hand-wire a base
/// test class's constructor/Dispose or an xUnit fixture around
/// <see cref="SharedAncestor.ResetAllForTesting"/> themselves; this is that
/// wiring, packaged.
///
/// <code>
/// [IsolatesSharedAncestor]
/// public class MyTests
/// {
///     [Fact]
///     public void FirstTest()
///     {
///         SharedAncestor.Put("hq", new Account());
///         // ... nothing here leaks into FirstTest, or out to any later test
///     }
/// }
/// </code>
///
/// Resetting *before* protects this test from whatever an earlier,
/// non-isolated test left behind; resetting *after* protects every later
/// test from this one, whether or not that test is itself isolated.
/// Verified against xUnit v3's actual runner - <c>Before</c>/<c>After</c>
/// genuinely fire per test method, including when the attribute is applied
/// at the class level rather than repeated on every method.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class IsolatesSharedAncestorAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test) => SharedAncestor.ResetAllForTesting();

    public override void After(MethodInfo methodUnderTest, IXunitTest test) => SharedAncestor.ResetAllForTesting();
}
