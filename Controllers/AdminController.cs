using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GEPS.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        [Authorize]
        public ActionResult Index()
        {
            if (Session["Rol"]?.ToString() != "Administrador")
                return RedirectToAction("Index", "Login");

            return View();
        }
    }
}