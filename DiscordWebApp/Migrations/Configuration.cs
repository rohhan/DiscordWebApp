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
                        ServerOwner = "",
                        Users = new List<User> {
                            new User { Username = "Sosh", DateCreated = DateTime.Now, NumberOfFlowers = 50},
                            new User { Username = "Silverhammer", DateCreated = DateTime.Now, NumberOfFlowers = 25}
                        }
                    },
                    new Server
                    {
                        Name = "NotSoc",
                        ServerOwner = "",
                        Users = new List<User> {
                            new User { Username = "Sushi", DateCreated = DateTime.Now, NumberOfFlowers = 42},
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
