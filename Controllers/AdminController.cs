using GEPS.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
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

        //  PANEL PRINCIPAL
        public ActionResult Index()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            ViewBag.TotalSemilleros = _semilleros.CountDocuments(Builders<Semillero>.Filter.Empty);
            ViewBag.SemillerosActivos = _semilleros.CountDocuments(Builders<Semillero>.Filter.Eq("activo", true));
            ViewBag.TotalLideres = _usuarios.CountDocuments(Builders<Usuario>.Filter.Eq("rol", "Lider"));

            return View();
        }

        //  SEMILLEROS — LISTAR
        public ActionResult Semilleros()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var lista = _semilleros.Find(Builders<Semillero>.Filter.Empty)
                                   .SortByDescending(s => s.FechaCreacion)
                                   .ToList();
            return View(lista);
        }

        //  SEMILLEROS — CREAR
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

        //  SEMILLEROS — EDITAR
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

        //  SEMILLEROS — HABILITAR / DESHABILITAR
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


        //  LÍDERES — LISTAR
        public ActionResult Lideres()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var lista = _usuarios.Find(
                Builders<Usuario>.Filter.Eq("rol", "Lider")
            ).ToList();

            // Solo semilleros activos SIN líder asignado
            var todosLosSemilleros = _semilleros.Find(
                Builders<Semillero>.Filter.Eq("activo", true)
            ).ToList();

            // Obtener IDs de semilleros que ya tienen líder
            var semillerosConLider = _usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Lider"),
                    Builders<Usuario>.Filter.Ne("idSemillero", BsonNull.Value),
                    Builders<Usuario>.Filter.Ne("idSemillero", "")
                )
            ).ToList().Select(l => l.IdSemillero).ToList();

            // Filtrar semilleros disponibles — activos y sin líder
            var semillerosDisponibles = todosLosSemilleros
                .Where(s => !semillerosConLider.Contains(s.Id))
                .ToList();

            ViewBag.Semilleros = todosLosSemilleros; // para mostrar nombre en tabla
            ViewBag.SemillerosDisponibles = semillerosDisponibles; // para el dropdown del modal

            return View(lista);
        }

        //  LÍDERES — CREAR CREDENCIALES
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

            // Verificar que el correo no esté registrado
            var correoExiste = _usuarios.Find(
                Builders<Usuario>.Filter.Eq("correo", correo)
            ).FirstOrDefault();

            if (correoExiste != null)
            {
                ViewBag.Error = "Ya existe un usuario registrado con ese correo electrónico.";
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
            string cuerpo = $@"<!DOCTYPE html>
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
            <p style='font-size:15px;color:#333;margin:0 0 6px;'>Hola <strong style='color:#0d2f5e;'>{nombre}</strong>,</p>
            <p style='font-size:14px;color:#666;margin:0 0 24px;'>El administrador ha creado tus credenciales de acceso al sistema GEPS. A continuación encontrarás tus datos de ingreso:</p>
            <!-- CREDENCIALES -->
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
            <!-- RECOMENDACION -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background:#fff8e1;border-left:3px solid #f59e0b;border-radius:0 8px 8px 0;margin-bottom:24px;'>
              <tr><td style='padding:10px 14px;font-size:12px;color:#92400e;'>
                ⚠️ Por seguridad cambia tu contraseña al iniciar sesión por primera vez usando la opción <strong>¿Olvidaste tu contraseña?</strong>
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


            ServicioCorreo.Enviar(correo, "Credenciales de acceso – GEPS", cuerpo);

            TempData["Exito"] = $"Líder creado correctamente. Clave asignada: {clave}";
            return RedirectToAction("Lideres");
        }

        //  LÍDERES — ASIGNAR A SEMILLERO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarLider(string idLider, string idSemillero)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            // Verificar si el semillero ya tiene un líder asignado
            var liderExistente = _usuarios.Find(
                Builders<Usuario>.Filter.And(
                    Builders<Usuario>.Filter.Eq("rol", "Lider"),
                    Builders<Usuario>.Filter.Eq("idSemillero", idSemillero),
                    Builders<Usuario>.Filter.Ne("_id", MongoDB.Bson.ObjectId.Parse(idLider))
                )
            ).FirstOrDefault();

            if (liderExistente != null)
            {
                TempData["Error"] = $"El semillero ya tiene asignado al líder {liderExistente.Nombre}. Desactívalo primero antes de asignar otro.";
                return RedirectToAction("Lideres");
            }

            // Asignar el nuevo líder
            var filtroUsuario = Builders<Usuario>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(idLider));
            var updateUsuario = Builders<Usuario>.Update.Set("idSemillero", idSemillero);
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

        //  REPORTE PDF
        public ActionResult Reporte()
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var semilleros = _semilleros.Find(
                Builders<Semillero>.Filter.Empty
            ).SortByDescending(s => s.FechaCreacion).ToList();

            return View(semilleros);
        }

        public ActionResult DescargarReporte(string idSemillero, bool descargar = false)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            List<Semillero> semilleros;

            if (!string.IsNullOrEmpty(idSemillero))
            {
                semilleros = _semilleros.Find(
                    Builders<Semillero>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(idSemillero))
                ).ToList();
            }
            else
            {
                semilleros = _semilleros.Find(
                    Builders<Semillero>.Filter.Empty
                ).SortByDescending(s => s.FechaCreacion).ToList();
            }

            using (var ms = new System.IO.MemoryStream())
            {
                var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 60, 40);
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms);
                document.Open();

                // ── Colores del aplicativo ──
                var colorAzulOscuro = new iTextSharp.text.BaseColor(13, 47, 94);
                var colorAzulMedio = new iTextSharp.text.BaseColor(26, 82, 118);
                var colorVerde = new iTextSharp.text.BaseColor(30, 132, 73);
                var colorVerdeMenta = new iTextSharp.text.BaseColor(79, 195, 161);
                var colorGrisClaro = new iTextSharp.text.BaseColor(240, 244, 248);
                var colorFilaPar = new iTextSharp.text.BaseColor(245, 249, 255);
                var colorRojo = new iTextSharp.text.BaseColor(192, 57, 43);
                var colorGrisTexto = new iTextSharp.text.BaseColor(100, 100, 100);
                var colorGrisPie = new iTextSharp.text.BaseColor(150, 150, 150);

                // ── Fuentes ──
                var fuenteTitulo = iTextSharp.text.FontFactory.GetFont("Arial", 20, iTextSharp.text.Font.BOLD, colorAzulOscuro);
                var fuenteSubtitulo = iTextSharp.text.FontFactory.GetFont("Arial", 11, iTextSharp.text.Font.NORMAL, colorAzulMedio);
                var fuenteInfo = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.NORMAL, colorGrisTexto);
                var fuenteHeader = iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.WHITE);
                var fuenteKey = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorAzulOscuro);
                var fuenteVal = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
                var fuentePie = iTextSharp.text.FontFactory.GetFont("Arial", 8, iTextSharp.text.Font.ITALIC, colorGrisPie);
                var fuenteActivo = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorVerde);
                var fuenteInactivo = iTextSharp.text.FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.BOLD, colorRojo);

                // ── ENCABEZADO ──
                var tablaHeader = new iTextSharp.text.pdf.PdfPTable(1);
                tablaHeader.WidthPercentage = 100;
                tablaHeader.SpacingAfter = 0;

                var celdaHeader = new iTextSharp.text.pdf.PdfPCell();
                celdaHeader.BackgroundColor = colorAzulOscuro;
                celdaHeader.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaHeader.Padding = 16;
                celdaHeader.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;

                var pTitulo = new iTextSharp.text.Paragraph();
                pTitulo.Add(new iTextSharp.text.Chunk("GEPS", iTextSharp.text.FontFactory.GetFont("Arial", 24, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.WHITE)));
                pTitulo.Add(new iTextSharp.text.Chunk("  |  Gestión Eficiente para Semilleros", iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.NORMAL, colorVerdeMenta)));
                pTitulo.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                celdaHeader.AddElement(pTitulo);

                var pSub = new iTextSharp.text.Paragraph("Reporte de Semilleros de Investigación", iTextSharp.text.FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, new iTextSharp.text.BaseColor(200, 220, 240)));
                pSub.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                celdaHeader.AddElement(pSub);

                tablaHeader.AddCell(celdaHeader);
                document.Add(tablaHeader);

                // Barra verde menta decorativa
                var tablaBarra = new iTextSharp.text.pdf.PdfPTable(1);
                tablaBarra.WidthPercentage = 100;
                tablaBarra.SpacingAfter = 12;
                var celdaBarra = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(" "));
                celdaBarra.BackgroundColor = colorVerdeMenta;
                celdaBarra.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaBarra.FixedHeight = 4;
                tablaBarra.AddCell(celdaBarra);
                document.Add(tablaBarra);

                // Info del reporte
                var tablaInfo = new iTextSharp.text.pdf.PdfPTable(2);
                tablaInfo.WidthPercentage = 100;
                tablaInfo.SetWidths(new float[] { 50f, 50f });
                tablaInfo.SpacingAfter = 16;

                var celdaFecha = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", fuenteInfo));
                celdaFecha.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaFecha.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
                celdaFecha.Padding = 4;
                tablaInfo.AddCell(celdaFecha);

                var celdaAdmin = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"Administrador: {Session["Nombre"]}", fuenteInfo));
                celdaAdmin.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaAdmin.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
                celdaAdmin.Padding = 4;
                tablaInfo.AddCell(celdaAdmin);

                document.Add(tablaInfo);

                if (semilleros.Count == 0)
                {
                    document.Add(new iTextSharp.text.Paragraph("No se encontraron semilleros.", fuenteVal));
                }
                else
                {
                    foreach (var s in semilleros)
                    {
                        var tabla = new iTextSharp.text.pdf.PdfPTable(2);
                        tabla.WidthPercentage = 100;
                        tabla.SetWidths(new float[] { 35f, 65f });
                        tabla.SpacingAfter = 20;

                        // Título del semillero
                        var celdaTitulo = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(s.Nombre.ToUpper(), fuenteHeader));
                        celdaTitulo.Colspan = 2;
                        celdaTitulo.BackgroundColor = colorAzulOscuro;
                        celdaTitulo.Padding = 10;
                        celdaTitulo.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
                        celdaTitulo.Border = iTextSharp.text.Rectangle.NO_BORDER;
                        tabla.AddCell(celdaTitulo);

                        // Sublínea verde menta
                        var celdaLinea = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(" "));
                        celdaLinea.Colspan = 2;
                        celdaLinea.BackgroundColor = colorVerdeMenta;
                        celdaLinea.Border = iTextSharp.text.Rectangle.NO_BORDER;
                        celdaLinea.FixedHeight = 3;
                        tabla.AddCell(celdaLinea);

                        // Buscar líder
                        var lider = _usuarios.Find(
                            Builders<Usuario>.Filter.And(
                                Builders<Usuario>.Filter.Eq("rol", "Lider"),
                                Builders<Usuario>.Filter.Eq("idSemillero", s.Id)
                            )
                        ).FirstOrDefault();

                        // Contar proyectos
                        var totalProyectos = 0;
                        try
                        {
                            var dbProyectos = ConexionMongo.ObtenerDB()
                                .GetCollection<MongoDB.Bson.BsonDocument>("Proyectos");
                            totalProyectos = (int)dbProyectos.CountDocuments(
                                Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("idSemillero", s.Id)
                            );
                        }
                        catch { }

                        var campos = new List<(string, string)>
                {
                    ("Línea investigativa", s.LineaInvestigativa ?? "—"),
                    ("Descripción",         s.Descripcion ?? "—"),
                    ("Fecha de creación",   s.FechaCreacion.ToString("dd/MM/yyyy")),
                    ("Líder asignado",      lider != null ? $"{lider.Nombre}  |  Cédula: {lider.Cedula}" : "Sin asignar"),
                    ("Total de proyectos",  totalProyectos.ToString()),
                    ("Estado",              s.Activo ? "Activo" : "Inactivo"),
                };

                        bool fila = false;
                        foreach (var (key, val) in campos)
                        {
                            var colorFila = fila ? colorFilaPar : iTextSharp.text.BaseColor.WHITE;

                            var celdaKey = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(key, fuenteKey));
                            celdaKey.BackgroundColor = colorFila;
                            celdaKey.Padding = 8;
                            celdaKey.Border = iTextSharp.text.Rectangle.BOTTOM_BORDER;
                            celdaKey.BorderColor = new iTextSharp.text.BaseColor(220, 220, 220);
                            tabla.AddCell(celdaKey);

                            iTextSharp.text.pdf.PdfPCell celdaVal;
                            if (key == "Estado")
                            {
                                var fEstado = s.Activo ? fuenteActivo : fuenteInactivo;
                                celdaVal = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(val, fEstado));
                            }
                            else
                            {
                                celdaVal = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(val, fuenteVal));
                            }
                            celdaVal.BackgroundColor = colorFila;
                            celdaVal.Padding = 8;
                            celdaVal.Border = iTextSharp.text.Rectangle.BOTTOM_BORDER;
                            celdaVal.BorderColor = new iTextSharp.text.BaseColor(220, 220, 220);
                            tabla.AddCell(celdaVal);

                            fila = !fila;
                        }

                        document.Add(tabla);
                    }
                }

                // ── PIE DE PÁGINA ──
                var tablaPie = new iTextSharp.text.pdf.PdfPTable(1);
                tablaPie.WidthPercentage = 100;
                tablaPie.SpacingBefore = 10;

                var celdaPie = new iTextSharp.text.pdf.PdfPCell();
                celdaPie.BackgroundColor = colorAzulOscuro;
                celdaPie.Border = iTextSharp.text.Rectangle.NO_BORDER;
                celdaPie.Padding = 10;
                celdaPie.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                celdaPie.AddElement(new iTextSharp.text.Paragraph(
                    $"GEPS – Reporte generado automáticamente  |  Total semilleros: {semilleros.Count}  |  {DateTime.Now:dd/MM/yyyy}",
                    iTextSharp.text.FontFactory.GetFont("Arial", 8, iTextSharp.text.Font.NORMAL,
                        new iTextSharp.text.BaseColor(200, 220, 240))
                )
                { Alignment = iTextSharp.text.Element.ALIGN_CENTER });
                tablaPie.AddCell(celdaPie);
                document.Add(tablaPie);

                document.Close();

                if (descargar)
                {
                    return File(ms.ToArray(), "application/pdf",
                        $"Reporte_Semilleros_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
                }
                else
                {
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("Content-Disposition", "inline; filename=Reporte_Semilleros.pdf");
                    Response.BinaryWrite(ms.ToArray());
                    Response.End();
                    return null;
                }
            }
        }

        // EDITAR LÍDER
        public ActionResult EditarLider(string id)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var lider = _usuarios.Find(
                Builders<Usuario>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id))
            ).FirstOrDefault();

            if (lider == null) return RedirectToAction("Lideres");
            return View(lider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarLider(string id, string nombre, string correo)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "El nombre y el correo son obligatorios.";
                var lider = _usuarios.Find(
                    Builders<Usuario>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id))
                ).FirstOrDefault();
                return View(lider);
            }

            var filtro = Builders<Usuario>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
            var update = Builders<Usuario>.Update
                .Set("nombre", nombre)
                .Set("correo", correo);

            _usuarios.UpdateOne(filtro, update);

            TempData["Exito"] = "Líder actualizado correctamente.";
            return RedirectToAction("Lideres");
        }

        // DESACTIVAR LIDER
        public ActionResult CambiarEstadoLider(string id)
        {
            if (!EsAdmin()) return RedirectToAction("Index", "Login");

            var filtro = Builders<Usuario>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
            var lider = _usuarios.Find(filtro).FirstOrDefault();

            if (lider != null)
            {
                UpdateDefinition<Usuario> update;

                if (lider.Activo)
                {
                    // Al desactivar — quitar el semillero asignado
                    update = Builders<Usuario>.Update
                        .Set("activo", false)
                        .Set("idSemillero", BsonNull.Value);
                }
                else
                {
                    // Al activar — solo cambiar estado
                    update = Builders<Usuario>.Update
                        .Set("activo", true);
                }

                _usuarios.UpdateOne(filtro, update);

                TempData["Exito"] = lider.Activo
                    ? "Líder desactivado correctamente. El semillero quedó disponible."
                    : "Líder activado correctamente.";
            }

            return RedirectToAction("Lideres");
        }
    }
}

        
    

    


