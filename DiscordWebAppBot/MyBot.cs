using Discord;
using Discord.Commands;
using DiscordWebApp;
using DiscordWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DiscordWebAppBot
{
    class MyBot
    {
        DiscordClient discord;
        const string DiscordConnectionToken = "ENTER STRING HERE";

        public MyBot()
        {

            // initialize bot and set logging
            discord = new DiscordClient(x =>
            {
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = Log;
            });

            // commands
            discord.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;
            });

            // test command
            var commands = discord.GetService<CommandService>();
            commands.CreateCommand("test")
                .Parameter("GreetedPerson", ParameterType.Optional)
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($"test deez nuts {e.User.Name}");
                });

            // user info
            commands.CreateCommand("user info")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($" {e.User.Name} \n Joined the server at: {e.User.JoinedAt} \n Last Online at: {e.User.LastOnlineAt} \n Last Active at: {e.User.LastActivityAt}");
                });

            // list users
            commands.CreateCommand("users")
                .Do(async (e) =>
                {
                    foreach (var item in e.User.Server.Users.ToList())
                    {
                        await e.Channel.SendMessage($"Response: {item.Name}");
                    }

                });

            // save info to db
            // note: Last Activity and Last Online are tracked by bot, not pulled from discord 
            // so they will be null when seeding unless bot is kept online
            commands.CreateCommand("seed db")
                .Do(async (e) =>
                {
                    if (e.User.Id == 193488434051022848)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            Console.WriteLine("one");
                            var serverIdString = e.Server.Id.ToString();
                            var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();
                            Console.WriteLine("two");

                            //create server in db if it doesnt exist
                            if (currentServer == null)
                            {
                                Console.WriteLine("Server doesn't exist...Creating...");
                                var newServer = new DiscordWebApp.Models.Server()
                                {
                                    GuildId = serverIdString,
                                    Name = e.Server.Name,
                                    ServerOwner = e.Server.Owner.Name,
                                    Users = new List<DiscordWebApp.Models.User>()
                                };

                                _db.Servers.Add(newServer);
                                _db.SaveChanges();
                                Console.WriteLine("New server added to db");
                            }
                            else
                            {
                                Console.WriteLine("Found server...Adding users...");
                            }

                            // add users to server if they dont exist
                            int numUsers = 0;
                            foreach (var item in e.User.Server.Users.ToList())
                            {
                                numUsers++;
                                Console.WriteLine($"Adding user {item.Name}, {item.JoinedAt}");


                                var user = new DiscordWebApp.Models.User()
                                {
                                    GuildId = serverIdString,
                                    DateCreated = item.JoinedAt,
                                    LastOnline = item.LastOnlineAt,
                                    LastActive = item.LastActivityAt,
                                    Username = item.Name
                                };

                                currentServer.Users.Add(user);
                                Console.WriteLine("Done adding a user");
                            }
                            _db.SaveChanges();
                            Console.WriteLine($"Number of users: {numUsers}");
                            await e.Channel.SendMessage($"Done seeding database.");

                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"You do not have permission to run that command.");
                    }

                });

            // track when users joined
            discord.UserJoined += async (s, e) =>
            {
                var logChannel = e.Server.FindChannels("mods").FirstOrDefault();
                await logChannel.SendMessage($"User Joined: {e.User} at {DateTime.Now}");
            };

            // track when users leave
            discord.UserLeft += async (s, e) =>
            {
                var logChannel = e.Server.FindChannels("mods").FirstOrDefault();
                await logChannel.SendMessage($"User Left: {e.User} at {DateTime.Now}");
            };

            // connect
            // IMPORTANT: DO NOT PUSH TOKEN TO GITHUB
            discord.ExecuteAndWait(async () =>
            {
                await discord.Connect(DiscordConnectionToken, TokenType.Bot);
            });
        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
