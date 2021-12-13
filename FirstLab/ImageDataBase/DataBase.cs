using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ImageDataBase
{
	public class Image
	{
		public int ImageId { get; set; }
		public string ImageName { get; set; }
		public string ImageHash { get; set; }
		public virtual ImageData ImagePhoto { get; set; }
		public virtual ICollection<ImageObject> ImageObjects { get; set; }
	}

	public class ImageData
	{
		public int ImageDataId { get; set; }
		public byte[] ImageDataArray { get; set; }
	}

	public class ImageObject
	{
		public int ImageObjectId { get; set; }
		public string ImageObjectName { get; set; }
		public float X1 { get; set; }
		public float Y1 { get; set; }
		public float X2 { get; set; }
		public float Y2 { get; set; }
	}

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
