using GEPS.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using static GEPS.Models.Proyecto;

namespace GEPS.Controllers
{
    [Authorize]
    public class LiderController : Controller
    {
        private IMongoCollection<Usuario> usuarios;
        private IMongoCollection<Semillero> semilleros;
        private IMongoCollection<Proyecto> proyectos;
        private IMongoCollection<Reunion> reuniones;
        private IMongoCollection<Evento> eventos;

        public LiderController()
        {
            var db = ConexionMongo.ObtenerDB();
            usuarios = db.GetCollection<Usuario>("Usuarios");
            semilleros = db.GetCollection<Semillero>("Semilleros");
            proyectos = db.GetCollection<Proyecto>("Proyectos");
            reuniones = db.GetCollection<Reunion>("Reuniones");
            eventos = db.GetCollection<Evento>("Eventos");
        }

        // ── Verificar rol ──
        private bool EsLider()
        {
            return Session["Rol"]?.ToString() == "Lider";
        }

        // ── Obtener líder actual ──
        private Usuario ObtenerLiderActual()
        {
            string cedula = Session["Cedula"]?.ToString();
            return usuarios.Find(
                Builders<Usuario>.Filter.Eq("cedula", cedula)
            ).FirstOrDefault();
        }

        // ══════════════════════════════════════
        //  PANEL PRINCIPAL
        // ══════════════════════════════════════
        public ActionResult Index()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            if (lider == null || string.IsNullOrEmpty(lider.IdSemillero))
            {
                ViewBag.SinSemillero = true;
                return View();
            }

            var semillero = semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(lider.IdSemillero))
            ).FirstOrDefault();

            var totalProyectos = proyectos.CountDocuments(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            );

            var proyectosActivos = proyectos.CountDocuments(
                Builders<Proyecto>.Filter.And(
                    Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero)),
                    Builders<Proyecto>.Filter.Eq("estado", "En ejecución")
                )
            );

            var idsProyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList().Select(p => ObjectId.Parse(p.Id)).ToList();

            var totalReuniones = reuniones.CountDocuments(
                Builders<Reunion>.Filter.In("idProyecto", idsProyectos)
            );

            var totalEventos = eventos.CountDocuments(
                Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos)
            );

            var totalInvestigadores = usuarios.CountDocuments(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Investigador"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero)),
                    Builders<Usuario>.Filter.Eq("activo", true)
                )
            );

            ViewBag.Semillero = semillero;
            ViewBag.TotalProyectos = totalProyectos;
            ViewBag.ProyectosActivos = proyectosActivos;
            ViewBag.TotalReuniones = totalReuniones;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.TotalInvestigadores = totalInvestigadores;

            return View();
        }

        // ══════════════════════════════════════
        //  INVESTIGADORES — LISTAR
        // ══════════════════════════════════════
        public ActionResult Investigadores()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            var lista = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Investigador"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
                )
            ).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  INVESTIGADORES — CREAR
        // ══════════════════════════════════════
        public ActionResult CrearInvestigador()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearInvestigador(string nombre, string cedula, string correo,
            string fechaNacimiento, string genero, string celular, string programa)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(cedula) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(fechaNacimiento) || string.IsNullOrEmpty(genero)
            || string.IsNullOrEmpty(celular) || string.IsNullOrEmpty(programa))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View();
            }

            // Verificar cédula duplicada
            var cedulaExiste = usuarios.Find(
                Builders<Usuario>.Filter.Eq("cedula", cedula)
            ).FirstOrDefault();

            if (cedulaExiste != null)
            {
                ViewBag.Error = "Ya existe un usuario con esa cédula.";
                return View();
            }

            // Verificar correo duplicado
            var correoExiste = usuarios.Find(
                Builders<Usuario>.Filter.Eq("correo", correo)
            ).FirstOrDefault();

            if (correoExiste != null)
            {
                ViewBag.Error = "Ya existe un usuario registrado con ese correo.";
                return View();
            }

            // Verificar celular duplicado
            var celularExiste = usuarios.Find(
                Builders<Usuario>.Filter.Eq("celular", celular)
            ).FirstOrDefault();

            if (celularExiste != null)
            {
                ViewBag.Error = "Ya existe un usuario registrado con ese número de celular.";
                return View();
            }

            var lider = ObtenerLiderActual();

            // Generar clave automática
            string clave = "GEPS" + cedula.Substring(cedula.Length - 4);

            var investigador = new Usuario
            {
                Nombre = nombre,
                Cedula = cedula,
                Correo = correo,
                Clave = clave,
                Rol = "Investigador",
                FechaNacimiento = string.IsNullOrEmpty(fechaNacimiento) ? (DateTime?)null : DateTime.Parse(fechaNacimiento),
                Genero = genero,
                Celular = celular,
                Programa = programa,
                IdSemillero = lider.IdSemillero,
                Activo = true
            };

            usuarios.InsertOne(investigador);

            // Enviar correo con credenciales
            string cuerpo = $@"<!DOCTYPE html>
