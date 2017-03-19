using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DiscordWebApp.Models;

namespace DiscordWebApp.Controllers
{
    public class ServerController : Controller
    {
        private DiscordWebAppDb db = new DiscordWebAppDb();

        // GET: Server
        public ActionResult Index()
        {
            return View(db.Servers.ToList());
        }

        // GET: Server/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // get server info
            Server currentServer = db.Servers.Find(id);

            // get info for past N days
            var numDays = 7;
            var negativeNumDays = numDays * -1;

            if (currentServer == null)
            {
                return HttpNotFound();
            }
            else
            {
                // new users info for the past N days
                var numNewUsers =
                currentServer
                    .Users
                    .GroupBy(x => x.UserId) // because one user might have multiple rows (leave/rejoin)
                    .Select(x => x.First())
                    .Where(x => x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays))
                    .ToList()
                    .Count();

                var numNewUsersWhoLeft =
                    currentServer
                    .Users
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .Where(x => (x.DateJoined >= DateTime.UtcNow.Date.AddDays(negativeNumDays)) && (x.DateLeft >= DateTime.UtcNow.Date.AddDays(negativeNumDays)))
                    .ToList()
                    .Count();

                var numNewUsersWhoStayed = numNewUsers - numNewUsersWhoLeft;
                var percentRetention = (int)Math.Round((double)(100 * numNewUsersWhoStayed) / numNewUsers);

                var numTotalUsers =
                    currentServer
                    .Users
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .Where(x => x.DateLeft == null)
                    .ToList()
                    .Count();

                UserInfoViewModel model = new UserInfoViewModel()
                {
                    DbServerId = currentServer.Id,
                    GuildName = currentServer.Name,
                    TotalUserCount = numTotalUsers,
                    NewUserCount = numNewUsers,
                    NumNewUsersWhoStayed = numNewUsersWhoStayed,
                    NumNewUsersWhoLeft = numNewUsersWhoLeft,
                    NewUserPercentRetention = percentRetention
                };

                return View(model);
            }
            
        }

        // GET: Server/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Server/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,ServerOwner")] Server server)
        {
            if (ModelState.IsValid)
            {
                db.Servers.Add(server);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(server);
        }

        // GET: Server/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Server server = db.Servers.Find(id);
            if (server == null)
            {
                return HttpNotFound();
            }
            return View(server);
        }

        // POST: Server/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,ServerOwner")] Server server)
        {
            if (ModelState.IsValid)
            {
                db.Entry(server).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(server);
        }

        // GET: Server/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Server server = db.Servers.Find(id);
            if (server == null)
            {
                return HttpNotFound();
            }
            return View(server);
        }

        // POST: Server/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Server server = db.Servers.Find(id);
            db.Servers.Remove(server);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // user growth graph stuff
        public ActionResult Graph([Bind(Prefix = "id" )] int serverId)
        {
            var currentServer = db.Servers.Find(serverId);

            // not completely accurate - this only shows current users
            // we want to also add users who have left to the graph
            var users =
                    currentServer
                    .Users
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .Where(x => x.DateLeft == null)
                    .OrderBy(x => x.DateJoined)
                    .ToList();

            return PartialView("_Graph", users);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
