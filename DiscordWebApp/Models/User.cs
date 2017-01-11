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

        public string Username { get; set; }
        public DateTime DateCreated { get; set; }

        [Range(1, 100)]
        [Display(Name="Number of Flowers")]
        public int NumberOfFlowers { get; set; }
        public int ServerId { get; set; }
    }
}