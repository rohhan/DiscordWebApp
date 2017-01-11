using DiscordWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DiscordWebApp.Controllers
{
    public class UserController : Controller
    {
        DiscordWebAppDb _db = new DiscordWebAppDb();

        // GET: User
        public ActionResult Index([Bind(Prefix = "id")] int serverId)
        {
            var server = _db.Servers.Find(serverId);
            if (server != null) {
                return View(server);
            }
            return HttpNotFound();
        }

        protected override void Dispose(bool disposing)
        {
            _db.Dispose();
            base.Dispose(disposing);
        }
    }
}