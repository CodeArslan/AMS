using AMS.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class EmployeeController : Controller
    {
        // GET: Employee
        private ApplicationDbContext _dbContext;
        public EmployeeController()
        {
            _dbContext = new ApplicationDbContext();
        }
        public ActionResult Index()
        {
            var deparmentlist = _dbContext.Departments.ToList();
            var viewModel = new RegisterViewModel
            {
                Department = deparmentlist,
            };
            return View(viewModel);
        }
        public async Task<ActionResult> GetEmployeeData()
        {
            var empList = await _dbContext.Users.Include(c=>c.Department).AsNoTracking().ToListAsync();
            return Json(empList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Delete(string id)
        {
            var userinDb = _dbContext.Users.Find(id);
            if (userinDb == null)
            {
                return Json(new { success = false, message = "Employee Record Doesnot Exist" });
            }
            else
            {
                _dbContext.Users.Remove(userinDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Employee Successfully Deleted" });

            }
        }
        

    }
}