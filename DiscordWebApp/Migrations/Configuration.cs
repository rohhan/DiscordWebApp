namespace DiscordWebApp.Migrations
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<DiscordWebApp.Models.DiscordWebAppDb>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "DiscordWebApp.Models.DiscordWebAppDb";
        }

        protected override void Seed(DiscordWebApp.Models.DiscordWebAppDb context)
        {
            //  This method will be called after migrating to the latest version.

            context.Servers.AddOrUpdate(
                    s => s.Name,
                    new Server {
                        Name = "Socialites",
                        GuildId = "",
                        ServerOwner = ""
                    },
                    new Server
                    {
                        Name = "NotSoc",
                        GuildId = "",
                        ServerOwner = "",
                        Users = new List<User> {
                            new User { Username = "Sushi", DateCreated = DateTime.Now, GuildId= "", NumberOfFlowers = 42},
                        }
                    }
                );
            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
