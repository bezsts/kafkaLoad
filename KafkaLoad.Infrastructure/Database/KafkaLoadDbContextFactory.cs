using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KafkaLoad.Infrastructure.Database;

// Used only by the EF Core CLI tooling (dotnet ef migrations add / update).
// Not used at runtime — the actual DbContext is created in App.axaml.cs.
public class KafkaLoadDbContextFactory : IDesignTimeDbContextFactory<KafkaLoadDbContext>
{
    public KafkaLoadDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KafkaLoadDbContext>()
            .UseSqlite("Data Source=kafkaload_design.db")
            .Options;

        return new KafkaLoadDbContext(options);
    }
}
