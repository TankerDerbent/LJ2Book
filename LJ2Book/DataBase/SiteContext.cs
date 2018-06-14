using System;
using System.Data.Entity;
using SQLite.CodeFirst;

namespace LJ2Book.DataBase
{
	public class SiteContext : DbContext
	{
		public SiteContext() : base("DefaultConnection")
		{
			Config();
		}
		public SiteContext(string nameOrConnectionString) : base(nameOrConnectionString)
		{
			Config();
		}
		protected void Config()
		{
			Configuration.ProxyCreationEnabled = true;
			Configuration.LazyLoadingEnabled = true;
		}
		public DbSet<Param> Params { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Blog> Blogs { get; set; }
		public DbSet<Article> Articles { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			//ModelConfiguration.Configure(modelBuilder);
			var initializer = new SiteDbInitializer(modelBuilder);
			Database.SetInitializer(initializer);
		}
	}
	public class SiteDbInitializer : SqliteDropCreateDatabaseWhenModelChanges<SiteContext>
	{
		public SiteDbInitializer(DbModelBuilder modelBuilder) : base(modelBuilder, typeof(CustomHistory))
		{
			//
		}
		protected override void Seed(SiteContext context)
		{
			//base.Seed(context);
		}
	}

	public class CustomHistory : IHistory
	{
		public int Id { get; set; }
		public string Hash { get; set; }
		public string Context { get; set; }
		public DateTime CreateDate { get; set; }
	}
}
