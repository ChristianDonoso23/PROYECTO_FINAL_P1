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

            // 🔹 Cargar todos los usuarios a memoria
            var todosUsuarios = userManager.Users.ToList();

            // 🔹 Filtrar usuarios con rol "Medico"
            var usuariosConRolMedico = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Medico"))
                .ToList();

            // 🔹 Excluir usuarios con ficha ya creada
            var usuariosConFicha = db.medicos.Select(m => m.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolMedico
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();

            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email");
            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion");

            // 📸 Cargar imágenes disponibles
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
            // 🔁 Recargar combos en caso de error
            ViewBag.IdEspecialidad = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", medico.IdEspecialidad);

            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, medico.Foto);

            // Configurar Identity
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 1,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };

            // Usuarios disponibles (para mantener el combo)
            var todosUsuarios = userManager.Users.ToList();
            var usuariosConRolMedico = todosUsuarios.Where(u => userManager.IsInRole(u.Id, "Medico")).ToList();
            var usuariosConFicha = db.medicos.Select(m => m.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolMedico.Where(u => !usuariosConFicha.Contains(u.Id)).ToList();
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email", medico.IdUsuario);

            if (!ModelState.IsValid)
                return View(medico);

            // 🧩 Si no se seleccionó usuario, crear uno automáticamente
            if (string.IsNullOrEmpty(medico.IdUsuario))
            {
                if (string.IsNullOrWhiteSpace(medico.Nombre))
                {
                    ModelState.AddModelError("", "Debe ingresar un nombre para generar el usuario automáticamente.");
                    return View(medico);
                }

                // Generar correo y username
                var partes = medico.Nombre.Trim().Split(' ');
                string primerNombre = partes.Length > 0 ? partes[0] : medico.Nombre;
                string apellido = partes.Length > 1 ? partes[partes.Length - 1] : "medico";

                string Sanitize(string s) =>
                    new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                    .Replace(" ", "")
                    .ToLowerInvariant();

                string baseUsuario = (Sanitize(primerNombre).Length >= 2 ? Sanitize(primerNombre).Substring(0, 2) : Sanitize(primerNombre)) + Sanitize(apellido);
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

                // Crear usuario nuevo
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

                TempData["Mensaje"] = $"✅ Médico creado correctamente.\nUsuario: {emailFinal}\nContraseña: 123";
            }

            db.medicos.Add(medico);
            db.SaveChanges();

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

            // Fotos
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, medico.Foto);

            // Evitar cambiar usuario vinculado
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

        // POST: Medico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var medico = db.medicos.Find(id);
            db.medicos.Remove(medico);
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
