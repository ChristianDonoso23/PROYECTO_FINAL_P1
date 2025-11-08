using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    public class RecetaController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Receta
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Index()
        {
            var recetas = db.recetas.Include(r => r.consultas).Include(r => r.medicamentos);
            return View(recetas.ToList());
        }

        // GET: Receta/Details/5
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas.Find(id);
            if (receta == null)
                return HttpNotFound();

            return View(receta);
        }

        // GET: Receta/Create
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create()
        {
            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico");
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre");
            return View();
        }

        // POST: Receta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create([Bind(Include = "IdReceta,IdConsulta,IdMedicamento,Cantidad")] recetas receta)
        {
            if (ModelState.IsValid)
            {
                db.recetas.Add(receta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico", receta.IdConsulta);
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);
            return View(receta);
        }

        // GET: Receta/Edit/5
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas.Find(id);
            if (receta == null)
                return HttpNotFound();

            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico", receta.IdConsulta);
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);
            return View(receta);
        }

        // POST: Receta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit([Bind(Include = "IdReceta,IdConsulta,IdMedicamento,Cantidad")] recetas receta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(receta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico", receta.IdConsulta);
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);
            return View(receta);
        }

        // GET: Receta/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas.Find(id);
            if (receta == null)
                return HttpNotFound();

            return View(receta);
        }

        // POST: Receta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult DeleteConfirmed(int id)
        {
            recetas receta = db.recetas.Find(id);
            db.recetas.Remove(receta);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
