using clientejwt.Models;
using clientejwt.Services.Backend;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace clientejwt.Controllers
{
    [Authorize]
    public class CuentasController : Controller
    {
        private readonly IBackend _backend;

        public CuentasController(IBackend backend)
        {
            _backend = backend;
        }

        // Este atributo permite que /Cuentas/Login si pueda ser accedido aunque no se este logueado
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Esta función verifica en backend que el correo y contraseña sean válidos
                    var authUser = await _backend.AutenticacionAsync(model.Correo, model.Password);
                    // Regresa null si no es válido

                    if (authUser == null)
                    {
                        ModelState.AddModelError("Correo", "Credenciales no válidas. Inténtelo nuevamente.");
                    }
                    else
                    {
                        var claims = new List<Claim>
                        {
                            // Todo esto se guarda en la Cookie
                            new Claim(ClaimTypes.Name, authUser.Email),
                            new Claim(ClaimTypes.GivenName, authUser.Nombre),
                            new Claim(ClaimTypes.Email, authUser.Email),
                            new Claim("token", authUser.AccessToken),
                            new Claim(ClaimTypes.Role, authUser.Rol),
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties();
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                        // Usuario válido, lo envía al Home
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> PerfilAsync()
        {
            // Obtiene el perfil del usuario
            var token = User.FindFirstValue("token");
            var correo = User.FindFirstValue(ClaimTypes.Email);
            ViewData["token"] = User.FindFirstValue("token");

            var usuario = await _backend.GetUsuarioAsync(correo, token);
            
            return View(usuario);
        }

        public async Task<IActionResult> LogoutAsync(string returnUrl = null)
        {
            // Cierra la sesión
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (returnUrl != null)
            {
                // Si hay una acción a donde regresar, se realiza un redirect
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Sino, se redirige al inicio de sesión
                return RedirectToAction("Login");
            }
        }
    }
}
