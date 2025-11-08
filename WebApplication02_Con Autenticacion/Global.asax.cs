using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebApplication02_Con_Autenticacion.Models;

namespace WebApplication02_Con_Autenticacion
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ApplicationDbContext db = new ApplicationDbContext();
            CrearRolesSistema(db);
            CrearUsuariosSistema(db);
            AsignarRolUsuario(db);
        }
        /* Metodo para crear los roles del sistema */
        private void CrearRolesSistema(ApplicationDbContext db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            string[] roles = { "SuperAdmin", "Administrador", "Medico", "Paciente" };

            foreach (var rol in roles)
            {
                if (!roleManager.RoleExists(rol))
                {
                    roleManager.Create(new IdentityRole(rol));
                    System.Diagnostics.Debug.WriteLine($"Rol '{rol}' creado correctamente.");
                }
            }
        }
        /* Metodo para crear los usuarios del sistema */
        private void CrearUsuariosSistema(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 1,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };

            CrearUsuarioSistema(userManager, "SuperAdmin", "superadmin@veris.com");
            CrearUsuarioSistema(userManager, "ADM", "admin@veris.com");
        }
        /* Metodo auxiliar para crear un usuario */
        private void CrearUsuarioSistema(UserManager<ApplicationUser> userManager, string username, string email)
        {
            var usuario = userManager.FindByName(username);
            if (usuario == null)
            {
                var nuevoUsuario = new ApplicationUser
                {
                    UserName = username,
                    Email = email
                };

                var resultado = userManager.Create(nuevoUsuario, "123");

                if (resultado.Succeeded)
                    System.Diagnostics.Debug.WriteLine($" Usuario '{username}' creado correctamente (contraseña: 123).");
                else
                    System.Diagnostics.Debug.WriteLine($" Error al crear el usuario '{username}': {string.Join(", ", resultado.Errors)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($" El usuario '{username}' ya existe.");
            }
        }
        /* Metodo para asignar roles a los usuarios del sistema */
        private void AsignarRolUsuario(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var superadmin = userManager.FindByName("SuperAdmin");
            if (superadmin != null)
            {
                AsignarRoles(userManager, superadmin.Id, "SuperAdmin", "Administrador", "Medico", "Paciente");
            }

            var administrador = userManager.FindByName("ADM");
            if (administrador != null)
            {
                AsignarRoles(userManager, administrador.Id, "Administrador");
            }
        }
        /* Metodo auxiliar para asignar varios roles a un usuario */
        private void AsignarRoles(UserManager<ApplicationUser> userManager, string userId, params string[] roles)
        {
            foreach (var rol in roles)
            {
                if (!userManager.IsInRole(userId, rol))
                {
                    userManager.AddToRole(userId, rol);
                    System.Diagnostics.Debug.WriteLine($" Rol '{rol}' asignado al usuario ID={userId}.");
                }
            }
        }
    }
}
