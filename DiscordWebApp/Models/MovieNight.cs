using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class MovieNight
    {
        public int Id { get; set; }
        public string MovieNightName { get; set; }
        public DateTime DateCreated { get; set; }

        // quick hack to get it done
        // instead of datetime fields with error checking and input validation
        public string DateAndTime { get; set; }

        public virtual ICollection<Movie> Movies { get; set; }
    }
}