using System.Reflection;

namespace Net.NowhereAtAll.Xfty.Persistence;

/// <summary>
/// The one seam a real backing store plugs into. <see cref="InsertMode.Now"/>
/// throws <see cref="NotSupportedException"/> everywhere in this library
/// unless a gateway is supplied (<c>RecordProvider.SetPersistenceGateway</c>,
/// or passed directly to <see cref="DepthBatchedInserter"/> /
/// <see cref="DeferredInsertBuffer"/> / <see cref="DeferredInserter"/>).
///
/// Deliberately the smallest possible surface: persist a batch of records of
/// one type, and write each one's generated identifier back onto <paramref
/// name="idField"/> via reflection - the same shape <see cref="IdMocker"/>
/// already uses for <see cref="InsertMode.Mock"/>, so a real gateway is a
/// drop-in swap. Nothing here mentions any particular storage technology; an
/// implementation is free to use Entity Framework Core, Dapper, raw ADO.NET,
/// or an in-memory fake for tests.
/// </summary>
public interface IPersistenceGateway
{
    /// <summary>
    /// Persist every record in <paramref name="records"/> - all the same
    /// type - and set <paramref name="idField"/> on each to its real,
    /// generated identifier.
    /// </summary>
    Task Insert(List<object> records, PropertyInfo idField);
}
