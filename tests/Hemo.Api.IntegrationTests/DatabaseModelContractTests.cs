using Hemo.Infrastructure.Organization;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class DatabaseModelContractTests
{
    private readonly IModel _model = BuildDesignTimeModel();

    private static IModel BuildDesignTimeModel()
    {
        var options = new DbContextOptionsBuilder<HemoDbContext>()
            .UseSqlServer("Server=(local);Database=Hemo;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new HemoDbContext(options);
        return db.GetService<IDesignTimeModel>().Model;
    }

    [Theory]
    [InlineData(typeof(AccountUser), "sec", "AccountUser")]
    [InlineData(typeof(HemoRole), "sec", "Role")]
    [InlineData(typeof(HemoPermission), "sec", "Permission")]
    [InlineData(typeof(UserRole), "sec", "UserRole")]
    [InlineData(typeof(RolePermission), "sec", "RolePermission")]
    [InlineData(typeof(Facility), "org", "Facility")]
    [InlineData(typeof(Department), "org", "Department")]
    [InlineData(typeof(Bed), "org", "Bed")]
    public void Entities_map_to_existing_SQL_schema(Type entity, string schema, string table)
    {
        var metadata = _model.FindEntityType(entity);
        Assert.NotNull(metadata);
        Assert.Equal(schema, metadata.GetSchema());
        Assert.Equal(table, metadata.GetTableName());
        Assert.True(metadata.IsTableExcludedFromMigrations());
    }

    [Theory]
    [InlineData(typeof(AccountUser), nameof(AccountUser.UserName), 100)]
    [InlineData(typeof(AccountUser), nameof(AccountUser.PasswordHash), 500)]
    [InlineData(typeof(Facility), nameof(Facility.Code), 30)]
    [InlineData(typeof(Department), nameof(Department.Name), 160)]
    [InlineData(typeof(Bed), nameof(Bed.BedCode), 30)]
    public void Entity_column_lengths_match_baseline(Type entity, string property, int length)
    {
        Assert.Equal(length, _model.FindEntityType(entity)!
            .FindProperty(property)!.GetMaxLength());
    }
}
