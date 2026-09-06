using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.EntityFrameworkCore;

/// <summary>
/// The real, database-backed <see cref="IPersistenceGateway"/> - the piece
/// that makes <c>InsertMode.Now</c> and <c>.DepthBatched()</c> actually
/// persist, proven against a real Entity Framework Core <see cref="DbContext"/>
/// rather than a mock. Register it with
/// <c>recordProvider.SetPersistenceGateway(new EfPersistenceGateway(dbContext))</c>.
///
/// A string-typed Id with no value is filled with a fresh GUID before
/// <c>Add</c> - the common shape for a string primary key, which EF Core has
/// no built-in generator for (unlike an integer identity column, which EF
/// already populates after <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/>
/// on its own, left untouched here). One SaveChangesAsync call per
/// depth-batched layer, matching <see cref="Persistence.DepthBatchedInserter"/>'s
/// one-call-per-type-per-layer contract.
/// </summary>
public sealed class EfPersistenceGateway(DbContext dbContext) : IPersistenceGateway
{
    public async Task Insert(List<object> records, PropertyInfo idField)
    {
        records.ForEach(record => this.AddOne(record, idField));
        _ = await dbContext.SaveChangesAsync();
    }

    private void AddOne(object record, PropertyInfo idField)
    {
        if (idField.PropertyType == typeof(string) && idField.GetValue(record) is null)
        {
            idField.SetValue(record, Guid.NewGuid().ToString());
        }

        _ = dbContext.Add(record);
    }
}
