using DiscordWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        public ActionResult Create(int serverId) {
            return View();
        }

        [HttpPost]
        public ActionResult Create(User user) {
            if (ModelState.IsValid) {
                _db.Users.Add(user);
                _db.SaveChanges();
                return RedirectToAction("Index", new { id = user.ServerId });
            }
            return View();
        }

        [HttpGet]
        public ActionResult Edit(int id) {
            var model = _db.Users.Find(id);
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(User user) {
            if (ModelState.IsValid) {
                _db.Entry(user).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index", new { id = user.ServerId });
            }
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            _db.Dispose();
            base.Dispose(disposing);
        }
    }
}