using ApplicationCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ApplicationCore.DataAccess;
public class DefaultContext : DbContext
{
  
   public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
	{
      
   }
   protected override void OnModelCreating(ModelBuilder builder)
   {
      base.OnModelCreating(builder);
      builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

      if (Database.IsNpgsql())
      {
         var types = builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));
         foreach (var property in types)
         {
            property.SetColumnType("timestamp without time zone");
         }
      }
   }


   public DbSet<SourceModel> SourceModels => Set<SourceModel>();
   public DbSet<DocModel> DocModels => Set<DocModel>();
   public DbSet<UnitPerson> UnitPersons => Set<UnitPerson>();

   public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();

}
