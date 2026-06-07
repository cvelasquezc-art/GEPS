using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GEPS.Controllers
{
    public class LiderController : Controller
    {
        // GET: Lider
        public ActionResult Index()
        {
            if (Session["Rol"]?.ToString() != "Lider")
                return RedirectToAction("Index", "Login");

            return View();
        }
    }
}
