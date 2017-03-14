using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MovieLengthInMinutes { get; set; }
        public string Director { get; set; }
        public string Genre { get; set; }
        public string Rating { get; set; }
    }
}