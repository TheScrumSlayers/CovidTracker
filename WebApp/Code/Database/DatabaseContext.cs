using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using CovidTracker.Code.IO;
using Microsoft.EntityFrameworkCore;

namespace CovidTracker.Code.Database
{
    /// <summary>
    /// Database context for our local server.
    /// </summary>
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Signin> Signins { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Signin>().ToTable("Signin");
        }
    }

    /// <summary>
    /// Database context for the external server, the one owned by the SHO.
    /// Note that this is a mockup and no actual server->server communication is implemented.
    /// </summary>
    public class ExternalDatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Signin> Signins { get; set; }

        public ExternalDatabaseContext(DbContextOptions<ExternalDatabaseContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Signin>().ToTable("Signin");
        }
    }

    public class User : IEquatable<User>
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string PhoneNo { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as User);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserID);
        }

        public bool Equals(User other)
        {
            return other.UserID == UserID;
        }
    }

    public class Signin : IEquatable<Signin>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SigninID { get; set; }

        public int UserID { get; set; }
        public DateTime Time { get; set; }
        public string PhoneNo { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Signin);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SigninID);
        }

        public bool Equals(Signin other)
        {
            return other.SigninID == SigninID;
        }
    }
}
