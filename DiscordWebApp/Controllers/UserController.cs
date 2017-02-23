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
        public ActionResult Index([Bind(Prefix = "id")] int serverId, string searchTerm = null, int page = 1, string sortBy= null)
        {

            // for sorting
            ViewBag.SortNameParameter = string.IsNullOrEmpty(sortBy) ? "Name desc" : "";
            ViewBag.SortLastActiveParameter = sortBy == "Active" ? "Active desc" : "Active";
            ViewBag.SortDateJoindParameter = sortBy == "DateJoined" ? "DateJoined desc" : "DateJoined";


            // get server info
            var server = _db.Servers.Find(serverId);
            ViewBag.ServerName = server.Name;
            ViewBag.ServerId = server.Id;

            // start building model
            var model =
                server.Users
                    .OrderBy(x => x.Username)
                    .Where(x => searchTerm == null || x.Username.Contains(searchTerm));

            // Sorting
            switch (sortBy)
            {
                case "Active desc":
                    model = model.OrderByDescending(x => x.LastActive);
                    break;
                case "Active":
                    model = model.OrderBy(x => x.LastActive);
                    break;
                case "DateJoined desc":
                    model = model.OrderByDescending(x => x.DateJoined);
                    break;
                case "DateJoined":
                    model = model.OrderBy(x => x.DateJoined);
                    break;
                case "Name desc":
                    model = model.OrderByDescending(x => x.Username);
                    break;
                default:
                    model = model.OrderBy(x => x.Username);
                    break;
            }

            if (server != null) {
                return View(model.ToPagedList(page, 10));
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