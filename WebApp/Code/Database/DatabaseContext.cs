﻿using System;
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

        public string DbPath { get; private set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
            DbPath = FileIO.StorageDirectory + Path.DirectorySeparatorChar + "database.db";
        }

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

        public ExternalDatabaseContext()
        { }
    }

    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string PhoneNo { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public int Postcode { get; set; }
    }

    public class Signin
    {
        public int SigninID { get; set; }
        public int UserID { get; set; }
        public DateTime Time { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public int Postcode { get; set; }
    }
}
