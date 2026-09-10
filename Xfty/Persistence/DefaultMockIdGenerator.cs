using System.Threading;

using Net.NowhereAtAll.Xfty.Core;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// The built-in <see cref="IMockIdGenerator"/>: one process-wide incrementing
/// sequence, rendered as the Id field's own type - <c>"mock-N"</c> for a
/// <c>string</c>, <c>N</c> for <c>int</c>/<c>long</c>, a fresh
/// <see cref="Guid"/> for <see cref="Guid"/>. Any other Id type throws
/// <see cref="XftyConfigurationException"/> naming the type and pointing at
/// <c>WithMockIdGenerator(...)</c> - supply your own.
///
/// The sequence is <c>static</c>, so every instance shares it and no mocked
/// Id repeats for the life of the process.
/// </summary>
public sealed class DefaultMockIdGenerator : IMockIdGenerator
{
    /// <summary>The shared instance - what XFTY uses when nothing else is configured.</summary>
    public static DefaultMockIdGenerator Instance { get; } = new();

    private const string MockStringPrefix = "mock-";

    private static int s_sequence;

    public object NextId(MockIdContext context) =>
        RenderedId(UnwrappedIdType(context.IdField.PropertyType), NextSequence(), context);

    /// <summary>The next <c>"mock-N"</c> string - the shape the old string-only mocker produced, kept for callers that just want one.</summary>
    internal static string NextMockString() => $"{MockStringPrefix}{NextSequence()}";

    private static int NextSequence() => Interlocked.Increment(ref s_sequence);

    private static Type UnwrappedIdType(Type idFieldType) => Nullable.GetUnderlyingType(idFieldType) ?? idFieldType;

    private static object RenderedId(Type idType, int sequence, MockIdContext context) =>
        idType switch
        {
            _ when idType == typeof(string) => $"{MockStringPrefix}{sequence}",
            _ when idType == typeof(int) => sequence,
            _ when idType == typeof(long) => (long)sequence,
            _ when idType == typeof(Guid) => Guid.NewGuid(),
            _ => throw NoBuiltInGenerator(idType, context),
        };

    private static XftyConfigurationException NoBuiltInGenerator(Type idType, MockIdContext context) =>
        new(
            $"InsertMode.Mock has no built-in Id generator for a {idType.Name} Id on {context.RecordType.Name}. "
            + "Supply an IMockIdGenerator via MasterTemplate<T>.WithMockIdGenerator(...) or "
            + "RecordProvider.SetMockIdGenerator(...).");
}