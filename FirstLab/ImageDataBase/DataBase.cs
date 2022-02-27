using Microsoft.EntityFrameworkCore;
using ImageRecognitionContract;

namespace ImageDataBase
{
	public class ImageDB : DbContext
	{
		public DbSet<Image> Images { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseLazyLoadingProxies().UseSqlite(@"Data Source=C:\Users\torre\Desktop\library.db");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Image>().HasMany(e => e.ImageObjects).WithOne().OnDelete(DeleteBehavior.Cascade);
		}
	}
}
