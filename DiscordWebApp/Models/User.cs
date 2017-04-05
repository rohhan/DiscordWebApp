using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string Username { get; set; }

        [Display(Name = "Date Joined")]
        public DateTime DateJoined { get; set; }

        [Display(Name = "Joined From")]
        public string JoinedFrom { get; set; }

        [Display(Name = "Date Left")]
        public DateTime? DateLeft { get; set; }

        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Display(Name = "Last Online")]
        public DateTime? LastOnline { get; set; }

        [Display(Name = "Last Active")]
        public DateTime? LastActive { get; set; }

        public int? Age { get; set; }
        public string Sex { get; set; }
        public string Location { get; set; }
        public string Notes { get; set; }

        [Display(Name="Number of Flowers")]
        public int? NumberOfFlowers { get; set; }

        public int RandomId { get; set; }
        public DateTime? RandomIdUpdateTime { get; set; }

        public int ServerId { get; set; }
        public string GuildId { get; set; }
    }
}