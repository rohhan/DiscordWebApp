using DiscordWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DiscordWebApp.Controllers
{
    public class HomeController : Controller
    {
        DiscordWebAppDb _db = new DiscordWebAppDb();

        public ActionResult Index()
        {
            //var model =
            //    from s in _db.Servers
            //    select new ServerListViewModel
            //    {
            //        Id = s.Id,
            //        Name = s.Name,
            //        ServerOwner = s.ServerOwner,
            //        CountOfUsers = s.Users.Count()
            //    };

            var model =
                _db.Servers
                    .Select(s => new ServerListViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ServerOwner = s.ServerOwner,
                        CountOfUsers = s.Users.Count()
                    }
                    );

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (_db != null) {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}