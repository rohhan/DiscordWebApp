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
            ViewBag.SortDateJoindParameter = string.IsNullOrEmpty(sortBy) ? "DateJoined" : "";
            ViewBag.SortNameParameter = sortBy == "Name" ? "Name desc" : "Name";
            ViewBag.SortLastActiveParameter = sortBy == "Active" ? "Active desc" : "Active";


            // get server info
            var server = _db.Servers.Find(serverId);
            ViewBag.ServerName = server.Name;
            ViewBag.ServerId = server.Id;

            // start building model
            // don't include users who have left
            var model =
                server.Users
                    .Where(x => (searchTerm == null || x.Username.ToLower().Contains(searchTerm.ToLower())) &&(x.DateLeft == null))
                    .OrderByDescending(x => x.DateJoined);

            // Sorting
            switch (sortBy)
            {
                case "Active desc":
                    model = model.OrderByDescending(x => x.LastActive);
                    break;
                case "Active":
                    model = model.OrderBy(x => x.LastActive);
                    break;
                case "Name desc":
                    model = model.OrderByDescending(x => x.Username);
                    break;
                case "Name":
                    model = model.OrderBy(x => x.Username);
                    break;
                case "DateJoined":
                    model = model.OrderBy(x => x.DateJoined);
                    break;
                default:
                    model = model.OrderByDescending(x => x.DateJoined);
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