<html lang='es'>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f0f4f8;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f0f4f8;padding:32px 16px;'>
    <tr><td align='center'>
      <table width='500' cellpadding='0' cellspacing='0' style='background:white;border-radius:16px;overflow:hidden;border:1px solid #e0e0e0;'>
        <tr>
          <td style='background:linear-gradient(135deg,#0d2f5e 0%,#1a5276 55%,#1e8449 100%);padding:32px;text-align:center;'>
            <div style='font-size:30px;font-weight:900;color:white;letter-spacing:3px;'>GEP<span style='color:#4fc3a1;'>S</span></div>
            <div style='color:rgba(255,255,255,0.65);font-size:12px;margin-top:5px;letter-spacing:0.5px;'>Gestión Eficiente para Semilleros</div>
            <div style='width:36px;height:3px;background:#4fc3a1;border-radius:2px;margin:14px auto 0;'></div>
          </td>
        </tr>
        <tr>
          <td style='padding:32px;'>
            <p style='font-size:15px;color:#333;margin:0 0 6px;'>Hola <strong style='color:#0d2f5e;'>{nombre}</strong>,</p>
            <p style='font-size:14px;color:#666;margin:0 0 24px;'>El líder de tu semillero ha creado tus credenciales de acceso al sistema GEPS:</p>
            <table width='100%' cellpadding='0' cellspacing='0' style='background:linear-gradient(135deg,#f0f7ff,#e8f5ee);border:1.5px solid #4fc3a1;border-radius:12px;margin-bottom:20px;'>
              <tr><td style='padding:24px 16px;text-align:center;'>
                <div style='font-size:11px;font-weight:700;letter-spacing:2px;color:#1a5276;text-transform:uppercase;margin-bottom:16px;'>Credenciales de acceso</div>
                <table width='100%' cellpadding='0' cellspacing='0'>
                  <tr>
                    <td style='padding:8px 16px;text-align:right;width:45%;font-size:13px;font-weight:700;color:#1a5276;'>Cédula:</td>
                    <td style='padding:8px 16px;text-align:left;font-size:15px;font-weight:900;color:#0d2f5e;letter-spacing:2px;font-family:monospace;'>{cedula}</td>
                  </tr>
                  <tr>
                    <td style='padding:8px 16px;text-align:right;font-size:13px;font-weight:700;color:#1a5276;'>Contraseña:</td>
                    <td style='padding:8px 16px;text-align:left;font-size:15px;font-weight:900;color:#0d2f5e;letter-spacing:2px;font-family:monospace;'>{clave}</td>
                  </tr>
                </table>
              </td></tr>
            </table>
            <table width='100%' cellpadding='0' cellspacing='0' style='background:#fff8e1;border-left:3px solid #f59e0b;border-radius:0 8px 8px 0;margin-bottom:24px;'>
              <tr><td style='padding:10px 14px;font-size:12px;color:#92400e;'>
                ⚠️ Por seguridad cambia tu contraseña al iniciar sesión por primera vez usando la opción <strong>¿Olvidaste tu contraseña?</strong>
              </td></tr>
            </table>
            <p style='font-size:13px;color:#aaa;text-align:center;margin:0;'>Este es un correo automático, no respondas a este mensaje.</p>
          </td>
        </tr>
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

            ServicioCorreo.Enviar(correo, "Credenciales de acceso – GEPS", cuerpo);

            TempData["Exito"] = $"Investigador creado correctamente.";
            return RedirectToAction("Investigadores");
        }

        // ══════════════════════════════════════
        //  INVESTIGADORES — EDITAR
        // ══════════════════════════════════════
        public ActionResult EditarInvestigador(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var investigador = usuarios.Find(
                Builders<Usuario>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (investigador == null) return RedirectToAction("Investigadores");
            return View(investigador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarInvestigador(string id, string nombre, string correo,
            string fechaNacimiento, string genero, string celular, string programa)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(fechaNacimiento) || string.IsNullOrEmpty(genero) || string.IsNullOrEmpty(celular) ||
            string.IsNullOrEmpty(programa))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                var inv = usuarios.Find(
                    Builders<Usuario>.Filter.Eq("_id", ObjectId.Parse(id))
                ).FirstOrDefault();
                return View(inv);
            }

            var filtro = Builders<Usuario>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<Usuario>.Update
                .Set("nombre", nombre)
                .Set("correo", correo)
                .Set("genero", genero)
                .Set("celular", celular)
                .Set("programa", programa)
                .Set("fechaNacimiento", DateTime.Parse(fechaNacimiento));

            usuarios.UpdateOne(filtro, update);

            TempData["Exito"] = "Investigador actualizado correctamente.";
            return RedirectToAction("Investigadores");
        }

        // ══════════════════════════════════════
        //  INVESTIGADORES — CAMBIAR ESTADO
        // ══════════════════════════════════════
        public ActionResult CambiarEstadoInvestigador(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Usuario>.Filter.Eq("_id", ObjectId.Parse(id));
            var investigador = usuarios.Find(filtro).FirstOrDefault();

            if (investigador != null)
            {
                UpdateDefinition<Usuario> update;
                if (investigador.Activo)
                {
                    update = Builders<Usuario>.Update
                        .Set("activo", false);
                }
                else
                {
                    var lider = ObtenerLiderActual();
                    update = Builders<Usuario>.Update
                        .Set("activo", true);
                }

                usuarios.UpdateOne(filtro, update);

                TempData["Exito"] = investigador.Activo
                    ? "Investigador desactivado correctamente."
                    : "Investigador activado correctamente.";
            }

            return RedirectToAction("Investigadores");
        }

        // ══════════════════════════════════════
        //  PROYECTOS — LISTAR
        // ══════════════════════════════════════
        public ActionResult Proyectos()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            if (lider == null || string.IsNullOrEmpty(lider.IdSemillero))
                return RedirectToAction("Index");

            var lista = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).SortByDescending(p => p.FechaInicio).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  PROYECTOS — CREAR
        // ══════════════════════════════════════
        public ActionResult CrearProyecto()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearProyecto(Proyecto proyecto)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(proyecto.Titulo) || string.IsNullOrEmpty(proyecto.Objetivo) || proyecto.DuracionMeses < 1
                || proyecto.DuracionMeses > 12)
            {
                ViewBag.Error = "Todos los campos son obligatorios y la duracion debe estar entre 1 y 12";
                return View(proyecto);
            }

            var lider = ObtenerLiderActual();
            var fechaInicio = DateTime.Now;
            proyecto.IdSemillero = lider.IdSemillero;
            proyecto.FechaInicio = fechaInicio;
            proyecto.FechaFin =fechaInicio.AddMonths(proyecto.DuracionMeses);
            proyecto.Estado = "En ejecución";
            proyecto.Fases = new List<Fase>();

            proyectos.InsertOne(proyecto);

            TempData["Exito"] = "Proyecto creado correctamente.";
            return RedirectToAction("Proyectos");
        }

        // ══════════════════════════════════════
        //  PROYECTOS — EDITAR
        // ══════════════════════════════════════
        public ActionResult EditarProyecto(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");
            return View(proyecto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarProyecto(string id, string titulo, string objetivo)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(titulo) || string.IsNullOrEmpty(objetivo))
            {
                ViewBag.Error = "El título y el objetivo son obligatorios.";
                var proyecto = proyectos.Find(
                    Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id))
                ).FirstOrDefault();
                return View(proyecto);
            }

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<Proyecto>.Update
                .Set("titulo", titulo)
                .Set("objetivo", objetivo);

            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Proyecto actualizado correctamente.";
            return RedirectToAction("Proyectos");
        }

        // ══════════════════════════════════════
        //  PROYECTOS — CAMBIAR ESTADO
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstadoProyecto(string id, string estado)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<Proyecto>.Update.Set("estado", estado);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Estado del proyecto actualizado correctamente.";
            return RedirectToAction("Proyectos");
        }

        // ══════════════════════════════════════
        //  DETALLE PROYECTO
        // ══════════════════════════════════════
        public ActionResult DetalleProyecto(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");
            return View(proyecto);
        }

        // ══════════════════════════════════════
        //  FASES — AGREGAR
        // ══════════════════════════════════════
        public ActionResult AgregarFase(string idProyecto)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");
            ViewBag.IdProyecto = idProyecto;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarFase(string idProyecto, string nombreFase, int duracionMeses)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombreFase) || duracionMeses < 1 || duracionMeses > 12)
            {
                ViewBag.Error = "El nombre de la fase es obligatorio y la duración debe estar entre 1 y 12 meses.";
                ViewBag.IdProyecto = idProyecto;
                return View();
            }

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto))
                    ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");

            int duracionAcumulada = proyecto.Fases.Sum(f => f.DuracionMeses);

            if (duracionAcumulada + duracionMeses > proyecto.DuracionMeses)
            {
                ViewBag.Error = $"La suma de duraciones de las fases ({duracionAcumulada + duracionMeses} meses) no puede superar la duración del proyecto ({proyecto.DuracionMeses} meses).";
                ViewBag.IdProyecto = idProyecto;
                return View();
            }

            var fase = new Fase
            {
                Nombre = nombreFase,
                DuracionMeses = duracionMeses,
                Actividades = new List<Actividad>()
            };

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Push("fases", fase);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Fase agregada correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ══════════════════════════════════════
        //  ACTIVIDADES — AGREGAR
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarActividad(string idProyecto, int indiceFase,
            string nombreActividad, int duracionDias, DateTime fechaEntrega)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var actividad = new Actividad
            {
                Nombre = nombreActividad,
                DuracionDias = duracionDias,
                FechaEntrega = fechaEntrega
            };

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Push(
                $"fases.{indiceFase}.actividades", actividad);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Actividad agregada correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ══════════════════════════════════════
        //  FASES — EDITAR NOMBRE
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarNombreFase(string idProyecto, int indiceFase, string nombreFase)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombreFase))
            {
                TempData["Error"] = "El nombre de la fase es obligatorio.";
                return RedirectToAction("DetalleProyecto", new { id = idProyecto });
            }

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Set($"fases.{indiceFase}.nombre", nombreFase);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Nombre de la fase actualizado correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ══════════════════════════════════════
        //  FASES — ELIMINAR
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarFase(string idProyecto, int indiceFase)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto))
            ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");

            if (indiceFase < 0 || indiceFase >= proyecto.Fases.Count)
                return RedirectToAction("DetalleProyecto", new { id = idProyecto });

            proyecto.Fases.RemoveAt(indiceFase);

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Set("fases", proyecto.Fases);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Fase eliminada correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ══════════════════════════════════════
        //  ACTIVIDADES — EDITAR NOMBRE
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarNombreActividad(string idProyecto, int indiceFase, int indiceActividad, string nombreActividad)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombreActividad))
            {
                TempData["Error"] = "El nombre de la actividad es obligatorio.";
                return RedirectToAction("DetalleProyecto", new { id = idProyecto });
            }

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Set(
                $"fases.{indiceFase}.actividades.{indiceActividad}.nombre", nombreActividad);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Nombre de la actividad actualizado correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ══════════════════════════════════════
        //  ACTIVIDADES — ELIMINAR
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarActividad(string idProyecto, int indiceFase, int indiceActividad)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto))
            ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");

            if (indiceFase < 0 || indiceFase >= proyecto.Fases.Count)
                return RedirectToAction("DetalleProyecto", new { id = idProyecto });

            var fase = proyecto.Fases[indiceFase];

            if (indiceActividad < 0 || indiceActividad >= fase.Actividades.Count)
                return RedirectToAction("DetalleProyecto", new { id = idProyecto });

            fase.Actividades.RemoveAt(indiceActividad);

            var filtro = Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto));
            var update = Builders<Proyecto>.Update.Set($"fases.{indiceFase}.actividades", fase.Actividades);
            proyectos.UpdateOne(filtro, update);

            TempData["Exito"] = "Actividad eliminada correctamente.";
            return RedirectToAction("DetalleProyecto", new { id = idProyecto });
        }

        // ── Calcular estado de una reunión según fecha/hora actual ──
        private string CalcularEstadoReunion(DateTime fecha, string hora)
        {
            if (!TimeSpan.TryParse(hora, out TimeSpan horaInicio))
                return "Programada";

            DateTime inicio = fecha.Date.Add(horaInicio);
            DateTime fin = inicio.AddHours(1);
            DateTime ahora = DateTime.Now;

            if (ahora < inicio) return "Programada";
            if (ahora >= inicio && ahora < fin) return "En ejecución";
            return "Finalizada";
        }

        // ── Validar horario de reunión: L-V, slots de 1h entre 8am y 7pm (última 7pm-8pm) ──
        private bool EsHorarioValidoReunion(DateTime fecha, string hora)
        {
            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                return false;

            if (!TimeSpan.TryParse(hora, out TimeSpan horaInicio))
                return false;

            // Slots válidos: 08:00, 09:00, ..., 19:00 (última que inicia, termina a las 20:00)
            if (horaInicio.Minutes != 0 || horaInicio.Seconds != 0)
                return false;

            if (horaInicio.Hours < 8 || horaInicio.Hours > 19)
                return false;

            return true;
        }

        // ══════════════════════════════════════
        //  REUNIONES — LISTAR
        // ══════════════════════════════════════
        public ActionResult Reuniones()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            var idsProyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList().Select(p => ObjectId.Parse(p.Id)).ToList();

            var lista = reuniones.Find(
                Builders<Reunion>.Filter.In("idProyecto", idsProyectos)
            ).SortByDescending(r => r.Fecha).ToList();

            // Recalcular y persistir estado actualizado
            foreach (var r in lista)
            {
                string estadoActual = CalcularEstadoReunion(r.Fecha, r.Hora);
                if (r.Estado != estadoActual)
                {
                    var filtro = Builders<Reunion>.Filter.Eq("_id", ObjectId.Parse(r.Id));
                    var update = Builders<Reunion>.Update.Set("estado", estadoActual);
                    reuniones.UpdateOne(filtro, update);
                    r.Estado = estadoActual;
                }
            }

            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  REUNIONES — CREAR
        // ══════════════════════════════════════
        public ActionResult CrearReunion()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearReunion(Reunion reunion)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            if (string.IsNullOrEmpty(reunion.Motivo) ||
                string.IsNullOrEmpty(reunion.IdProyecto) ||
                string.IsNullOrEmpty(reunion.Hora) ||
                string.IsNullOrEmpty(reunion.Lugar))
            {
                ViewBag.Error = "Todos los campos obligatorios deben completarse.";
                return View(reunion);
            }

            // Validar horario L-V 8am-7pm en slots de 1h
            if (!EsHorarioValidoReunion(reunion.Fecha, reunion.Hora))
            {
                ViewBag.Error = "Las reuniones solo se pueden programar de lunes a viernes, en horarios de 8:00am a 7:00pm (slots de 1 hora).";
                return View(reunion);
            }

            // Validar que la fecha esté dentro del rango del proyecto
            var proyectoSeleccionado = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(reunion.IdProyecto))
            ).FirstOrDefault();

            if (proyectoSeleccionado == null)
            {
                ViewBag.Error = "El proyecto seleccionado no existe.";
                return View(reunion);
            }

            if (reunion.Fecha.Date < proyectoSeleccionado.FechaInicio.Date ||
                reunion.Fecha.Date > proyectoSeleccionado.FechaFin.Date)
            {
                ViewBag.Error = $"La fecha debe estar entre {proyectoSeleccionado.FechaInicio:dd/MM/yyyy} y {proyectoSeleccionado.FechaFin:dd/MM/yyyy} (rango del proyecto).";
                return View(reunion);
            }

            // Validar que no exista otra reunión en el mismo proyecto, misma fecha y hora
            var conflicto = reuniones.Find(
                Builders<Reunion>.Filter.And(
                    Builders<Reunion>.Filter.Eq("idProyecto", reunion.IdProyecto),
                    Builders<Reunion>.Filter.Eq("fecha", reunion.Fecha.Date),
                    Builders<Reunion>.Filter.Eq("hora", reunion.Hora),
                    Builders<Reunion>.Filter.Eq("activo", true)
                )
            ).FirstOrDefault();

            if (conflicto != null)
            {
                ViewBag.Error = "Ya existe una reunión programada en ese proyecto para esa fecha y hora.";
                return View(reunion);
            }

            reunion.Activo = true;
            reunion.Estado = CalcularEstadoReunion(reunion.Fecha, reunion.Hora);

            reuniones.InsertOne(reunion);

            TempData["Exito"] = "Reunión creada correctamente.";
            return RedirectToAction("Reuniones");
        }

        // ══════════════════════════════════════
        //  REUNIONES — EDITAR
        // ══════════════════════════════════════
        public ActionResult EditarReunion(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var reunion = reuniones.Find(
                Builders<Reunion>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (reunion == null) return RedirectToAction("Reuniones");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            return View(reunion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarReunion(string id, Reunion reunion)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            if (string.IsNullOrEmpty(reunion.Motivo) ||
                string.IsNullOrEmpty(reunion.IdProyecto) ||
                string.IsNullOrEmpty(reunion.Hora) ||
                string.IsNullOrEmpty(reunion.Lugar))
            {
                ViewBag.Error = "Todos los campos obligatorios deben completarse.";
                reunion.Id = id;
                return View(reunion);
            }

            if (!EsHorarioValidoReunion(reunion.Fecha, reunion.Hora))
            {
                ViewBag.Error = "Las reuniones solo se pueden programar de lunes a viernes, en horarios de 8:00am a 7:00pm (slots de 1 hora).";
                reunion.Id = id;
                return View(reunion);
            }

            var proyectoSeleccionado = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(reunion.IdProyecto))
            ).FirstOrDefault();

            if (proyectoSeleccionado == null)
            {
                ViewBag.Error = "El proyecto seleccionado no existe.";
                reunion.Id = id;
                return View(reunion);
            }

            if (reunion.Fecha.Date < proyectoSeleccionado.FechaInicio.Date ||
                reunion.Fecha.Date > proyectoSeleccionado.FechaFin.Date)
            {
                ViewBag.Error = $"La fecha debe estar entre {proyectoSeleccionado.FechaInicio:dd/MM/yyyy} y {proyectoSeleccionado.FechaFin:dd/MM/yyyy} (rango del proyecto).";
                reunion.Id = id;
                return View(reunion);
            }

            // Validar conflicto excluyendo la propia reunión
            var conflicto = reuniones.Find(
                Builders<Reunion>.Filter.And(
                    Builders<Reunion>.Filter.Eq("idProyecto", reunion.IdProyecto),
                    Builders<Reunion>.Filter.Eq("fecha", reunion.Fecha.Date),
                    Builders<Reunion>.Filter.Eq("hora", reunion.Hora),
                    Builders<Reunion>.Filter.Eq("activo", true),
                    Builders<Reunion>.Filter.Ne("_id", ObjectId.Parse(id))
                )
            ).FirstOrDefault();

            if (conflicto != null)
            {
                ViewBag.Error = "Ya existe otra reunión programada en ese proyecto para esa fecha y hora.";
                reunion.Id = id;
                return View(reunion);
            }

            var filtro = Builders<Reunion>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<Reunion>.Update
                .Set("fecha", reunion.Fecha)
                .Set("hora", reunion.Hora)
                .Set("lugar", reunion.Lugar)
                .Set("enlace", reunion.Enlace)
                .Set("motivo", reunion.Motivo)
                .Set("idProyecto", reunion.IdProyecto)
                .Set("estado", CalcularEstadoReunion(reunion.Fecha, reunion.Hora));

            reuniones.UpdateOne(filtro, update);

            TempData["Exito"] = "Reunión actualizada correctamente.";
            return RedirectToAction("Reuniones");
        }

        // ══════════════════════════════════════
        //  REUNIONES — HABILITAR/DESHABILITAR
        // ══════════════════════════════════════
        public ActionResult CambiarEstadoReunion(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Reunion>.Filter.Eq("_id", ObjectId.Parse(id));
            var reunion = reuniones.Find(filtro).FirstOrDefault();

            if (reunion != null)
            {
                var update = Builders<Reunion>.Update.Set("activo", !reunion.Activo);
                reuniones.UpdateOne(filtro, update);

                TempData["Exito"] = reunion.Activo
                        ? "Reunión deshabilitada correctamente."
                        : "Reunión habilitada correctamente.";
            }

            return RedirectToAction("Reuniones");
        }

        // ══════════════════════════════════════
        //  EVENTOS — LISTAR
        // ══════════════════════════════════════
        public ActionResult Eventos()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            var idsProyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList().Select(p => ObjectId.Parse(p.Id)).ToList();

            var lista = eventos.Find(
                Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos)
            ).SortByDescending(e => e.Fecha).ToList();

            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  EVENTOS — CREAR
        // ══════════════════════════════════════
        public ActionResult CrearEvento()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearEvento(Evento evento, string idProyecto)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider2 = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider2.IdSemillero))
            ).ToList();

            if (string.IsNullOrEmpty(evento.Nombre) ||
                string.IsNullOrEmpty(idProyecto) ||
                string.IsNullOrEmpty(evento.Lugar) ||
                string.IsNullOrEmpty(evento.Tipo) ||
                string.IsNullOrEmpty(evento.NombreOrganizador))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                return View(evento);
            }

            var proyectoSeleccionado = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto))
            ).FirstOrDefault();

            if (proyectoSeleccionado == null)
            {
                ViewBag.Error = "El proyecto seleccionado no existe.";
                return View(evento);
            }

            if (evento.Fecha.Date < proyectoSeleccionado.FechaInicio.Date ||
                evento.Fecha.Date > proyectoSeleccionado.FechaFin.Date)
            {
                ViewBag.Error = $"La fecha debe estar entre {proyectoSeleccionado.FechaInicio:dd/MM/yyyy} y {proyectoSeleccionado.FechaFin:dd/MM/yyyy} (rango del proyecto).";
                return View(evento);
            }

            // Validar que no exista otro evento en el mismo proyecto, en la misma fecha
            var conflicto = eventos.Find(
                Builders<Evento>.Filter.And(
                    Builders<Evento>.Filter.AnyEq("proyectos", idProyecto),
                    Builders<Evento>.Filter.Eq("fecha", evento.Fecha.Date),
                    Builders<Evento>.Filter.Eq("activo", true)
                )
            ).FirstOrDefault();

            if (conflicto != null)
            {
                ViewBag.Error = "Ya existe un evento programado en ese proyecto para esa fecha.";
                return View(evento);
            }

            evento.Activo = true;
            evento.Proyectos = new List<string> { idProyecto };
            eventos.InsertOne(evento);

            TempData["Exito"] = "Evento creado correctamente.";
            return RedirectToAction("Eventos");
        }

        // ══════════════════════════════════════
        //  EVENTOS — EDITAR
        // ══════════════════════════════════════
        public ActionResult EditarEvento(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var evento = eventos.Find(
                Builders<Evento>.Filter.Eq("_id", ObjectId.Parse(id))
            ).FirstOrDefault();

            if (evento == null) return RedirectToAction("Eventos");

            var lider = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            return View(evento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarEvento(string id, Evento evento, string idProyecto)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider2 = ObtenerLiderActual();
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider2.IdSemillero))
            ).ToList();

            if (string.IsNullOrEmpty(evento.Nombre) ||
                string.IsNullOrEmpty(idProyecto) ||
                string.IsNullOrEmpty(evento.Lugar) ||
                string.IsNullOrEmpty(evento.Tipo) ||
                string.IsNullOrEmpty(evento.NombreOrganizador))
            {
                ViewBag.Error = "Todos los campos son obligatorios.";
                evento.Id = id;
                return View(evento);
            }

            var proyectoSeleccionado = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(idProyecto))
            ).FirstOrDefault();

            if (proyectoSeleccionado == null)
            {
                ViewBag.Error = "El proyecto seleccionado no existe.";
                evento.Id = id;
                return View(evento);
            }

            if (evento.Fecha.Date < proyectoSeleccionado.FechaInicio.Date ||
                evento.Fecha.Date > proyectoSeleccionado.FechaFin.Date)
            {
                ViewBag.Error = $"La fecha debe estar entre {proyectoSeleccionado.FechaInicio:dd/MM/yyyy} y {proyectoSeleccionado.FechaFin:dd/MM/yyyy} (rango del proyecto).";
                evento.Id = id;
                return View(evento);
            }

            // Validar conflicto excluyendo el propio evento
            var conflicto = eventos.Find(
                Builders<Evento>.Filter.And(
                    Builders<Evento>.Filter.AnyEq("proyectos", idProyecto),
                    Builders<Evento>.Filter.Eq("fecha", evento.Fecha.Date),
                    Builders<Evento>.Filter.Eq("activo", true),
                    Builders<Evento>.Filter.Ne("_id", ObjectId.Parse(id))
                )
            ).FirstOrDefault();

            if (conflicto != null)
            {
                ViewBag.Error = "Ya existe otro evento programado en ese proyecto para esa fecha.";
                evento.Id = id;
                return View(evento);
            }

            var filtro = Builders<Evento>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<Evento>.Update
                .Set("nombre", evento.Nombre)
                .Set("fecha", evento.Fecha)
                .Set("lugar", evento.Lugar)
                .Set("tipo", evento.Tipo)
                .Set("nombreOrganizador", evento.NombreOrganizador)
                .Set("proyectos", new List<string> { idProyecto });

            eventos.UpdateOne(filtro, update);

            TempData["Exito"] = "Evento actualizado correctamente.";
            return RedirectToAction("Eventos");
        }

        // ══════════════════════════════════════
        //  EVENTOS — HABILITAR/DESHABILITAR
        // ══════════════════════════════════════
        public ActionResult CambiarEstadoEvento(string id)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Evento>.Filter.Eq("_id", ObjectId.Parse(id));
            var evento = eventos.Find(filtro).FirstOrDefault();

            if (evento != null)
            {
                var update = Builders<Evento>.Update.Set("activo", !evento.Activo);
                eventos.UpdateOne(filtro, update);

                TempData["Exito"] = evento.Activo
                    ? "Evento deshabilitado correctamente."
                    : "Evento habilitado correctamente.";
            }

            return RedirectToAction("Eventos");
        }

        // ══════════════════════════════════════
        //  REPORTE PDF
        // ══════════════════════════════════════
        public ActionResult Reporte()
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();
            var semillero = semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(lider.IdSemillero))
            ).FirstOrDefault();

            var _proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            var idsProyectos = _proyectos.Select(p => ObjectId.Parse(p.Id)).ToList();

            var _reuniones = reuniones.Find(
                Builders<Reunion>.Filter.In("idProyecto", idsProyectos)
            ).ToList();

            var _eventos = eventos.Find(
                Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos)
            ).ToList();

            var investigadores = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Investigador"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
                )
            ).ToList();

            ViewBag.Semillero = semillero;
            ViewBag.Proyectos = _proyectos;
            ViewBag.Reuniones = _reuniones;
            ViewBag.Eventos = _eventos;
            ViewBag.Investigadores = investigadores;

            return View();
        }

        public ActionResult DescargarReporte(bool descargar = false)
        {
            if (!EsLider()) return RedirectToAction("Index", "Login");

            var lider = ObtenerLiderActual();

            var semillero = semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(lider.IdSemillero))
            ).FirstOrDefault();

            var _proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
            ).ToList();

            var idsProyectos = _proyectos.Select(p => ObjectId.Parse(p.Id)).ToList();

            var _reuniones = reuniones.Find(
                Builders<Reunion>.Filter.In("idProyecto", idsProyectos)
            ).ToList();

            var _eventos = eventos.Find(
                Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos)
            ).ToList();

            var investigadores = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Investigador"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(lider.IdSemillero))
                )
            ).ToList();

            using (var ms = new System.IO.MemoryStream())
            {
                var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 60, 40);
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms);
                document.Open();

                // ── Colores ──
                var colorAzul = new iTextSharp.text.BaseColor(13, 47, 94);
                var colorAzulMedio = new iTextSharp.text.BaseColor(26, 82, 118);
                var colorVerde = new iTextSharp.text.BaseColor(30, 132, 73);
                var colorMenta = new iTextSharp.text.BaseColor(79, 195, 161);
                var colorFilaPar = new iTextSharp.text.BaseColor(245, 249, 255);
                var colorRojo = new iTextSharp.text.BaseColor(192, 57, 43);
                var colorGris = new iTextSharp.text.BaseColor(100, 100, 100);

                // ── Fuentes ──
                var fHeader = iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.WHITE);
                var fTitulo = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD, colorAzul);
                var fKey = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorAzul);
                var fVal = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
                var fSmall = iTextSharp.text.FontFactory.GetFont("Arial", 8, iTextSharp.text.Font.NORMAL, colorGris);
                var fVerde = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorVerde);
                var fRojo = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorRojo);

                // ── ENCABEZADO ──
                var tablaHeader = new iTextSharp.text.pdf.PdfPTable(1);
                tablaHeader.WidthPercentage = 100;
                tablaHeader.SpacingAfter = 0;

                var celdaHeader = new iTextSharp.text.pdf.PdfPCell();
                celdaHeader.BackgroundColor = colorAzul;
                celdaHeader.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaHeader.Padding = 16;
                celdaHeader.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;

                var pTitulo = new iTextSharp.text.Paragraph();
                pTitulo.Add(new iTextSharp.text.Chunk("GEPS", iTextSharp.text.FontFactory.GetFont("Arial", 24, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.WHITE)));
                pTitulo.Add(new iTextSharp.text.Chunk("  |  Gestión Eficiente para Semilleros", iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.NORMAL, colorMenta)));
                pTitulo.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                celdaHeader.AddElement(pTitulo);

                var pSub = new iTextSharp.text.Paragraph($"Reporte del Semillero — {semillero?.Nombre ?? "Sin semillero"}",
                    iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, new iTextSharp.text.BaseColor(200, 220, 240)));
                pSub.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                celdaHeader.AddElement(pSub);

                tablaHeader.AddCell(celdaHeader);
                document.Add(tablaHeader);

                // Barra verde menta
                var tablaBarra = new iTextSharp.text.pdf.PdfPTable(1);
                tablaBarra.WidthPercentage = 100;
                tablaBarra.SpacingAfter = 12;
                var celdaBarra = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(" "));
                celdaBarra.BackgroundColor = colorMenta;
                celdaBarra.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaBarra.FixedHeight = 4;
                tablaBarra.AddCell(celdaBarra);
                document.Add(tablaBarra);

                // Info generación
                var tablaInfo = new iTextSharp.text.pdf.PdfPTable(2);
                tablaInfo.WidthPercentage = 100;
                tablaInfo.SetWidths(new float[] { 50f, 50f });
                tablaInfo.SpacingAfter = 16;
                var cFecha = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", fSmall));
                cFecha.Border = iTextSharp.text.Rectangle.NO_BORDER; cFecha.Padding = 4;
                tablaInfo.AddCell(cFecha);
                var cLider = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"Líder: {Session["Nombre"]}", fSmall));
                cLider.Border = iTextSharp.text.Rectangle.NO_BORDER; cLider.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT; cLider.Padding = 4;
                tablaInfo.AddCell(cLider);
                document.Add(tablaInfo);

                // ── SEMILLERO ──
                AgregarSeccionPDF(document, "Información del Semillero", colorAzul, colorMenta, fHeader);
                if (semillero != null)
                {
                    var t = new iTextSharp.text.pdf.PdfPTable(2);
                    t.WidthPercentage = 100; t.SetWidths(new float[] { 35f, 65f }); t.SpacingAfter = 16;
                    AgregarFilaPDF(t, "Nombre", semillero.Nombre, fKey, fVal, iTextSharp.text.BaseColor.WHITE);
                    AgregarFilaPDF(t, "Línea investigativa", semillero.LineaInvestigativa, fKey, fVal, colorFilaPar);
                    AgregarFilaPDF(t, "Descripción", semillero.Descripcion ?? "—", fKey, fVal, iTextSharp.text.BaseColor.WHITE);
                    AgregarFilaPDF(t, "Fecha creación", semillero.FechaCreacion.ToString("dd/MM/yyyy"), fKey, fVal, colorFilaPar);
                    AgregarFilaPDF(t, "Estado", semillero.Activo ? "Activo" : "Inactivo", fKey, semillero.Activo ? fVerde : fRojo, iTextSharp.text.BaseColor.WHITE);
                    document.Add(t);
                }

                // ── INVESTIGADORES ──
                AgregarSeccionPDF(document, $"Investigadores ({investigadores.Count})", colorAzul, colorMenta, fHeader);
                if (investigadores.Count == 0)
                {
                    document.Add(new iTextSharp.text.Paragraph("No hay investigadores registrados.", fVal) { SpacingAfter = 12 });
                }
                else
                {
                    var t = new iTextSharp.text.pdf.PdfPTable(3);
                    t.WidthPercentage = 100; t.SetWidths(new float[] { 40f, 30f, 30f }); t.SpacingAfter = 16;
                    AgregarHeaderFilaPDF(t, new[] { "Nombre", "Programa", "Estado" }, colorAzulMedio, fHeader);
                    bool alt = false;
                    foreach (var inv in investigadores)
                    {
                        var color = alt ? colorFilaPar : iTextSharp.text.BaseColor.WHITE;
                        AgregarCeldaPDF(t, inv.Nombre, fKey, color);
                        AgregarCeldaPDF(t, inv.Programa, fVal, color);
                        AgregarCeldaPDF(t, inv.Activo ? "Activo" : "Inactivo", inv.Activo ? fVerde : fRojo, color);
                        alt = !alt;
                    }
                    document.Add(t);
                }

                // ── PROYECTOS ──
                AgregarSeccionPDF(document, $"Proyectos ({_proyectos.Count})", colorAzul, colorMenta, fHeader);
                foreach (var p in _proyectos)
                {
                    var tProyecto = new iTextSharp.text.pdf.PdfPTable(2);
                    tProyecto.WidthPercentage = 100; tProyecto.SetWidths(new float[] { 35f, 65f }); tProyecto.SpacingAfter = 12;

                    var cTitulo = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(p.Titulo.ToUpper(), fHeader));
                    cTitulo.Colspan = 2; cTitulo.BackgroundColor = colorAzulMedio;
                    cTitulo.Border = iTextSharp.text.Rectangle.NO_BORDER; cTitulo.Padding = 8;
                    tProyecto.AddCell(cTitulo);

                    AgregarFilaPDF(tProyecto, "Objetivo", p.Objetivo, fKey, fVal, iTextSharp.text.BaseColor.WHITE);
                    AgregarFilaPDF(tProyecto, "Duración", $"{p.DuracionMeses} mes(es)", fKey, fVal, colorFilaPar);
                    AgregarFilaPDF(tProyecto, "Fecha inicio", p.FechaInicio.ToString("dd/MM/yyyy"), fKey, fVal, iTextSharp.text.BaseColor.WHITE);
                    AgregarFilaPDF(tProyecto, "Estado", p.Estado, fKey, p.Estado == "En ejecución" ? fVerde : fRojo, colorFilaPar);
                    AgregarFilaPDF(tProyecto, "Fases", $"{p.Fases.Count} fase(s)", fKey, fVal, iTextSharp.text.BaseColor.WHITE);
                    document.Add(tProyecto);
                }

                // ── REUNIONES ──
                AgregarSeccionPDF(document, $"Reuniones ({_reuniones.Count})", colorAzul, colorMenta, fHeader);
                if (_reuniones.Count == 0)
                {
                    document.Add(new iTextSharp.text.Paragraph("No hay reuniones registradas.", fVal) { SpacingAfter = 12 });
                }
                else
                {
                    var t = new iTextSharp.text.pdf.PdfPTable(4);
                    t.WidthPercentage = 100; t.SetWidths(new float[] { 35f, 20f, 20f, 25f }); t.SpacingAfter = 16;
                    AgregarHeaderFilaPDF(t, new[] { "Motivo", "Fecha", "Hora", "Lugar" }, colorAzulMedio, fHeader);
                    bool alt = false;
                    foreach (var r in _reuniones)
                    {
                        var color = alt ? colorFilaPar : iTextSharp.text.BaseColor.WHITE;
                        AgregarCeldaPDF(t, r.Motivo, fVal, color);
                        AgregarCeldaPDF(t, r.Fecha.ToString("dd/MM/yyyy"), fVal, color);
                        AgregarCeldaPDF(t, r.Hora, fVal, color);
                        AgregarCeldaPDF(t, r.Lugar, fVal, color);
                        alt = !alt;
                    }
                    document.Add(t);
                }

                // ── EVENTOS ──
                AgregarSeccionPDF(document, $"Eventos ({_eventos.Count})", colorAzul, colorMenta, fHeader);
                if (_eventos.Count == 0)
                {
                    document.Add(new iTextSharp.text.Paragraph("No hay eventos registrados.", fVal) { SpacingAfter = 12 });
                }
                else
                {
                    var t = new iTextSharp.text.pdf.PdfPTable(4);
                    t.WidthPercentage = 100; t.SetWidths(new float[] { 35f, 20f, 20f, 25f }); t.SpacingAfter = 16;
                    AgregarHeaderFilaPDF(t, new[] { "Nombre", "Fecha", "Tipo", "Lugar" }, colorAzulMedio, fHeader);
                    bool alt = false;
                    foreach (var ev in _eventos)
                    {
                        var color = alt ? colorFilaPar : iTextSharp.text.BaseColor.WHITE;
                        AgregarCeldaPDF(t, ev.Nombre, fVal, color);
                        AgregarCeldaPDF(t, ev.Fecha.ToString("dd/MM/yyyy"), fVal, color);
                        AgregarCeldaPDF(t, ev.Tipo, fVal, color);
                        AgregarCeldaPDF(t, ev.Lugar, fVal, color);
                        alt = !alt;
                    }
                    document.Add(t);
                }

                // ── PIE ──
                var tablaPie = new iTextSharp.text.pdf.PdfPTable(1);
                tablaPie.WidthPercentage = 100; tablaPie.SpacingBefore = 10;
                var cPie = new iTextSharp.text.pdf.PdfPCell();
                cPie.BackgroundColor = colorAzul; cPie.Border = iTextSharp.text.Rectangle.NO_BORDER; cPie.Padding = 10;
                cPie.AddElement(new iTextSharp.text.Paragraph(
                    $"GEPS – Reporte generado automáticamente  |  {DateTime.Now:dd/MM/yyyy}",
                    iTextSharp.text.FontFactory.GetFont("Arial", 8, iTextSharp.text.Font.NORMAL, new iTextSharp.text.BaseColor(200, 220, 240))
                )
                { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                tablaPie.AddCell(cPie);
                document.Add(tablaPie);

                document.Close();

                if (descargar)
                {
                    return File(ms.ToArray(), "application/pdf",
                        $"Reporte_{semillero?.Nombre}_{DateTime.Now:yyyyMMdd}.pdf");
                }
                else
                {
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("Content-Disposition", "inline; filename=Reporte_Semillero.pdf");
                    Response.BinaryWrite(ms.ToArray());
                    Response.End();
                    return null;
                }
            }
        }

        // ── Métodos auxiliares PDF ──
        private void AgregarSeccionPDF(iTextSharp.text.Document doc, string titulo,
            iTextSharp.text.BaseColor colorFondo, iTextSharp.text.BaseColor colorLinea,
            iTextSharp.text.Font fuente)
        {
            var t = new iTextSharp.text.pdf.PdfPTable(1);
            t.WidthPercentage = 100; t.SpacingBefore = 12; t.SpacingAfter = 8;
            var c = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(titulo, fuente));
            c.BackgroundColor = colorFondo; c.Border = iTextSharp.text.Rectangle.NO_BORDER; c.Padding = 8;
            t.AddCell(c);
            var cLinea = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(" "));
            cLinea.BackgroundColor = colorLinea; cLinea.Border = iTextSharp.text.Rectangle.NO_BORDER; cLinea.FixedHeight = 3;
            t.AddCell(cLinea);
            doc.Add(t);
        }

        private void AgregarFilaPDF(iTextSharp.text.pdf.PdfPTable tabla, string key, string val,
            iTextSharp.text.Font fKey, iTextSharp.text.Font fVal, iTextSharp.text.BaseColor color)
        {
            var border = iTextSharp.text.Rectangle.BOTTOM_BORDER;
            var borderColor = new iTextSharp.text.BaseColor(220, 220, 220);
            var ck = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(key, fKey));
            ck.BackgroundColor = color; ck.Padding = 7; ck.Border = border; ck.BorderColor = borderColor;
            tabla.AddCell(ck);
            var cv = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(val ?? "—", fVal));
            cv.BackgroundColor = color; cv.Padding = 7; cv.Border = border; cv.BorderColor = borderColor;
            tabla.AddCell(cv);
        }

        private void AgregarHeaderFilaPDF(iTextSharp.text.pdf.PdfPTable tabla, string[] headers,
            iTextSharp.text.BaseColor color, iTextSharp.text.Font fuente)
        {
            foreach (var h in headers)
            {
                var c = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(h, fuente));
                c.BackgroundColor = color; c.Border = iTextSharp.text.Rectangle.NO_BORDER; c.Padding = 7;
                tabla.AddCell(c);
            }
        }

        private void AgregarCeldaPDF(iTextSharp.text.pdf.PdfPTable tabla, string val,
            iTextSharp.text.Font fuente, iTextSharp.text.BaseColor color)
        {
            var c = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(val ?? "—", fuente));
            c.BackgroundColor = color; c.Padding = 7;
            c.Border = iTextSharp.text.Rectangle.BOTTOM_BORDER;
            c.BorderColor = new iTextSharp.text.BaseColor(220, 220, 220);
            tabla.AddCell(c);
        }

        [HttpPost]
        public async Task<JsonResult> SugerirObjetivoProyecto(string titulo)
        {
            if (!EsLider()) return Json(new { error = "No autorizado" });

            if (string.IsNullOrEmpty(titulo))
                return Json(new { error = "El título es requerido." });

            string prompt = $@"Genera un objetivo general profesional y conciso (máximo 3 líneas) para un semillero de investigación  con este título:'{titulo}' 
               Responde SOLO con el texto del objetivo, sin comillas ni explicaciones adicionales.";

            try
            {
                string objetivo = await ServicioGemini.GenerarTexto(prompt);
                return Json(new { objetivo });
            }
            catch (Exception ex)
            {
                return Json(new { error = "No se pudo generar el objetivo: " + ex.Message });
            }
        }
    }
}