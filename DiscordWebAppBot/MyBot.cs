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
        // IMPORTANT: DO NOT PUSH TOKEN TO GITHUB
        const string DiscordConnectionToken = "token goes here";
        const string logChannelName = "logs";

        public MyBot()
        {

            // initialize bot and set logging
            discord = new DiscordClient(x =>
            {
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = Log;
                x.MessageCacheSize = 25;  // Caching is necessary to retrieve deleted message info
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
                    await e.Channel.SendMessage($"this is a test for {e.User.Name}");
                });

            // user info
            commands.CreateCommand("user info")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($"```Markdown\n# \" User info for: {e.User} \"\n+ Joined server: {e.User.JoinedAt}\n+ Id: {e.User.Id}\n+ Last online: {e.User.LastOnlineAt}\n+ Last Active: {e.User.LastActivityAt}\n```");
                });


            // save info to db
            // note: Last Activity and Last Online are tracked by bot, not pulled from discord 
            // so they will be null when seeding unless bot is kept online
            commands.CreateCommand("seed db")
                .Do(async (e) =>
                {
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            var serverIdString = e.Server.Id.ToString();
                            var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();

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
                                // NOTE: THIS WILL CAUSE SOME DESCREPENCIES
                                // WE ARE USING .JOINEDAT HERE BUT WE USED DATETIME.NOW NORMALLY
                                numUsers++;
                                Console.WriteLine($"Adding user {item.Name}, {item.JoinedAt}");


                                var user = new DiscordWebApp.Models.User()
                                {
                                    GuildId = serverIdString,
                                    DateJoined = item.JoinedAt,
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

            discord.MessageDeleted += async (s, e) =>
            {
                var logChannel = e.Server.FindChannels(logChannelName).FirstOrDefault();

                // don't need to log deleted messages from Nadeko/Buster bot (buster deletes a lot of game/trivia/currency type events)
                if (e.Message != null && e.User != null && e.User.Id != 196844256894124042)
                {
                    var message = e.Message.Text;
                    var messageUser = e.Message.User;
                    //var deletingUser = e.User.Name; not returning the person i want

                    await logChannel.SendMessage($"```Markdown\n# \" Message deleted \"\n{e.Message.User} - \"{e.Message.Text}\"\n```");
                }
                else
                    await logChannel.SendMessage($"```Markdown\n# \" Message deleted \"\nCould not retrieve deleted info (message not in cache)\n```");
            };

            // track when users joined
            // NOTE: Datetime.now is different from JoinedAt date (timezones)
            // USE ONE OR THE OTHER
            // FOR: JOINED, LEFT, MESSAGE DELETED
            discord.UserJoined += async (s, e) =>
            {
                using (var _db = new DiscordWebAppDb())
                {   
                    // get server info
                    var serverIdString = e.Server.Id.ToString();
                    var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                    // create new user
                    var newUser = new DiscordWebApp.Models.User()
                    {
                        UserId = e.User.Id.ToString(),
                        Username = e.User.ToString(),
                        GuildId = serverIdString,
                        DateJoined = DateTime.Now,

                    };

                    // add user to server
                    currentServer.Users.Add(newUser);

                    // save to db
                    _db.SaveChanges();
                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logChannelName).FirstOrDefault();
                // Perl is just for colored markdown
                await logChannel.SendMessage($"```Perl\n\" User has joined the server - {DateTime.Now} \"\n{e.User}\n```");
            };

            // track when users leave
            discord.UserLeft += async (s, e) =>
            {
                using (var _db = new DiscordWebAppDb())
                {
                    // get server info
                    var serverIdString = e.Server.Id.ToString();
                    var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                    // get user
                    // select most recent user match from db since same user might leave and come back
                    var user = _db.Users.Where(u => u.GuildId == serverIdString).OrderByDescending(x => x.DateJoined).FirstOrDefault();

                    // update user leave info
                    // ban event happens first, and then leave event (?)
                    if (user.LeaveType != "BANNED")
                    {
                        user.LeaveType = "LEFT";
                        user.DateLeft = DateTime.Now;
                    }
                    
                    
                    // save to db
                    _db.SaveChanges();
                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logChannelName).FirstOrDefault();
                await logChannel.SendMessage($"```Diff\n+ \" User has left the server - {DateTime.Now} \"\n{e.User}\n```");
            };

            // track kicked users
            discord.UserBanned += async (s, e) =>
            {
                using (var _db = new DiscordWebAppDb())
                {
                    // get server info
                    var serverIdString = e.Server.Id.ToString();
                    var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                    // get user
                    // select most recent user match from db since same user might leave and come back
                    var user = _db.Users.Where(u => u.GuildId == serverIdString).OrderByDescending(x => x.DateJoined).FirstOrDefault();

                    // update user leave info
                    user.LeaveType = "BANNED";
                    user.DateLeft = DateTime.Now;

                    // save to db
                    _db.SaveChanges();
                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logChannelName).FirstOrDefault();
                await logChannel.SendMessage($"```Diff\n+ \" User has been BANNED from the server - {DateTime.Now} \"\n{e.User}\n```");
            };

            // connect
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
