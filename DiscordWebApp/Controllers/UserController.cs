using DiscordWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace DiscordWebApp.Controllers
{
    public class UserController : Controller
    {
        DiscordWebAppDb _db = new DiscordWebAppDb();

        // GET: User
        public ActionResult Index([Bind(Prefix = "id")] int serverId, int page = 1)
        {
            var server = _db.Servers.Find(serverId);
            ViewBag.ServerName = server.Name;
            ViewBag.ServerId = server.Id;

            if (server != null) {
                return View(server.Users.OrderBy(x => x.Username).ToPagedList(page, 10));
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