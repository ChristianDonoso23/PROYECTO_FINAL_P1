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
    [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
    public class PacienteController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Paciente
        public ActionResult Index()
        {
            var pacientes = db.pacientes.Include(p => p.AspNetUsers);
            return View(pacientes.ToList());
        }

        // GET: Paciente/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            return View(paciente);
        }

        // GET: Paciente/Create
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create()
        {
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            // 🔹 Traer todos los usuarios
            var todosUsuarios = userManager.Users.ToList();

            // 🔹 Usuarios con rol Paciente
            var usuariosConRolPaciente = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Paciente"))
                .ToList();

            // 🔹 Usuarios con ficha ya creada
            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();

            // 🔹 Solo los usuarios disponibles sin ficha
            var usuariosDisponibles = usuariosConRolPaciente
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();

            // Combo de usuarios
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email");

            // Combo de fotos
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos);

            return View();
        }

        // POST: Paciente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            // 🔧 Política de contraseñas
            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 1,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };

            // Cargar fotos
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            // Usuarios disponibles
            var todosUsuarios = userManager.Users.ToList();
            var usuariosConRolPaciente = todosUsuarios.Where(u => userManager.IsInRole(u.Id, "Paciente")).ToList();
            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolPaciente.Where(u => !usuariosConFicha.Contains(u.Id)).ToList();
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email", paciente.IdUsuario);

            if (!ModelState.IsValid)
                return View(paciente);

            // 🧩 Si no se seleccionó usuario, crear uno automáticamente
            if (string.IsNullOrEmpty(paciente.IdUsuario))
            {
                if (string.IsNullOrWhiteSpace(paciente.Nombre))
                {
                    ModelState.AddModelError("", "Debe ingresar un nombre para generar el usuario automáticamente.");
                    return View(paciente);
                }

                // ✉️ Generar correo
                var partes = paciente.Nombre.Trim().Split(' ');
                string primerNombre = partes.Length > 0 ? partes[0] : paciente.Nombre;
                string apellido = partes.Length > 1 ? partes[partes.Length - 1] : "paciente";

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

                // Crear usuario
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
                    return View(paciente);
                }

                // Asignar rol "Paciente"
                if (!userManager.IsInRole(nuevoUsuario.Id, "Paciente"))
                    userManager.AddToRole(nuevoUsuario.Id, "Paciente");

                // Asociar con el paciente
                paciente.IdUsuario = nuevoUsuario.Id;

                TempData["Mensaje"] = $"✅ Paciente creado correctamente.\nUsuario: {emailFinal}\nContraseña: 123";
            }

            db.pacientes.Add(paciente);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Paciente/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            // Fotos
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            ViewBag.IdUsuario = new SelectList(db.AspNetUsers.Where(u => u.Id == paciente.IdUsuario), "Id", "Email", paciente.IdUsuario);

            return View(paciente);
        }

        // POST: Paciente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            if (!ModelState.IsValid)
                return View(paciente);

            db.Entry(paciente).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Paciente/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            return View(paciente);
        }

        // POST: Paciente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var paciente = db.pacientes.Find(id);
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
