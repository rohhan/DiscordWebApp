using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class UserInfoViewModel
    {
        public int DbServerId { get; set; }
        public string GuildName { get; set; }
        public int TotalUserCount { get; set; }
        public int NewUserCount { get; set; }
        public int NumNewUsersWhoStayed { get; set; }
        public int NumNewUsersWhoLeft { get; set; }
        public int NewUserPercentRetention { get; set; }
    }
}