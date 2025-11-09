using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperAdmin, Administrador")]
    public class MedicoController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Medico
        public ActionResult Index()
        {
            var medicos = db.medicos.Include(m => m.AspNetUsers).Include(m => m.especialidades);
            return View(medicos.ToList());
        }

        // GET: Medico/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var medico = db.medicos.Find(id);
            if (medico == null)
                return HttpNotFound();

            return View(medico);
        }

        // GET: Medico/Create
        public ActionResult Create()
        {
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            var todosUsuarios = userManager.Users.ToList();

            // 🔹 Usuarios con rol "Medico"
            var usuariosConRolMedico = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Medico"))
                .ToList();

            // 🔹 Excluir los que ya tienen ficha
            var usuariosConFicha = db.medicos.Select(m => m.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolMedico
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();

            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email");
            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion");

            // 🔹 Cargar imágenes desde la carpeta Imagenes
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos);

            return View();
        }

        // POST: Medico/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IdMedico,IdUsuario,Nombre,IdEspecialidad,Foto")] medicos medico)
        {
            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", medico.IdEspecialidad);

            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, medico.Foto);

            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 1,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonLetterOrDigit = false
            };

            var todosUsuarios = userManager.Users.ToList();
            var usuariosConRolMedico = todosUsuarios.Where(u => userManager.IsInRole(u.Id, "Medico")).ToList();
            var usuariosConFicha = db.medicos.Select(m => m.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolMedico.Where(u => !usuariosConFicha.Contains(u.Id)).ToList();
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email", medico.IdUsuario);

            if (!ModelState.IsValid)
                return View(medico);

            // 🔹 Crear usuario automáticamente si no se selecciona
            if (string.IsNullOrEmpty(medico.IdUsuario))
            {
                if (string.IsNullOrWhiteSpace(medico.Nombre))
                {
                    ModelState.AddModelError("", "Debe ingresar un nombre para generar el usuario automáticamente.");
                    return View(medico);
                }

                var partes = medico.Nombre.Trim().Split(' ');
                string primerNombre = partes.Length > 0 ? partes[0] : medico.Nombre;
                string apellido = partes.Length > 1 ? partes[partes.Length - 1] : "medico";

                string Sanitize(string s) =>
                    new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                    .Replace(" ", "")
                    .ToLowerInvariant();

                string baseUsuario = (Sanitize(primerNombre).Length >= 2
                    ? Sanitize(primerNombre).Substring(0, 2)
                    : Sanitize(primerNombre)) + Sanitize(apellido);

                string email = baseUsuario + "@hotmail.com";
                int contador = 1;
                string emailFinal = email;
                string usernameFinal = baseUsuario;

                while (userManager.FindByEmail(emailFinal) != null || userManager.FindByName(usernameFinal) != null)
                {
                    emailFinal = $"{baseUsuario}{contador}@hotmail.com";
                    usernameFinal = $"{baseUsuario}{contador}";
                    contador++;
                }

                // 🔐 Crear usuario nuevo
                var nuevoUsuario = new ApplicationUser
                {
                    UserName = usernameFinal,
                    Email = emailFinal,
                    EmailConfirmed = true
                };

                string password = "123";
                var result = userManager.Create(nuevoUsuario, password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error);
                    return View(medico);
                }

                // Asignar rol "Medico"
                if (!userManager.IsInRole(nuevoUsuario.Id, "Medico"))
                    userManager.AddToRole(nuevoUsuario.Id, "Medico");

                medico.IdUsuario = nuevoUsuario.Id;
            }

            db.medicos.Add(medico);
            db.SaveChanges();

            TempData["Mensaje"] = "✅ Médico registrado correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Medico/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var medico = db.medicos.Find(id);
            if (medico == null)
                return HttpNotFound();

            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", medico.IdEspecialidad);

            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, medico.Foto);

            ViewBag.IdUsuario = new SelectList(db.AspNetUsers.Where(u => u.Id == medico.IdUsuario), "Id", "Email", medico.IdUsuario);

            return View(medico);
        }

        // POST: Medico/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdMedico,IdUsuario,Nombre,IdEspecialidad,Foto")] medicos medico)
        {
            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", medico.IdEspecialidad);

            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, medico.Foto);

            if (!ModelState.IsValid)
                return View(medico);

            db.Entry(medico).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Medico/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var medico = db.medicos.Find(id);
            if (medico == null)
                return HttpNotFound();

            return View(medico);
        }

        // ✅ POST: Medico/Delete/5 (con eliminación segura del usuario asociado)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var medico = db.medicos.Find(id);
            if (medico == null)
                return HttpNotFound();

            string idUsuario = medico.IdUsuario;

            // 1️⃣ Eliminar primero el médico (rompe la relación FK)
            db.medicos.Remove(medico);
            db.SaveChanges();

            // 2️⃣ Luego eliminar el usuario asociado
            if (!string.IsNullOrEmpty(idUsuario))
            {
                var identityDb = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

                var usuario = userManager.FindById(idUsuario);
                if (usuario != null)
                {
                    var roles = userManager.GetRoles(usuario.Id);
                    foreach (var rol in roles)
                        userManager.RemoveFromRole(usuario.Id, rol);

                    userManager.Delete(usuario);
                }
            }

            TempData["Mensaje"] = "🗑️ Médico y usuario eliminados correctamente.";
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
