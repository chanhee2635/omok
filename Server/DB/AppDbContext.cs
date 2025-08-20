using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.DB
{
    public class AppDbContext : DbContext
    {
        public DbSet<AccountDb> Accounts { get; set; }
        public DbSet<PlayerDb> Players { get; set; }

        static readonly ILoggerFactory _logger = LoggerFactory.Create( builder => { builder.AddConsole(); });
        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options
                .UseLoggerFactory(_logger)
                .UseSqlServer(ConfigManager.Config.connectionString); 
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AccountDb>()
                .HasIndex(a => a.GoogleId)
                .IsUnique();

            builder.Entity<PlayerDb>()
                .HasIndex(p => p.Name)
                .IsUnique();
        }
    }
}
