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
    public class PacienteController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Paciente
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Index()
        {
            var pacientes = db.pacientes.Include(p => p.AspNetUsers);
            return View(pacientes.ToList());
        }

        // GET: Paciente/Details/5
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            pacientes paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            return View(paciente);
        }

        // GET: Paciente/Create
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create()
        {
            ViewBag.IdUsuario = new SelectList(db.AspNetUsers, "Id", "Email");
            return View();
        }

        // POST: Paciente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = new SelectList(db.AspNetUsers, "Id", "Email", paciente.IdUsuario);
                return View(paciente);
            }

            db.pacientes.Add(paciente);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Paciente/Edit/5
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            pacientes paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            ViewBag.IdUsuario = new SelectList(db.AspNetUsers, "Id", "Email", paciente.IdUsuario);
            return View(paciente);
        }

        // POST: Paciente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Edit([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = new SelectList(db.AspNetUsers, "Id", "Email", paciente.IdUsuario);
                return View(paciente);
            }

            db.Entry(paciente).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Paciente/Delete/5
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            pacientes paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            return View(paciente);
        }

        // POST: Paciente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            pacientes paciente = db.pacientes.Find(id);
            db.pacientes.Remove(paciente);
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
