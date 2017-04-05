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

                                // check to make sure all users in remote server are also in db
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

                                // check to see if any users in db are missing from remote server (if so, add leave date)
                                var numUpdatedUsers = 0;
                                foreach (var item in currentServer.Users.ToList())
                                {
                                    if (item.DateLeft == null)
                                    {
                                        var match = false;
                                        foreach (var serverUser in e.User.Server.Users.ToList())
                                        {
                                            var userIdString = serverUser.Id.ToString();
                                            if (userIdString == item.UserId)
                                            {
                                                match = true;
                                            }
                                        }

                                        if (match == false)
                                        {
                                            numUpdatedUsers++;
                                            item.LeaveType = "UNKNOWN";
                                            item.DateLeft = DateTime.UtcNow;
                                        }
                                    }
                                }

                                _db.SaveChanges();
                                Console.WriteLine($"Total numbers of users: {numTotalUsers}");
                                Console.WriteLine($"Number of new users: {numNewUsers}");
                                await e.Channel.SendMessage($"Done updating users.  \nNew users added to DB: **{numNewUsers}**.  \nUsers (who left) udpated in DB : **{numUpdatedUsers}**  \nTotal users: **{numTotalUsers}**");
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
                                    if (currentUser != null)
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
                                    else
                                    {
                                        var newUser = new DiscordWebApp.Models.User()
                                        {
                                            GuildId = serverIdString,
                                            DateJoined = user.JoinedAt,
                                            UserId = user.Id.ToString(),
                                            Username = user.ToString()
                                        };
                                        Console.WriteLine($"Created new user - {user}");
                                    }
                                    
                                }
                                catch
                                {
                                    // Remember to run "update users" command before "update active users"
                                    // If users join when bot is down and db isn't updated, error will be thrown here
                                    Console.WriteLine($"Caught an error");
                                }

                            }

                            await e.Channel.SendMessage($"Successfully updated active users.  Active users in the past 7 days: **{numActive}**.  Inactive users in the past 7 days: **{numInactive}**");
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
            commands.CreateCommand("users overview")
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

                                // New users:
                                var numNewUsers =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId) // because one user might have multiple rows (leave/rejoin)
                                        .Select(x => x.First())
                                        .Where(x => x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays))
                                        .ToList()
                                        .Count();

                                var numNewUsersWhoLeft =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.DateLeft >= DateTime.UtcNow.Date.AddDays(negativeNumDays)))
                                        .ToList()
                                        .Count();

                                var numNewUsersWhoStayed = numNewUsers - numNewUsersWhoLeft;
                                var percentRetention = (int)Math.Round((double)(100 * numNewUsersWhoStayed) / numNewUsers);

                                // active new users:
                                var numNewUsersActive =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.LastActive >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.DateLeft == null))
                                        .ToList()
                                        .Count();

                                var percentNewUsersActive = (int)Math.Round((double)(100 * numNewUsersActive) / numNewUsersWhoStayed);

                                var numNewUsersWhoTalkedThenLeft =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.LastActive >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.DateLeft != null))
                                        .ToList()
                                        .Count();

                                var percentTalkedAndLeft = (int)Math.Round((double)(100 * numNewUsersWhoTalkedThenLeft) / numNewUsersWhoLeft);

                                // Existing users:
                                var numTotalUsersWhoLeft =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => x.DateLeft >= DateTime.UtcNow.Date.AddDays(negativeNumDays))
                                        .ToList()
                                        .Count();

                                // get everyone currently in the server
                                var numTotalUsers =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateLeft == null))
                                        .ToList()
                                        .Count();

                                var numTotalOldUsers =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateJoined < DateTime.UtcNow.Date.AddDays(negativeNumDays)))
                                        .ToList()
                                        .Count();

                                var numOldUsersWhoLeft =
                                    currentServer
                                        .Users
                                        .GroupBy(x => x.UserId)
                                        .Select(x => x.First())
                                        .Where(x => (x.DateJoined < DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.DateLeft >= DateTime.UtcNow.Date.AddDays(negativeNumDays)))
                                        .ToList()
                                        .Count();

                                var totes = e.Server.Users.ToList().Count();

                                Console.WriteLine(numTotalUsers);
                                Console.WriteLine(totes);

                                // JOIN DATA

                                // LEAVE DATA

                                // RETENTION

                                //var numOldUsersWhoLeft = numTotalUsersWhoLeft - numNewUsersWhoLeft;

                                var oldie = totes - numNewUsers;
                                var percentDecay = (int)Math.Round((double)(100 * numOldUsersWhoLeft) / numTotalOldUsers);// how many older users are leaving (don't include new users when calculating percent)

                                var channelMessage =
                                    "__**New Users:**__  \n" +
                                    $"Total new users joined in the past {e.GetArg("days")} days: **{numNewUsers}** \n" +
                                    $"New user retention: **{percentRetention}%** ({numNewUsersWhoStayed} / {numNewUsers})  \n" +
                                    $"Out of all new users who left, how many talked: **{percentTalkedAndLeft}%** ({numNewUsersWhoTalkedThenLeft} / {numNewUsersWhoLeft}) \n" +
                                    $"Out of all the new users who stayed, how many talk: **{percentNewUsersActive}%**  ({numNewUsersActive} / {numNewUsersWhoStayed})\n\n" +
                                    //$"Total net new users: **{numNewUsers - numTotalUsersWhoLeft}**  \n\n" +

                                    $"__**Existing Users:**__\n" +
                                    "(*the math/code is slightly off*)  \n" +
                                    $"Total current users: **{numTotalUsers}**\n" +
                                    $"Total users before {e.GetArg("days")} days ago: **{numTotalOldUsers}** \n" +
                                    $"Number of old users who left: **{numOldUsersWhoLeft}**  \n" +
                                    //$"Total number of users who left in the past {e.GetArg("days")} days: **{numTotalUsersWhoLeft}**  \n" +
                                    $"Percent decay (percent of existing users that left): **{percentDecay}%**";

                                await e.Channel.SendMessage(channelMessage);
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

            // help commands:
            commands.CreateCommand("help new user")
                .Do(async (e) =>
                {
                    string message =
                        "__**New User Guide:**__ \n\n" +
                        "*Welcome to the Internet Addicts Server.  Please read the #info_and_announcements channel to get yourself up to date with the rules, events, and other server happenings.* \n\n" +
                        " __**Basic Info:**__\n\n" +
                        " **Text and Voice Channels:**\n" +
                        "• There are different text channels for different purposes (main chat, memes, music, fitness, etc.)  Please use the appropriate channels :)\n" +
                        "• Same thing goes for voice channels on the left.  *Protip: If you want a quieter chatting experience, check out one of the smaller size-limited voice channels!*\n\n" +
                        " **Video Games:**\n" +
                        "• If you play video games, be sure to set up your game roles so that you can easily find people to game with.  For more details, type **!help games**\n\n" +
                        " **Events:**\n" +
                        "• We host weekly server events.  For more information, type **!help events** .  There are also in house gaming tournaments so keep an eye out for the next one!\n\n" +
                        " **Secret Channels:**\n" +
                        "• There are a few special channels that you will need to manually unlock access to: \n" +
                        "        => Type **.iam nsfw** to unlock the nsfw channel.  \n" +
                        "        => Type **.iam book club** to unlock the book club.\n" +
                        "        => We have a private channel for venting/ranting/advice.  If you would like access to the **diary** channel, please contact a moderator or admin.\n\n" +

                        " **Experience, levels, and currency:**\n" +
                        "• You can earn *server experience*  while typing in chat.  Experience will level you up and you will unlock special roles at certain levels.  For example, level 3 unlocks the ability to embed images. \n" +
                        "• You can earn *server currency* while typing in chat.  You can use this currency to gamble, buy other people, top the leaderboards, and win rewards at the end of the season.  For more information, type **!help currency** or **!help gambling**\n\n" +
                        "If you have any other questions, feel free to contact a mod or admin.";

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("help events")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("__**Event Information:**__  \n\n*tl;dr: There are weekly server events to watch movies, party, and play games together*  \n\n1) **Movie Nights** - A different movie every Friday night, voted on by the members.  \n2) **Drunk On Cam Nights** - Every Saturday on Rabbit.  \n3) **TableTop Simulator** - Come play virtual board games with your virtual friends every Sunday.");
                });

            commands.CreateCommand("help games")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($"__**Game Role Commands:**__\n\n*tl;dr: Use the commands below to search through the list of games, add/remove games to yourself, and then find other users to play games with.*\n\n**.lsar** - See the list of all game roles available on the server.\n**.iam [game name]** - Add a game role to yourself.  Example: .iam Overwatch  \n**.iamnot [game name]** - Remove a game role from yourself.  \n**@[game name]** - You can ping game roles to notify players when you are looking for a group.  Example: 'Does anybody want to play @Overwatch ?'  \n\n*See a game missing from the list?  If you want a new game added to the .lsar list, contact a moderator or admin!*");
                });

            commands.CreateCommand("help currency")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($"__**ServerCurrency Information:**__  \n\n*tl;dr: Earn server currency by participating on the server, use it to gamble, buy waifus, and win rewards.*  \n\nThere are three ways to earn server currency - aka ***Very Important Points***  \n\n1) **Being active** - All active chatters receive some points every day.  \n2) **Winning bot games** - You can win points by winning at Trivia (no Hangman rewards yet sorry) or by joining a bot games race when it happens.  \n3) **Gambling** - You can gamble the points you already have to try to win more (Type '**!help gambling**' for more information on gambling)");
                });

            commands.CreateCommand("help gambling")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage($"__**Gambling Information:**__  \n\n*tl;dr: You can gamble your points to earn more by one of the three ways listed below*  \n\n1) **bf [heads/tails] [amount]** - Guess the correct coin flip.  Example: $bf 10 tails \n2) **$br [amount]** - Roll the dice.  Example: $br 10  \n3) **$slot [amount]** - Play the slot machine.  Example: $slot 5  \n\n");
                });

            // movie night stuff:
            commands.CreateCommand("create movie night")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("badmin").FirstOrDefault()))
                .Do(async (e) =>
                {
                    string message = "";

                    using (var _db = new DiscordWebAppDb())
                    {
                        Console.WriteLine("Creating new Movie Night entity...");

                        var movieNight = new DiscordWebApp.Models.MovieNight()
                        {
                            MovieNightName = "Default",
                            DateCreated = DateTime.UtcNow,
                            Movies = new List<DiscordWebApp.Models.Movie>()
                        };

                        _db.MovieNights.Add(movieNight);
                        _db.SaveChanges();

                        Console.WriteLine("Successfully created new Movie Night!");
                        message = "Successfully created new Movie Night!";
                    }

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("movie add")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("event host").FirstOrDefault()))
                .Parameter("movieName", ParameterType.Required)
                .Do(async (e) =>
                {
                    var movieName = e.GetArg("movieName");
                    var message = "";

                    using (var _db = new DiscordWebAppDb())
                    {
                        // get movie night object - harcoded for now, just one movienight object
                        var movieNight = _db.MovieNights.Where(x => x.MovieNightName == "Default").FirstOrDefault();

                        if (movieNight != null)
                        {
                            var newMovie = new DiscordWebApp.Models.Movie()
                            {
                                Name = movieName
                            };
                            movieNight.Movies.Add(newMovie);
                            _db.SaveChanges();
                            message = $"Successfully added movie to list - {movieName}";
                        }
                    }

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("movie remove")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("event host").FirstOrDefault()))
                .Parameter("movieName", ParameterType.Required)
                .Do(async (e) =>
                {
                    var movieName = e.GetArg("movieName");
                    var message = "";

                    using (var _db = new DiscordWebAppDb())
                    {
                        // get movie night object - harcoded for now, just one movienight object
                        var movieNight = _db.MovieNights.Where(x => x.MovieNightName == "Default").FirstOrDefault();

                        if (movieNight != null)
                        {
                            var movie = movieNight.Movies.Where(x => x.Name == movieName).FirstOrDefault();
                            if (movie != null)
                            {
                                //movieNight.Movies.Remove(movie); - this only removes the reference, we want to remove the enter row?
                                _db.Movies.Remove(movie);
                                _db.SaveChanges();
                                message = $"Successfully removed movie from list - {movieName}";
                            }
                            else
                            {
                                message = $"Error: {movieName} is not on the list.  Please enter a valid movie name.";
                            }
                        }
                    }

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("movie clear")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("event host").FirstOrDefault()))
                .Do(async (e) =>
                {
                    var message = "";
                    using (var _db = new DiscordWebAppDb())
                    {
                        var movieCount = _db.Movies.Count();

                        foreach (var movie in _db.Movies)
                        {
                            _db.Movies.Remove(movie);
                        }
                        _db.SaveChanges();
                        message = $"Successfully removed all movies ({movieCount}) from list.";
                    }

                    await e.Channel.SendMessage(message);
                });



            commands.CreateCommand("movie time")
                .AddCheck((c, u, ch) => u.HasRole(ch.Server.FindRoles("event host").FirstOrDefault()))
                .Parameter("movieTime", ParameterType.Required)
                .Do(async (e) =>
                {
                    var movieTime = e.GetArg("movieTime");
                    var message = "";

                    using (var _db = new DiscordWebAppDb())
                    {
                        // get movie night object - harcoded for now, just one movienight object
                        var movieNight = _db.MovieNights.Where(x => x.MovieNightName == "Default").FirstOrDefault();

                        if (movieNight != null)
                        {
                            movieNight.DateAndTime = movieTime;
                            _db.SaveChanges();
                            message = $"Successfully updated movie time! - {movieTime}";
                        }
                    }

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("movie info")
                .Do(async (e) =>
                {
                    var message = "";

                    using (var _db = new DiscordWebAppDb())
                    {
                        string movieList = "";

                        var movieNight = _db.MovieNights.Where(x => x.MovieNightName == "Default").FirstOrDefault();

                        foreach (var movie in movieNight.Movies)
                        {
                            movieList += $"► {movie.Name}  \n";
                        }

                        var movieTime = String.IsNullOrEmpty(movieNight.DateAndTime) ? "TBA" : movieNight.DateAndTime;
                        movieList = movieNight.Movies.Count > 0 ? movieList : "TBA \n";

                        message =
                            $"__**Time:**__  \n{movieTime}  \n\n" +
                            $"__**List of Movie Options** ({movieNight.Movies.Count()})__  \n" + movieList + "\n" +
                            $"*Voting takes place 30 minutes before the movie start time.*";

                    }

                    await e.Channel.SendMessage(message);
                });

            // anonymous commands
            commands.CreateCommand("help anon")
                .Do(async (e) =>
                {
                    var message = "";
                    message = "__**Anonymous Posting Guide:**__\n\n" +
                    "• Type `!anon diary \"message\"` inside a direct message to Alfred Bot.\n" +
                    "• Type `!anon shitpost \"message\"` inside a direct message to Alfred Bot.\n" +
                    "• Type `!anon \"message\"` inside a channel to post anonymously (LESS SECURE METHOD).\n\n" +
                    "__**You must post your message in quotations!**__\n\n" +
                    "    => !anon \"It's a secret!\" - **Good** \n" +
                    "    => !anon It's a secret! - **Bad** \n\n" +
                    "• Your message will be posted with an anonymous ID\n" +
                    "• You can respond to messages with the same ID for on hour.  After that, your ID will be reset.\n" +
                    "• If you want to manually reset your ID before one hour is up, type **!anon reset**\n\n" +
                    "***NOTE**: Due to lag and some android glitches, the only way to guarantee your message will be anonymous is to direct message through the bot.*";

                    await e.Channel.SendMessage(message);
                });

            commands.CreateCommand("lookup")
                .Parameter("anonId", ParameterType.Required)
                .Do(async (e) =>
                {
                    // TO DO: probably shouldnt hardcore channel names
                    if (e.Channel.Name == "mods" || e.Channel.Name == "batcave")
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            var message = "";
                            int anonId;

                            bool isValid = int.TryParse(e.GetArg("anonId"), out anonId);

                            if (isValid)
                            {
                                // get server and user info
                                var serverIdString = e.Server.Id.ToString();
                                var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                                // get user
                                var user =
                                _db.Users
                                    .Where(u => (u.GuildId == serverIdString) && (u.RandomId == anonId))
                                    .OrderByDescending(x => x.DateJoined)
                                    .FirstOrDefault();

                                if (user != null)
                                {
                                    var username = user.Username;
                                    message = $"User: {username}";
                                }

                            }
                            else
                            {
                                message = "Please enter a valid ID";
                            }

                            await e.Channel.SendMessage(message);
                        }
                    }
                    
                });

            commands.CreateCommand("anon")
                .Parameter("message", ParameterType.Required)
                .Do(async (e) =>
                {
                    using (var _db = new DiscordWebAppDb())
                    {
                        if (e.Server != null)
                        {
                            if (e.Channel.Name == "dear_diary" || e.Channel.Name == "gen_chat_shitposting" || e.Channel.Name == "batcave")
                            {
                                // get server and user info
                                var serverIdString = e.Server.Id.ToString();
                                var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                                var userId = e.User.Id.ToString();

                                // get message
                                var message = e.GetArg("message");
                                if (message.Contains("@everyone"))
                                {
                                    message = message.Replace("@everyone", "'everyone'");
                                }
                                if (message.Contains("@here"))
                                {
                                    message = message.Replace("@here", "'here'");
                                }

                                // get user
                                var user =
                                _db.Users
                                    .Where(u => (u.GuildId == serverIdString) && (u.UserId == userId))
                                    .OrderByDescending(x => x.DateJoined)
                                    .FirstOrDefault();

                                if (user != null)
                                {
                                    // update random id for user if it's blank or older than an hour
                                    var lastRandomized = user.RandomIdUpdateTime;
                                    var randomId = 0;

                                    if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                    {
                                        Random rnd = new Random();
                                        randomId = rnd.Next(100000000);
                                        user.RandomId = randomId;
                                        user.RandomIdUpdateTime = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        randomId = user.RandomId;
                                    }

                                    // save changes to db
                                    _db.SaveChanges();

                                    await e.Message.Delete();
                                    await e.Channel.SendMessage($"**Anon {randomId}**: {message}");
                                }
                                else
                                {
                                    // if user == null, create user
                                    // create new user
                                    var newUser = new DiscordWebApp.Models.User()
                                    {
                                        GuildId = serverIdString,
                                        DateJoined = e.User.JoinedAt,
                                        UserId = e.User.Id.ToString(),
                                        Username = e.User.ToString()
                                    };

                                    // update random id for user if it's blank or older than an hour
                                    var lastRandomized = newUser.RandomIdUpdateTime;
                                    var randomId = 0;

                                    if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                    {
                                        Random rnd = new Random();
                                        randomId = rnd.Next(100000000);
                                        newUser.RandomId = randomId;
                                        newUser.RandomIdUpdateTime = DateTime.UtcNow;
                                    }
                                    else
                                    {
                                        randomId = newUser.RandomId;
                                    }

                                    // add user to server
                                    currentServer.Users.Add(newUser);

                                    // save changes to db
                                    _db.SaveChanges();

                                    await e.Message.Delete();
                                    await e.Channel.SendMessage($"**Anon {randomId}**: {message}");

                                }
                            }   
                        }
                    }
                });

            // message into server from DMs
            commands.CreateCommand("anon diary")
                .Parameter("message", ParameterType.Required)
                .Do(async (e) =>
                {
                    // get server, channel, and role
                    var server = discord.FindServers("Internet Addicts").FirstOrDefault();
                    var logChannel = server.FindChannels("dear_diary").FirstOrDefault();
                    var diaryRole = server.FindRoles("dear diary").FirstOrDefault();

                    var _u = server.GetUser(e.User.Id);

                    if (e.Server == null && _u.HasRole(diaryRole))
                    {
                        using (var _db = new DiscordWebAppDb())
                        {
                            // get server and user info
                            var serverIdString = server.Id.ToString();
                            var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                            var userId = e.User.Id.ToString();

                            // get message
                            var message = e.GetArg("message");
                            if (message.Contains("@everyone"))
                            {
                                message = message.Replace("@everyone", "'everyone'");
                            }
                            if (message.Contains("@here"))
                            {
                                message = message.Replace("@here", "'here'");
                            }

                            // get user
                            var user =
                            _db.Users
                                .Where(u => (u.GuildId == serverIdString) && (u.UserId == userId))
                                .OrderByDescending(x => x.DateJoined)
                                .FirstOrDefault();

                            if (user != null)
                            {
                                // update random id for user if it's blank or older than an hour
                                var lastRandomized = user.RandomIdUpdateTime;
                                var randomId = 0;

                                if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                {
                                    Random rnd = new Random();
                                    randomId = rnd.Next(100000000);
                                    user.RandomId = randomId;
                                    user.RandomIdUpdateTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    randomId = user.RandomId;
                                }

                                // save changes to db
                                _db.SaveChanges();

                                await e.User.SendMessage("Successfully posted to the **dear_diary** channel!");
                                await logChannel.SendMessage($"**Anon {randomId}**: {message}");
                            }
                            else
                            {
                                // if user == null, create user
                                // create new user
                                var newUser = new DiscordWebApp.Models.User()
                                {
                                    GuildId = serverIdString,
                                    DateJoined = e.User.JoinedAt,
                                    UserId = e.User.Id.ToString(),
                                    Username = e.User.ToString()
                                };

                                // update random id for user if it's blank or older than an hour
                                var lastRandomized = newUser.RandomIdUpdateTime;
                                var randomId = 0;

                                if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                {
                                    Random rnd = new Random();
                                    randomId = rnd.Next(100000000);
                                    newUser.RandomId = randomId;
                                    newUser.RandomIdUpdateTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    randomId = newUser.RandomId;
                                }

                                // add user to server
                                currentServer.Users.Add(newUser);

                                // save changes to db
                                _db.SaveChanges();

                                await e.User.SendMessage("Successfully posted to the **dear_diary** channel!");
                                await logChannel.SendMessage($"**Anon {randomId}**: {message}");

                            }
                        }
                    }
                });

            // message into server from DMs
            commands.CreateCommand("anon shitpost")
                .Parameter("message", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (e.Server == null)
                    {
                        // get server and channel
                        var server = discord.FindServers("Internet Addicts").FirstOrDefault();
                        var logChannel = server.FindChannels("gen_chat_shitposting").FirstOrDefault();

                        using (var _db = new DiscordWebAppDb())
                        {
                            // get server and user info
                            var serverIdString = server.Id.ToString();
                            var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                            var userId = e.User.Id.ToString();

                            // get message
                            var message = e.GetArg("message");
                            if (message.Contains("@everyone"))
                            {
                                message = message.Replace("@everyone", "'everyone'");
                            }
                            if (message.Contains("@here"))
                            {
                                message = message.Replace("@here", "'here'");
                            }

                            // get user
                            var user =
                            _db.Users
                                .Where(u => (u.GuildId == serverIdString) && (u.UserId == userId))
                                .OrderByDescending(x => x.DateJoined)
                                .FirstOrDefault();

                            if (user != null)
                            {
                                // update random id for user if it's blank or older than an hour
                                var lastRandomized = user.RandomIdUpdateTime;
                                var randomId = 0;

                                if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                {
                                    Random rnd = new Random();
                                    randomId = rnd.Next(100000000);
                                    user.RandomId = randomId;
                                    user.RandomIdUpdateTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    randomId = user.RandomId;
                                }

                                // save changes to db
                                _db.SaveChanges();

                                await e.User.SendMessage("Successfully posted to the **gen_chat_shitposting** channel!");
                                await logChannel.SendMessage($"**Anon {randomId}**: {message}");
                            }
                            else
                            {
                                // if user == null, create user
                                // create new user
                                var newUser = new DiscordWebApp.Models.User()
                                {
                                    GuildId = serverIdString,
                                    DateJoined = e.User.JoinedAt,
                                    UserId = e.User.Id.ToString(),
                                    Username = e.User.ToString()
                                };

                                // update random id for user if it's blank or older than an hour
                                var lastRandomized = newUser.RandomIdUpdateTime;
                                var randomId = 0;

                                if (lastRandomized == null || lastRandomized <= DateTime.UtcNow.AddHours(-1))
                                {
                                    Random rnd = new Random();
                                    randomId = rnd.Next(100000000);
                                    newUser.RandomId = randomId;
                                    newUser.RandomIdUpdateTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    randomId = newUser.RandomId;
                                }

                                // add user to server
                                currentServer.Users.Add(newUser);

                                // save changes to db
                                _db.SaveChanges();

                                await e.User.SendMessage("Successfully posted to the **gen_chat_shitposting** channel!");
                                await logChannel.SendMessage($"**Anon {randomId}**: {message}");

                            }
                        }
                    }
                });

            // reset anon id (before the one hour is up)
            commands.CreateCommand("anon reset")
                .Do(async (e) =>
                {
                    using (var _db = new DiscordWebAppDb())
                    {
                        // get server and user info
                        var serverIdString = "";
                        if (e.Server == null)
                        {
                            var server = discord.FindServers("Internet Addicts").FirstOrDefault();
                            serverIdString = server.Id.ToString();
                        }
                        else
                        {
                            serverIdString = e.Server.Id.ToString();
                        }
                        var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                        var userId = e.User.Id.ToString();

                        // get user
                        var user =
                        _db.Users
                            .Where(u => (u.GuildId == serverIdString) && (u.UserId == userId))
                            .OrderByDescending(x => x.DateJoined)
                            .FirstOrDefault();

                        var randomId = 0;

                        if (user != null)
                        {
                            Random rnd = new Random();
                            randomId = rnd.Next(100000000);
                            user.RandomId = randomId;
                            user.RandomIdUpdateTime = DateTime.UtcNow;
                        }

                        _db.SaveChanges();

                        // delete if in server, not in PMs
                        if (e.Server != null)
                        {
                            await e.Message.Delete();
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
                        if (e.Server != null)
                        {
                            // get server and user info
                            var serverIdString = e.Server.Id.ToString();
                            var currentServer = _db.Servers.Where(g => g.GuildId == serverIdString).SingleOrDefault();

                            var userId = e.User.Id.ToString();


                            // select user
                            // select most recent user match from db since same user might leave and come back
                            var user =
                                _db.Users
                                    .Where(u => (u.GuildId == serverIdString) && (u.UserId == userId))
                                    .OrderByDescending(x => x.DateJoined)
                                    .FirstOrDefault();

                            if (user != null)
                            {
                                // update last active
                                user.LastActive = DateTime.UtcNow;

                                // save changes to db
                                _db.SaveChanges();
                            }
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
