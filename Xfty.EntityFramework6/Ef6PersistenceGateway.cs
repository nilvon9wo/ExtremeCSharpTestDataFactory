using System.Data.Entity;
using System.Reflection;
using Net.NowhereAtAll.Xfty.Persistence;

namespace Net.NowhereAtAll.Xfty.EntityFramework6;

/// <summary>
/// The classic-EF6-backed <see cref="IPersistenceGateway"/> - the piece that
/// makes <c>InsertMode.Now</c> and <c>.DepthBatched()</c> actually persist
/// through <see cref="System.Data.Entity.DbContext"/> (the <c>EntityFramework</c>
/// NuGet package, "EF6") rather than <c>Microsoft.EntityFrameworkCore.DbContext</c>
/// - see <c>Xfty.EntityFrameworkCore</c>'s <c>EfPersistenceGateway</c> for that
/// one instead. A project still on EF6 has no path to EF Core's `DbContext`
/// at all, so this is a separate implementation, not a variant of that one.
/// Register with
/// <c>recordProvider.SetPersistenceGateway(new Ef6PersistenceGateway(dbContext))</c>.
///
/// The only real behavioural difference from the EF Core gateway: EF6's
/// <see cref="System.Data.Entity.DbContext"/> has no non-generic
/// <c>Add(object)</c> convenience overload the way EF Core added - EF6
/// requires going through the entity's own <see cref="DbSet"/> instead, found
/// by <see cref="System.Data.Entity.DbContext.Set(Type)"/> since the record's
/// static type isn't known here (only <see cref="object.GetType"/>, from
/// reflection). Everything else - the string-Id-fill-before-insert rule, one
/// <see cref="System.Data.Entity.DbContext.SaveChangesAsync()"/> call per
/// depth-batched layer - is identical.
/// </summary>
public sealed class Ef6PersistenceGateway(DbContext dbContext) : IPersistenceGateway
{
    public async Task Insert(List<object> records, PropertyInfo idField)
    {
        records.ForEach(record => this.AddOne(record, idField));
        _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private void AddOne(object record, PropertyInfo idField)
    {
        if (idField.PropertyType == typeof(string) && idField.GetValue(record) is null)
        {
            idField.SetValue(record, Guid.NewGuid().ToString());
        }

        _ = dbContext.Set(record.GetType()).Add(record);
    }
}