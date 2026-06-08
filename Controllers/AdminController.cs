using GEPS.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;

namespace GEPS.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private IMongoCollection<Usuario> _usuarios;
        private IMongoCollection<Semillero> _semilleros;

        public AdminController()
        {
            var db = ConexionMongo.ObtenerDB();
            _usuarios = db.GetCollection<Usuario>("Usuarios");
            _semilleros = db.GetCollection<Semillero>("Semilleros");
        }

        // ── Verificar rol ──
        private bool EsAdmin()
        {
            return Session["Rol"]?.ToString() == "Administrador";
        }

        // ══════════════════════════════════════
        //  PANEL PRINCIPAL
        // ══════════════════════════════════════
        public ActionResult Index()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            ViewBag.TotalSemilleros = _semilleros.CountDocuments(Builders<Semillero>.Filter.Empty);
            ViewBag.SemillerosActivos = _semilleros.CountDocuments(Builders<Semillero>.Filter.Eq("activo", true));
            ViewBag.TotalLideres = _usuarios.CountDocuments(Builders<Usuario>.Filter.Eq("rol", "Lider"));

            return View();
        }

        // ══════════════════════════════════════
        //  SEMILLEROS — LISTAR
        // ══════════════════════════════════════
        public ActionResult Semilleros()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var lista = _semilleros.Find(Builders<Semillero>.Filter.Empty)
                                   .SortByDescending(s => s.FechaCreacion)
                                   .ToList();
            return View(lista);
        }

        // ══════════════════════════════════════
        //  SEMILLEROS — CREAR
        // ══════════════════════════════════════
        public ActionResult CrearSemillero()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearSemillero(Semillero semillero)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(semillero.Nombre) ||
                string.IsNullOrEmpty(semillero.LineaInvestigativa))
            {
                ViewBag.Error = "El nombre y la línea investigativa son obligatorios.";
                return View(semillero);
            }

            semillero.FechaCreacion = DateTime.Now;
            semillero.Activo = true;

            _semilleros.InsertOne(semillero);

            TempData["Exito"] = "Semillero creado correctamente.";
            return RedirectToAction("Semilleros");
        }

        // ══════════════════════════════════════
        //  SEMILLEROS — EDITAR
        // ══════════════════════════════════════
        public ActionResult EditarSemillero(string id)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var semillero = _semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (semillero == null) return RedirectToAction("Semilleros");
            return View(semillero);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarSemillero(Semillero semillero)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(semillero.Nombre) ||
                string.IsNullOrEmpty(semillero.LineaInvestigativa))
            {
                ViewBag.Error = "El nombre y la línea investigativa son obligatorios.";
                return View(semillero);
            }

            var filtro = Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(semillero.Id));
            var update = Builders<Semillero>.Update
                .Set("nombre", semillero.Nombre)
                .Set("lineaInvestigativa", semillero.LineaInvestigativa)
                .Set("descripcion", semillero.Descripcion);

            _semilleros.UpdateOne(filtro, update);

            TempData["Exito"] = "Semillero actualizado correctamente.";
            return RedirectToAction("Semilleros");
        }

        // ══════════════════════════════════════
        //  SEMILLEROS — HABILITAR / DESHABILITAR
        // ══════════════════════════════════════
        public ActionResult CambiarEstadoSemillero(string id)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(id));
            var semillero = _semilleros.Find(filtro).FirstOrDefault();

            if (semillero != null)
            {
                var update = Builders<Semillero>.Update
                    .Set("activo", !semillero.Activo);
                _semilleros.UpdateOne(filtro, update);

                TempData["Exito"] = semillero.Activo
                    ? "Semillero deshabilitado correctamente."
                    : "Semillero habilitado correctamente.";
            }

            return RedirectToAction("Semilleros");
        }

        // ══════════════════════════════════════
        //  LÍDERES — LISTAR
        // ══════════════════════════════════════
        public ActionResult Lideres()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var lista = _usuarios.Find(
                Builders<Usuario>.Filter.Eq("rol", "Lider")
            ).ToList();

            var semilleros = _semilleros.Find(
                Builders<Semillero>.Filter.Eq("activo", true)
            ).ToList();

            ViewBag.Semilleros = semilleros;
            return View(lista);
        }

        // ══════════════════════════════════════
        //  LÍDERES — CREAR CREDENCIALES
        // ══════════════════════════════════════
        public ActionResult CrearLider()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearLider(string nombre, string cedula, string correo)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombre) ||
                string.IsNullOrEmpty(cedula) ||
                string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            // Verificar que la cédula no exista
            var existe = _usuarios.Find(
                Builders<Usuario>.Filter.Eq("cedula", cedula)
            ).FirstOrDefault();

            if (existe != null)
            {
                ViewBag.Error = "Ya existe un usuario con esa cédula.";
                return View();
            }

            // Generar clave automática: GEPS + últimos 4 dígitos de la cédula
            string clave = "GEPS" + cedula.Substring(cedula.Length - 4);

            var lider = new Usuario
            {
                Nombre = nombre,
                Cedula = cedula,
                Correo = correo,
                Clave = clave,
                Rol = "Lider",
                Activo = true,
                IdSemillero = null
            };

            _usuarios.InsertOne(lider);

            // Enviar correo con credenciales
            string cuerpo = $@"
            <div style='font-family:Arial;max-width:500px;margin:auto;'>
                <div style='background:#0d2f5e;padding:20px;text-align:center;border-radius:8px 8px 0 0;'>
                    <h2 style='color:white;margin:0;'>GEPS</h2>
                    <p style='color:#ccc;margin:4px 0 0;'>Gestión Eficiente para Semilleros</p>
                </div>
                <div style='padding:24px;border:1px solid #ddd;border-radius:0 0 8px 8px;'>
                    <p>Hola <strong>{nombre}</strong>,</p>
                    <p>El administrador ha creado tus credenciales de acceso al sistema GEPS.</p>
                    <table style='width:100%;margin:16px 0;border-collapse:collapse;'>
                        <tr>
                            <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Cédula:</td>
                            <td style='padding:8px;'>{cedula}</td>
                        </tr>
                        <tr>
                            <td style='padding:8px;background:#f5f5f5;font-weight:bold;'>Contraseña:</td>
                            <td style='padding:8px;'>{clave}</td>
                        </tr>
                    </table>
                    <p style='color:#888;font-size:13px;'>Por seguridad te recomendamos cambiar tu contraseña al iniciar sesión por primera vez.</p>
                </div>
            </div>";

            ServicioCorreo.Enviar(correo, "Credenciales de acceso – GEPS", cuerpo);

            TempData["Exito"] = $"Líder creado correctamente. Clave asignada: {clave}";
            return RedirectToAction("Lideres");
        }

        // ══════════════════════════════════════
        //  LÍDERES — ASIGNAR A SEMILLERO
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarLider(string idLider, string idSemillero)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            // Solo actualiza idSemillero en el usuario líder
            var filtroUsuario = Builders<Usuario>.Filter.Eq("_id", ObjectId.Parse(idLider));
            var updateUsuario = Builders<Usuario>.Update
                .Set("idSemillero", idSemillero);
            _usuarios.UpdateOne(filtroUsuario, updateUsuario);

            TempData["Exito"] = "Líder asignado al semillero correctamente.";
            return RedirectToAction("Lideres");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Salir()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}



