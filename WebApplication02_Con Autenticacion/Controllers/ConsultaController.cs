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
    public class ConsultaController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Consulta
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Index()
        {
            var consultas = db.consultas.Include(c => c.pacientes).Include(c => c.medicos);
            return View(consultas.ToList());
        }

        // GET: Consulta/Details/5
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            return View(consulta);
        }

        // GET: Consulta/Create
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create()
        {
            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre");
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre");
            return View();
        }

        // POST: Consulta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create([Bind(Include = "IdConsulta,IdMedico,IdPaciente,FechaConsulta,HI,HF,Diagnostico")] consultas consulta)
        {
            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);

            if (!consulta.HorarioValido())
            {
                ModelState.AddModelError("HF", "La hora de fin debe ser mayor que la hora de inicio.");
            }

            if (!consulta.FechaValida())
            {
                ModelState.AddModelError("FechaConsulta", "La fecha de la consulta no puede ser futura.");
            }

            if (!ModelState.IsValid)
                return View(consulta);

            db.consultas.Add(consulta);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        // GET: Consulta/Edit/5
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);
            return View(consulta);
        }

        // POST: Consulta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit([Bind(Include = "IdConsulta,IdMedico,IdPaciente,FechaConsulta,HI,HF,Diagnostico")] consultas consulta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(consulta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);
            return View(consulta);
        }

        // GET: Consulta/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            return View(consulta);
        }

        // POST: Consulta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult DeleteConfirmed(int id)
        {
            consultas consulta = db.consultas.Find(id);
            db.consultas.Remove(consulta);
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
