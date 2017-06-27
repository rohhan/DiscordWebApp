using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class Campaign
    {
        public int Id { get; set; }
        public string DungeonmMasterId { get; set; }
        public string DungeonMasterName { get; set; }
        public string Time { get; set; }
        public string Descriptor { get; set; }
        public ICollection<CampaignPlayer> Players { get; set; }
    }
}