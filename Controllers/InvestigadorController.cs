using GEPS.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace GEPS.Controllers
{
    [Authorize]
    public class InvestigadorController : Controller
    {
        private IMongoCollection<Usuario> usuarios;
        private IMongoCollection<Semillero> semilleros;
        private IMongoCollection<Proyecto> proyectos;
        private IMongoCollection<Reunion> reuniones;
        private IMongoCollection<Evento> eventos;

        public InvestigadorController()
        {
            var db = ConexionMongo.ObtenerDB();
            usuarios = db.GetCollection<Usuario>("Usuarios");
            semilleros = db.GetCollection<Semillero>("Semilleros");
            proyectos = db.GetCollection<Proyecto>("Proyectos");
            reuniones = db.GetCollection<Reunion>("Reuniones");
            eventos = db.GetCollection<Evento>("Eventos");
        }

        // ── Verificar rol ──
        private bool EsInvestigador()
        {
            return Session["Rol"]?.ToString() == "Investigador";
        }

        // ══════════════════════════════════════
        //  PANEL PRINCIPAL
        // ══════════════════════════════════════
        public ActionResult Index()
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();

            if (string.IsNullOrEmpty(idSemillero))
            {
                ViewBag.SinSemillero = true;
                return View();
            }

            var semillero = semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(idSemillero))
            ).FirstOrDefault();

            var _proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero))
            ).ToList();

            var idsProyectos = _proyectos.Select(p => ObjectId.Parse(p.Id)).ToList();

            var totalReuniones = reuniones.CountDocuments(
                Builders<Reunion>.Filter.And(
                    Builders<Reunion>.Filter.In("idProyecto", idsProyectos),
                    Builders<Reunion>.Filter.Eq("activo", true)
                )
            );

            var totalEventos = eventos.CountDocuments(
                Builders<Evento>.Filter.And(
                    Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos),
                    Builders<Evento>.Filter.Eq("activo", true)
                )
            );

            var lider = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Lider"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero)),
                    Builders<Usuario>.Filter.Eq("activo", true)
                )
            ).FirstOrDefault();

            ViewBag.Semillero = semillero;
            ViewBag.TotalProyectos = _proyectos.Count;
            ViewBag.ProyectosActivos = _proyectos.Count(p => p.Estado == "En ejecución");
            ViewBag.TotalReuniones = totalReuniones;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.Lider = lider;

            return View();
        }

        // ══════════════════════════════════════
        //  SEMILLERO — VER DETALLE
        // ══════════════════════════════════════
        public ActionResult Semillero()
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();
            if (string.IsNullOrEmpty(idSemillero)) return RedirectToAction("Index");

            var semillero = semilleros.Find(
                Builders<Semillero>.Filter.Eq("_id", ObjectId.Parse(idSemillero))
            ).FirstOrDefault();

            if (semillero == null) return RedirectToAction("Index");

            var lider = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Lider"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero)),
                    Builders<Usuario>.Filter.Eq("activo", true)
                )
            ).FirstOrDefault();

            var compañeros = usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Investigador"),
                    Builders<Usuario>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero)),
                    Builders<Usuario>.Filter.Eq("activo", true)
                )
            ).ToList();

            ViewBag.Lider = lider;
            ViewBag.Compañeros = compañeros;

            return View(semillero);
        }

        // ══════════════════════════════════════
        //  PROYECTOS — LISTAR (solo lectura)
        // ══════════════════════════════════════
        public ActionResult Proyectos()
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();
            if (string.IsNullOrEmpty(idSemillero)) return RedirectToAction("Index");

            var lista = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero))
            ).SortByDescending(p => p.FechaInicio).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  PROYECTOS — VER DETALLE
        // ══════════════════════════════════════
        public ActionResult DetalleProyecto(string id)
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();

            var proyecto = proyectos.Find(
                Builders<Proyecto>.Filter.And(
                    Builders<Proyecto>.Filter.Eq("_id", ObjectId.Parse(id)),
                    Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero)) // seguridad: solo su semillero
                )
            ).FirstOrDefault();

            if (proyecto == null) return RedirectToAction("Proyectos");

            return View(proyecto);
        }

        // ══════════════════════════════════════
        //  REUNIONES — LISTAR (solo lectura)
        // ══════════════════════════════════════
        public ActionResult Reuniones()
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();
            if (string.IsNullOrEmpty(idSemillero)) return RedirectToAction("Index");

            var idsProyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero))
            ).ToList().Select(p => ObjectId.Parse(p.Id)).ToList();

            var lista = reuniones.Find(
                Builders<Reunion>.Filter.And(
                    Builders<Reunion>.Filter.In("idProyecto", idsProyectos),
                    Builders<Reunion>.Filter.Eq("activo", true)
                )
            ).SortByDescending(r => r.Fecha).ToList();

            // Para mostrar el nombre del proyecto en la vista
            ViewBag.Proyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero))
            ).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  EVENTOS — LISTAR (solo lectura)
        // ══════════════════════════════════════
        public ActionResult Eventos()
        {
            if (!EsInvestigador()) return RedirectToAction("Index", "Login");

            string idSemillero = Session["IdSemillero"]?.ToString();
            if (string.IsNullOrEmpty(idSemillero)) return RedirectToAction("Index");

            var idsProyectos = proyectos.Find(
                Builders<Proyecto>.Filter.Eq("idSemillero", ObjectId.Parse(idSemillero))
            ).ToList().Select(p => ObjectId.Parse(p.Id)).ToList();

            var lista = eventos.Find(
                Builders<Evento>.Filter.And(
                    Builders<Evento>.Filter.AnyIn("proyectos", idsProyectos),
                    Builders<Evento>.Filter.Eq("activo", true)
                )
            ).SortByDescending(e => e.Fecha).ToList();

            return View(lista);
        }

        // ══════════════════════════════════════
        //  CERRAR SESIÓN
        // ══════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Salir()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
    }
}