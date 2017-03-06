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
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344 || e.User.Id == 176870246940934144)
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
                    // Having an issue with BULK deletes through LINQ

                    // Only Creator / admin accounts
                    if (e.User.Id == 193488434051022848 || e.User.Id == 270645134226489344)
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            //// get server info from calling user
                            //var serverIdString = e.Server.Id.ToString();
                            //var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();

                            //var users = currentServer.Users.Where(x => x.GuildId == serverIdString);
                            

                            //foreach (var user in users)
                            //{
                            //    _db.Users.Remove(user);
                            //}
                            //_db.SaveChanges();
                            

                            //userList.Delete(x => x.GuildId == serverIdString);

                            //foreach (var item in e.User.Server.Users.ToList())
                            //{
                            //    currentServer.Users.Remove(item);
                            //}

                            // TODO
                            await e.Channel.SendMessage($"Userlist for this server has been successfully cleared.");
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"You do not have permission to run that command.");
                    }
                });


            // if user has been active in past N days, assign "active" role
            // otherwise remove role
            commands.CreateCommand("update active users")
                // note to self: c, u, ch = command, user, channel
                // can also add a second argument to addcheck for error message to return
                // find roles returns iEnumerable so need to use .FirstOrDefault()
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("badmin").FirstOrDefault()))
                .Do(async (e) =>
                {
                    using (var _db = new DiscordWebAppDb())
                    {
                        // get server info from calling user
                        var serverIdString = e.Server.Id.ToString();
                        var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();


                        // find 'active' role
                        var activeRole = e.Server.FindRoles("active").FirstOrDefault();

                        if (activeRole != null)
                        {
                            int numActive = 0;
                            int numInactive = 0;
                            // add 'active' role to active users
                            // CAREFUL: e.Server.Users doesnt track users who left
                            // but we have to use it here instead of _db.Server so we have access to the AddRoles method
                            foreach (var user in e.Server.Users)
                            {
                                var userIdString = user.Id.ToString();

                                var currentUser =
                                    currentServer
                                        .Users
                                        .OrderByDescending(x => x.DateJoined) // in case of duplicate same user (after leaving/rejoining)
                                        .Where(x => (x.GuildId == serverIdString) && (x.UserId == userIdString))
                                        .FirstOrDefault();

                                try
                                {
                                    if (currentUser.LastActive >= DateTime.UtcNow.Date.AddDays(-7))
                                    {
                                        numActive++;
                                        // don't assign if already has role or if is owner 
                                        if (!user.HasRole(activeRole) && user.Id != 270645134226489344)
                                        {
                                            await user.AddRoles(activeRole);
                                        }
                                    }
                                    else
                                    {
                                        numInactive++;

                                        if (user.HasRole(activeRole))
                                        {
                                            await user.RemoveRoles(activeRole);
                                        }

                                    }
                                }
                                catch
                                {
                                    // Remember to run "update users" command before "update active users"
                                    // If users join when bot is down and db isn't updated, error will be thrown here
                                    Console.WriteLine($"Caught an error");
                                }
                                
                            }

                            await e.Channel.SendMessage($"Successfully updated active users.  Active users in the past 7 days: **{numActive}**.  Inactive users in the past 7 days: {numInactive}");
                        }
                        else
                        {
                            await e.Channel.SendMessage($"Error: Missing role.");
                        }

                        
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

                                var numLeftUsers = currentServer.Users.GroupBy(x => x.UserId).Select(x => x.First()).Where(x => x.DateLeft >= DateTime.UtcNow.Date.AddDays(negativeNumDays)).ToList().Count();

                                await e.Channel.SendMessage($"New users in the past {e.GetArg("days")} days: **{numNewUsers}**. \nUsers who left: **{numLeftUsers}**.  \nTotal net: **{numNewUsers - numLeftUsers}**");
                            }
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage($"Error: Please enter a valid number.");
                    }
                });

            commands.CreateCommand("update rich users")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("badmin").FirstOrDefault()))
                .Do(async (e) =>
                {
                    using (var _db = new DiscordWebAppDb())
                    {

                        // get server info from calling user
                        var serverIdString = e.Server.Id.ToString();
                        var currentServer = _db.Servers.Where(s => s.GuildId == serverIdString).SingleOrDefault();


                        // List of users with over 1500 points
                        List<string> userIdList = new List<string>
                        {
                            "222492622873165824", "270645134226489344", "193488434051022848", "146164272278274048", "127971327666683904",
                            "190284094188421120", "192746604745195521", "202915354974879745", "247204729539526657", "155473464759812097",
                            "95591712935649280", "103651750757543936", "270770110111678464", "197041135279931392", "203013697382121473",
                            "194500163123806209", "223230145144553473", "225088501572567042", "180321407207473152", "206925756620734467",
                            "80857820991332352", "201449547606654976", "164684870390513665", "135466956957548544", "90524647044423680",
                            "181623740206022657", "162063932515680256", "196267152162947072", "98790249253068800", "156478093605732353",
                            "95681793226715136", "245657974813622282", "181579327320752129", "196291500760367104", "230406807665770496",
                            "135869064370323456", "202242452331954176", "114041975480516612", "178823916129746944", "148829787069087744",
                            "186702252231229440", "144850470412746752", "193804772405542922", "117783631358984196", "211947650625437696",
                            "272789470720425985", "196152888328847360", "191549067950555136", "170615503377661952", "241504501821997057",
                            "147977933510672384", "232287300841766913", "178353074547458048", "149601692466282496", "270667121040556042",
                            "249837057457913856", "159749759240765441", "199987428642127872", "194613817911672832", "197866017660207104",
                            "136629317244420097", "260567666325061633", "165676206497202177", "223214301702389771", "193690154236379136",
                            "243687424809500672", "148961736358232075", "272111615544000513", "157579699525124096", "219631484632301569",
                            "261670184878735360", "217125225416884224", "272955218923225098", "263193967460352000", "140116955255406592",
                            "134760463811477504", "209438385134108672", "268840992021544970", "124079524680826880", "152875095042293760",
                            "223229123227549696", "251990780032450560", "166422869448982528", "224652590485340160", "131059058336727040",
                            "133080813716766720", "265858401450328065", "269945560922980352", "155044949367193600", "211687511544561664",
                            "196728370480939008", "272503662041890816", "275845816210554880", "158292919671980032", "159754762890379274",
                            "192873182275829760", "123627422154227712", "188053902342488064", "109153280399011840", "204068808737030144",
                            "148341562022035457", "256211257638518786", "201892513311621120", "261340206286897153", "249718528142344194",
                            "171911256163352576", "139006003118211073"
                        };

                        // find 'active' and 'rich' role
                        var activeRole = e.Server.FindRoles("active").FirstOrDefault();
                        var moneyRole = e.Server.FindRoles("rich").FirstOrDefault();

                        var numInList = 0;

                        if (activeRole != null && moneyRole != null)
                        {
                            foreach (var user in e.Server.Users)
                            {
                                var userIdString = user.Id.ToString();

                                var currentUser =
                                    currentServer
                                        .Users
                                        .OrderByDescending(x => x.DateJoined) // in case of duplicate same user (after leaving/rejoining)
                                        .Where(x => (x.GuildId == serverIdString) && (x.UserId == userIdString))
                                        .FirstOrDefault();

                                foreach (var userId in userIdList)
                                {
                                    if (userId == userIdString && user.HasRole(activeRole))
                                    {
                                        numInList++;

                                        if (!user.HasRole(moneyRole) && user.Id != 270645134226489344)
                                        {
                                            await user.AddRoles(moneyRole);
                                        }
                                    }
                                }
                            }

                            await e.Channel.SendMessage("Successfully added 'rich' role to active users with over 1500 points.");
                        }
                        else
                        {
                            await e.Channel.SendMessage("There was an error.");
                        }

                        
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
                await logChannel.SendMessage($"```Diff\n+ \" User has left the server - {DateTime.UtcNow} (UTC) \"\n{e.User}\n```");
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
