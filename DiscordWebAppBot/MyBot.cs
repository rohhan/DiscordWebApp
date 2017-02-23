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
        const string logJoinedAndLeftChannel = "user_logs";

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


            //commands.CreateCommand("pls") - FAILED: NADEKO BOT DOES NOT ACCEPT COMMANDS FROM OTHER BOTS (e.g. this bot)
            //    .Do(async (e) =>
            //    {
            //        //await e.Channel.SendMessage($"{{@{e.User.Id}}}");
            //        await e.Channel.SendMessage($"$$$ {e.User.NicknameMention}");
            //    });


            // add server to db
            commands.CreateCommand("add server")
                .Do(async (e) =>
                {
                    // Only Creator / admin accounts
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            // get server info from calling user
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

                                await e.Channel.SendMessage($"Successfully added server to database.");
                            }
                            else
                            {
                                await e.Channel.SendMessage($"Server already exists in the database.");
                            }
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"You do not have permission to run that command.");
                    }
                });
            
            // note: Last Activity and Last Online are tracked by bot, not pulled from discord 
            // so they will be null when seeding unless bot is kept online
            commands.CreateCommand("update users")
                .Do(async (e) =>
                {
                    // Only Creator / admin accounts
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            // get server info from calling user
                            var serverIdString = e.Server.Id.ToString();
                            var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();

                            //check to make sure server exists in db
                            if (currentServer == null)
                            {
                                Console.WriteLine("Server does not exist in the database.  Please run 'add server' command.");
                                await e.Channel.SendMessage($"Server does not exist in the database.  Please run 'add server' command.");
                            }
                            else
                            {
                                Console.WriteLine("Found server...Adding users...");

                                // add users to server if they dont exist
                                int numTotalUsers = 0;
                                int numNewUsers = 0;
                                foreach (var item in e.User.Server.Users.ToList())
                                {
                                    numTotalUsers++;

                                    var userIdString = item.Id.ToString();
                                    var existingUser = _db.Users.Where(x => (x.GuildId == serverIdString) && (x.UserId == userIdString)).FirstOrDefault();

                                    if (existingUser != null)
                                    {
                                        Console.WriteLine($"User already exists: {item.Name}");
                                    }
                                    else
                                    {
                                        numNewUsers++;

                                        Console.WriteLine($"Adding user {item.Name}, {item.JoinedAt}");

                                        var newUser = new DiscordWebApp.Models.User()
                                        {
                                            GuildId = serverIdString,
                                            DateJoined = item.JoinedAt,
                                            UserId = userIdString,
                                            Username = item.Name
                                        };

                                        currentServer.Users.Add(newUser);
                                        Console.WriteLine("Done adding a user");
                                    }
                                }
                                _db.SaveChanges();
                                Console.WriteLine($"Total numbers of users: {numTotalUsers}");
                                Console.WriteLine($"Number of new users: {numNewUsers}");
                                await e.Channel.SendMessage($"Done updating users.  New users added: {numNewUsers}.  Total users: {numTotalUsers}");
                            }
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"You do not have permission to run that command.");
                    }

                });

            commands.CreateCommand("purge userlist")
                .Do(async (e) =>
                {
                    // Only Creator / admin accounts
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            // TODO
                            await e.Channel.SendMessage($"Userlist for this server has been successfully cleared.");
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"You do not have permission to run that command.");
                    }
                });

            // display total user count
            commands.CreateCommand("usercount")
                .Do(async (e) =>
                {
                    using (var _db = new DiscordWebAppDb())
                    {
                        var numUsers = e.User.Server.Users.ToList().Count();

                        await e.Channel.SendMessage($"Total user count: **{numUsers}** ");
                    }
                });

            // display new user count
            commands.CreateCommand("newusers")
                .Parameter("days", ParameterType.Required)
                .Do(async (e) =>
                {
                    // make sure correct parameters are passed in
                    int numDays;
                    bool isValid = int.TryParse(e.GetArg("days"), out numDays);

                    if (isValid)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            // get server info from calling user
                            var serverIdString = e.Server.Id.ToString();
                            var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();

                            // negative because we will subtract (want data for past N days).
                            var negativeNumDays = numDays * -1;

                            if (currentServer != null)
                            {
                                // make sure to check for duplicates (people leaving/rejoining)
                                // also might want to check for just who left vs who stayed
                                var numNewUsers = currentServer.Users.GroupBy(x => x.UserId).Select(x => x.First()).Where(x => x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays)).ToList().Count();

                                await e.Channel.SendMessage($"New users in the past {e.GetArg("days")} days: **{numNewUsers}** ");
                            }
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"Error: Please enter a valid number.");
                    }
                });

            discord.MessageDeleted += async (s, e) =>
            {
                var logChannel = e.Server.FindChannels(logChannelName).FirstOrDefault();

                if (e.Message != null && e.User != null)
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
                        GuildId = serverIdString,
                        DateJoined = e.User.JoinedAt,
                        UserId = e.User.Id.ToString(),
                        Username = e.User.ToString()
                    };

                    // add user to server
                    currentServer.Users.Add(newUser);

                    // save to db
                    _db.SaveChanges();
                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logJoinedAndLeftChannel).FirstOrDefault();
                // Perl is just for colored markdown
                await logChannel.SendMessage($"```Perl\n\" User has joined the server - {e.User.JoinedAt} (UTC) \"\n{e.User}\n```");
            };

            // track when users leave
            discord.UserLeft += async (s, e) =>
            {
                using (var _db = new DiscordWebAppDb())
                {
                    // get server and user info
                    var serverIdString = e.Server.Id.ToString();
                    var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                    var userId = e.User.Id.ToString();

                    // select user
                    // select most recent user match from db since same user might leave and come back
                    var user = _db.Users.Where(u => (u.GuildId == serverIdString) && (u.UserId == userId)).OrderByDescending(x => x.DateJoined).FirstOrDefault();

                    if (user == null) // in case  bot was down or didn't properly create user
                    {
                        // create user
                    }
                    else
                    {
                        // update user leave info
                        // ban event happens first, and then leave event (?)
                        if (user.LeaveType != "BANNED")
                        {
                            user.LeaveType = "LEFT";
                            user.DateLeft = DateTime.UtcNow;
                        }

                        // save to db
                        _db.SaveChanges();
                    }

                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logJoinedAndLeftChannel).FirstOrDefault();
                await logChannel.SendMessage($"```Diff\n+ \" User has left the server - {e.User.JoinedAt} (UTC) \"\n{e.User}\n```");
            };

            // track kicked users
            discord.UserBanned += async (s, e) =>
            {
                using (var _db = new DiscordWebAppDb())
                {
                    // get server and user info
                    var serverIdString = e.Server.Id.ToString();
                    var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                    var userId = e.User.Id.ToString();

                    // select user
                    // select most recent user match from db since same user might leave and come back
                    var user = _db.Users.Where(u => (u.GuildId == serverIdString) && (u.UserId == userId)).OrderByDescending(x => x.DateJoined).FirstOrDefault();

                    // update user leave info
                    user.LeaveType = "BANNED";
                    user.DateLeft = e.User.LastOnlineAt;

                    // save to db
                    _db.SaveChanges();
                }

                // output note to mod channel
                var logChannel = e.Server.FindChannels(logJoinedAndLeftChannel).FirstOrDefault();
                await logChannel.SendMessage($"```Diff\n+ \" User has been BANNED from the server - {e.User.LastOnlineAt} (UTC) \"\n{e.User}\n```");
            };

            // track user last active
            discord.MessageReceived += async (s, e) =>
            {
                // Check to make sure that the bot is not the author
                if (!e.Message.IsAuthor && e.User.Id != 196844256894124042)
                {
                    using (var _db = new DiscordWebAppDb())
                    {
                        // get server and user info
                        var serverIdString = e.Server.Id.ToString();
                        var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                        var userId = e.User.Id.ToString();


                        // select user
                        // select most recent user match from db since same user might leave and come back
                        var user = _db.Users.Where(u => (u.GuildId == serverIdString) && (u.UserId == userId)).OrderByDescending(x => x.DateJoined).FirstOrDefault();
                        if (user != null)
                        {
                            user.LastActive = DateTime.UtcNow;
                            _db.SaveChanges();
                        }
                    }

                    // is this correct usage?  i want an async method that awaits nothing
                    await Task.FromResult(true);
                }
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
