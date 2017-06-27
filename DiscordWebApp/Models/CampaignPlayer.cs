using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class CampaignPlayer
    {
        public int Id { get; set; }
        public string UserDiscordIdStr { get; set; }
        public string PlayerName { get; set; }
        public string PlayerClass { get; set; }
        public string DungeonMasterIdStr { get; set; }
        public string CampaignDescriptor { get; set; }
    }
}