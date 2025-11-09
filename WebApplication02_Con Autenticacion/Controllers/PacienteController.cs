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

            /* Se listan todos los usuarios existentes */
            var todosUsuarios = userManager.Users.ToList();

            /* Filtramos solo los usuarios que tienen rol paciente */
            var usuariosConRolPaciente = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Paciente"))
                .ToList();

            /* Obtenemos los usuarios que ya tienen ficha de paciente */
            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolPaciente
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();

            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email");

            /* Listamos las fotos disponibles en la carpeta Imagenes */
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
            /* Recargamos el combo de fotos */
            string path = Server.MapPath("~/Imagenes");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            /* Se inicializa el contexto de Identity */
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            /* Se listan los usuarios con rol paciente que aún no tienen ficha */
            var todosUsuarios = userManager.Users.ToList();
            var usuariosConRolPaciente = todosUsuarios.Where(u => userManager.IsInRole(u.Id, "Paciente")).ToList();
            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolPaciente.Where(u => !usuariosConFicha.Contains(u.Id)).ToList();
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email", paciente.IdUsuario);

            /* Validación de la cédula ecuatoriana */
            if (!paciente.CedulaValidaEcuatoriana(paciente.Cedula))
            {
                ModelState.AddModelError("Cedula", "Cédula ecuatoriana inválida. Revise el número ingresado.");
                return View(paciente);
            }

            if (!ModelState.IsValid)
                return View(paciente);

            /* Crear usuario automáticamente si no se seleccionó uno */
            if (string.IsNullOrEmpty(paciente.IdUsuario))
            {
                /* Verifica que el paciente tenga un nombre válido */
                if (string.IsNullOrWhiteSpace(paciente.Nombre))
                {
                    ModelState.AddModelError("", "Debe ingresar un nombre para generar el usuario automáticamente.");
                    return View(paciente);
                }

                /* Divide el nombre para obtener primer nombre y apellido */
                var partes = paciente.Nombre.Trim().Split(' ');
                string primerNombre = partes.Length > 0 ? partes[0] : paciente.Nombre;
                string apellido = partes.Length > 1 ? partes[partes.Length - 1] : "paciente";

                /* Se limpia la cadena para evitar caracteres especiales y convertimos a minúsculas */
                string Sanitize(string s) =>
                    new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                    .Replace(" ", "")
                    .ToLowerInvariant();

                /* Genera el nombre base del usuario */
                string baseUsuario = (Sanitize(primerNombre).Length >= 2
                    ? Sanitize(primerNombre).Substring(0, 2)
                    : Sanitize(primerNombre)) + Sanitize(apellido);

                /* Construye el correo base y el nombre de usuario */
                string email = baseUsuario + "@hotmail.com";
                int contador = 1;
                string emailFinal = email;
                string usernameFinal = baseUsuario;

                /* Verifica duplicados de usuario/correo y ajusta con un número incremental */
                while (userManager.FindByEmail(emailFinal) != null || userManager.FindByName(usernameFinal) != null)
                {
                    emailFinal = $"{baseUsuario}{contador}@hotmail.com";
                    usernameFinal = $"{baseUsuario}{contador}";
                    contador++;
                }

                /* Configura una política de contraseñas simple (para permitir “123”) */
                userManager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 1,
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireUppercase = false,
                    RequireNonLetterOrDigit = false
                };

                /* Crea el nuevo usuario en Identity */
                var nuevoUsuario = new ApplicationUser
                {
                    UserName = usernameFinal,
                    Email = emailFinal,
                    EmailConfirmed = true
                };

                string password = "123";
                var result = userManager.Create(nuevoUsuario, password);

                /* Si hubo errores en la creación, los muestra en la vista */
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error);
                    return View(paciente);
                }

                /* Asigna el rol de Paciente si aún no lo tiene */
                if (!userManager.IsInRole(nuevoUsuario.Id, "Paciente"))
                    userManager.AddToRole(nuevoUsuario.Id, "Paciente");

                /* Vincula el usuario creado al registro del paciente */
                paciente.IdUsuario = nuevoUsuario.Id;
            }

            /* Guarda el paciente en la base de datos */
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

            /* Validación de cédula antes de guardar */
            if (!paciente.CedulaValidaEcuatoriana(paciente.Cedula))
            {
                ModelState.AddModelError("Cedula", "Cédula ecuatoriana inválida. Revise el número ingresado.");
                return View(paciente);
            }

            if (!ModelState.IsValid)
                return View(paciente);

            /* Se actualiza el registro del paciente */
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
            if (paciente == null)
                return HttpNotFound();

            string idUsuario = paciente.IdUsuario;

            /* Se elimina primero el registro del paciente */
            db.pacientes.Remove(paciente);
            db.SaveChanges();

            /* Luego se elimina el usuario asociado en Identity */
            if (!string.IsNullOrEmpty(idUsuario))
            {
                var identityDb = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

                var usuario = userManager.FindById(idUsuario);
                if (usuario != null)
                {
                    /* Se eliminan todos los roles antes de eliminar el usuario */
                    var roles = userManager.GetRoles(usuario.Id);
                    foreach (var rol in roles)
                        userManager.RemoveFromRole(usuario.Id, rol);

                    /* Se elimina el usuario definitivamente */
                    userManager.Delete(usuario);
                }
            }

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
