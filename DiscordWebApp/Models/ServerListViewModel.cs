using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiscordWebApp.Models
{
    public class ServerListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ServerOwner { get; set; }
        public int CountOfUsers { get; set; }
    }
}