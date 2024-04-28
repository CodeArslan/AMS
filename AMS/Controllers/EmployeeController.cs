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
using System.IO.Ports;
using System.Threading;
using System.Web.Services.Description;

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
            var empList = await _dbContext.Users
                .Include(u => u.Department)
                .Include(u => u.Card) 
                .AsNoTracking()
                .ToListAsync(); return Json(empList, JsonRequestBehavior.AllowGet);
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

        [HttpGet]
        public ActionResult GetEmployeesForMeeting(List<int> departmentIds)
        {
            try
            {
                var employees = _dbContext.Users
                    .Where(e => departmentIds.Contains(e.DepartmentId))
                    .ToList();

                var departmentsWithEmployees = employees.Select(e => e.DepartmentId).Distinct();

                if (departmentsWithEmployees.Count() == departmentIds.Count())
                {
                    return Json(new { success = true, data = employees }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var departmentsWithoutEmployees = departmentIds.Except(departmentsWithEmployees);

                    // Fetch department names corresponding to department IDs
                    var departmentNames = _dbContext.Departments
                        .Where(d => departmentsWithoutEmployees.Contains(d.Id))
                        .Select(d => d.deptName)
                        .ToList();

                    if (employees.Any())
                    {
                        // If some departments have employees, return employees for those departments along with message
                        return Json(new { success = true, data = employees, message = $"Employees not found for the following departments: {string.Join(", ", departmentNames)}" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        // If no departments have employees, return error message with department names
                        return Json(new { success = false, message = $"No employees found for the following departments: {string.Join(", ", departmentNames)}" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }






    }
}