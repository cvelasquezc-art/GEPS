using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GEPS.Controllers
{
    public class InvestigadorController : Controller
    {
        // GET: Investigador
        public ActionResult Index()
        {
            if (Session["Rol"]?.ToString() != "Investigador")
                return RedirectToAction("Index", "Login");

            return View();
        }
    }
}