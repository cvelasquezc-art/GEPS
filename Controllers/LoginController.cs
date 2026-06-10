using GEPS.Models;
using MongoDB.Driver;
using System;
using System.Web.Mvc;
using System.Web.Security;

namespace GEPS.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private IMongoCollection<Usuario> usuarios;
 
        public LoginController()
        {
            var db = ConexionMongo.ObtenerDB();
            usuarios = db.GetCollection<Usuario>("Usuarios");
        }

        // GET: Login
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated && TempData["Exito"] == null)
                return RedirectToAction("Index", "Admin");
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string cedula, string clave)
        {
            if (string.IsNullOrEmpty(cedula) || string.IsNullOrEmpty(clave))
            {
                ViewBag.Error = "Por favor ingresa tu cédula y contraseña.";
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cedula, @"^\d{10}$"))
            {
                ViewBag.CedulaError = "La cédula debe tener exactamente 10 dígitos.";
                return View();
            }

            var filtro = Builders<Usuario>.Filter.And(
                Builders<Usuario>.Filter.Eq("cedula", cedula),
                Builders<Usuario>.Filter.Eq("clave", clave),
                Builders<Usuario>.Filter.Eq("activo", true)
            );

            var usuario = usuarios.Find(filtro).FirstOrDefault();

            if (usuario == null)
            {
                ViewBag.Error = "Cédula o contraseña incorrecta.";
                return View();
            }

            Session.Clear();
            FormsAuthentication.SetAuthCookie(cedula, false);
            Session["Nombre"] = usuario.Nombre;
            Session["Rol"] = usuario.Rol;
            Session["Cedula"] = usuario.Cedula;
            Session["IdSemillero"] = usuario.IdSemillero;

            switch (usuario.Rol)
            {
                case "Administrador":
                    return RedirectToAction("Index", "Admin");
                case "Lider":
                    return RedirectToAction("Index", "Lider");
                case "Investigador":
                    return RedirectToAction("Index", "Investigador");
                default:
                    ViewBag.Error = "Rol no reconocido.";
                    return View();
            }
        }

        // GET: Cerrar sesión
        public ActionResult Salir()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Login");
        }



        // PASO 1: Olvidé contraseña
        public ActionResult OlvideContrasena()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OlvideContrasena(string cedula)
        {
            if (string.IsNullOrEmpty(cedula) ||
                !System.Text.RegularExpressions.Regex.IsMatch(cedula, @"^\d{10}$"))
            {
                ViewBag.Error = "Ingresa una cédula válida de 10 dígitos.";
                return View();
            }

            var filtro = Builders<Usuario>.Filter.Eq("cedula", cedula);
            var usuario = usuarios.Find(filtro).FirstOrDefault();

            if (usuario == null)
            {
                ViewBag.Error = "No existe un usuario registrado con esa cédula.";
                return View();
            }

            var random = new Random();
            string codigo = random.Next(100000, 999999).ToString();

            var update = Builders<Usuario>.Update
                .Set("codigoRecuperacion", codigo)
                .Set("codigoExpira", DateTime.Now.AddMinutes(10));
            usuarios.UpdateOne(filtro, update);

            string cuerpo = $@"
<!DOCTYPE html>
<html lang='es'>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f0f4f8;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f0f4f8;padding:32px 16px;'>
    <tr><td align='center'>
      <table width='500' cellpadding='0' cellspacing='0' style='background:white;border-radius:16px;overflow:hidden;border:1px solid #e0e0e0;'>
        <!-- HEADER -->
        <tr>
          <td style='background:linear-gradient(135deg,#0d2f5e 0%,#1a5276 55%,#1e8449 100%);padding:32px;text-align:center;'>
            <div style='font-size:30px;font-weight:900;color:white;letter-spacing:3px;'>GEP<span style='color:#4fc3a1;'>S</span></div>
            <div style='color:rgba(255,255,255,0.65);font-size:12px;margin-top:5px;letter-spacing:0.5px;'>Gestión Eficiente para Semilleros</div>
            <div style='width:36px;height:3px;background:#4fc3a1;border-radius:2px;margin:14px auto 0;'></div>
          </td>
        </tr>
        <!-- BODY -->
        <tr>
          <td style='padding:32px;'>
            <p style='font-size:15px;color:#333;margin:0 0 6px;'>Hola <strong style='color:#0d2f5e;'>{usuario.Nombre}</strong>,</p>
            <p style='font-size:14px;color:#666;margin:0 0 24px;'>Recibimos una solicitud para restablecer tu contraseña. Usa el siguiente código de verificación:</p>
            <!-- CÓDIGO -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background:linear-gradient(135deg,#f0f7ff,#e8f5ee);border:1.5px solid #4fc3a1;border-radius:12px;margin-bottom:20px;'>
              <tr><td style='padding:24px 16px;text-align:center;'>
                <div style='font-size:11px;font-weight:700;letter-spacing:2px;color:#1a5276;text-transform:uppercase;margin-bottom:10px;'>Código de verificación</div>
                <div style='font-size:40px;font-weight:900;letter-spacing:12px;color:#0d2f5e;font-family:monospace;'>{codigo}</div>
                <div style='margin-top:12px;font-size:12px;color:#888;'>
                  <span style='display:inline-block;width:6px;height:6px;border-radius:50%;background:#4fc3a1;vertical-align:middle;margin-right:6px;'></span>
                  Expira en <strong style='color:#1e8449;'>10 minutos</strong>
                </div>
              </td></tr>
            </table>
            <!-- ADVERTENCIA -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background:#fff8e1;border-left:3px solid #f59e0b;border-radius:0 8px 8px 0;margin-bottom:24px;'>
              <tr><td style='padding:10px 14px;font-size:12px;color:#92400e;'>
                ⚠️ Si no solicitaste este código, ignora este mensaje. Tu cuenta sigue segura.
              </td></tr>
            </table>
            <p style='font-size:13px;color:#aaa;text-align:center;margin:0;'>Este es un correo automático, no respondas a este mensaje.</p>
          </td>
        </tr>
        <!-- FOOTER -->
        <tr>
          <td style='border-top:1px solid #eee;padding:18px 32px;text-align:center;background:#fafafa;'>
            <div style='font-size:12px;font-weight:700;color:#1a5276;letter-spacing:1px;'>GEPS</div>
            <p style='font-size:11px;color:#aaa;line-height:1.6;margin:4px 0 0;'>Gestión Eficiente para Semilleros<br>© 2025 — Todos los derechos reservados</p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            bool enviado = ServicioCorreo.Enviar(usuario.Correo,
                "Código de recuperación – GEPS", cuerpo);

            if (!enviado)
            {
                ViewBag.Error = "No se pudo enviar el correo. Verifica la configuración SMTP.";
                return View();
            }

            Session["CedulaRecuperacion"] = cedula;
            return RedirectToAction("VerificarCodigo");
        }

        // PASO 2: Verificar código
        public ActionResult VerificarCodigo()
        {
            if (Session["CedulaRecuperacion"] == null)
                return RedirectToAction("OlvideContrasena");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerificarCodigo(string codigo)
        {
            string cedula = Session["CedulaRecuperacion"]?.ToString();
            if (cedula == null)
                return RedirectToAction("OlvideContrasena");

            var filtro = Builders<Usuario>.Filter.Eq("cedula", cedula);
            var usuario = usuarios.Find(filtro).FirstOrDefault();

            if (usuario == null || usuario.CodigoRecuperacion != codigo)
            {
                ViewBag.Error = "Código incorrecto. Intenta de nuevo.";
                return View();
            }

            if (usuario.CodigoExpira < DateTime.Now)
            {
                ViewBag.Error = "El código ha expirado. Solicita uno nuevo.";
                return RedirectToAction("OlvideContrasena");
            }

            Session["CodigoVerificado"] = true;
            return RedirectToAction("NuevaContrasena");
        }

        // PASO 3: Nueva contraseña
        public ActionResult NuevaContrasena()
        {
            if (Session["CedulaRecuperacion"] == null ||
                Session["CodigoVerificado"] == null)
                return RedirectToAction("OlvideContrasena");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NuevaContrasena(string clave, string confirmarClave)
        {
            string cedula = Session["CedulaRecuperacion"]?.ToString();

            if (cedula == null || Session["CodigoVerificado"] == null)
                return RedirectToAction("OlvideContrasena");

            if (string.IsNullOrEmpty(clave) || clave != confirmarClave)
            {
                ViewBag.Error = "Las contraseñas no coinciden o están vacías.";
                return View();
            }

            var filtro = Builders<Usuario>.Filter.Eq("cedula", cedula);
            var update = Builders<Usuario>.Update
                .Set("clave", clave)
                .Unset("codigoRecuperacion")
                .Unset("codigoExpira");

            usuarios.UpdateOne(filtro, update);

            Session.Remove("CedulaRecuperacion");
            Session.Remove("CodigoVerificado");

            FormsAuthentication.SignOut();
            Session.Abandon();

            TempData["Exito"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Index", "Login");
        }
    }
}