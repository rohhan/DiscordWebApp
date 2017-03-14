using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class DiscordWebAppDb : DbContext
    {
        public DiscordWebAppDb() : base("name=DefaultConnection")
        {

        }
        public DbSet<Server> Servers { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<MovieNight> MovieNights { get; set; }
        public DbSet<Movie> Movies { get; set; }
    }
}