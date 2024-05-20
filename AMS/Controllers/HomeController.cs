using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                // If user is admin, redirect to admin dashboard
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                return RedirectToAction("EmployeeAttendance", "Attendance");
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Dashboard()
        {
            return View();
        }
    }
}