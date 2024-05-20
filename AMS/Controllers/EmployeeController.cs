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
            var empList = await _dbContext.Users.Where(e=>e.isLabour==false)
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
                    .Where(e => departmentIds.Contains(e.DepartmentId) && e.isLabour == false)
                    .Select(e => new
                    {
                        e.Id,
                        e.FirstName,
                        e.LastName,
                        e.Email,
                        DepartmentName = _dbContext.Departments.Where(d => d.Id == e.DepartmentId).Select(d => d.deptName).FirstOrDefault()
                    })
                    .ToList();

                var departmentsWithEmployees = employees.Select(e => e.DepartmentName).Distinct();

                if (departmentsWithEmployees.Count() == departmentIds.Count())
                {
                    return Json(new { success = true, data = employees }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var departmentsWithoutEmployees = departmentIds.Except(_dbContext.Departments.Where(d => departmentsWithEmployees.Contains(d.deptName)).Select(d => d.Id));

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
        public ActionResult getEmployeeGraph()
        {
            var employeeData = _dbContext.Users
                .Where(e => e.isActive) // Assuming there's an IsActive property indicating the employee's active status
                .GroupBy(e => e.Gender)
                .Select(g => new
                {
                    Gender = g.Key,
                    TotalEmployees = g.Count()
                })
                .ToList();

            var pieData = employeeData.Select(e => new { value = e.TotalEmployees, name = e.Gender }).ToList();

            return Json(pieData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult EmployeeCounts()
        {
            var count = _dbContext.Users.Where(u => u.isActive == true).Count();
            return Json(new { employeeCount = count }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult PayrollCountThisMonth()
        {
            int month = DateTime.Now.Month;

            var totalPayment = _dbContext.Payroll
                                         .Where(p => p.Month == month)
                                         .Sum(p => (decimal?)p.TotalSalary) ?? 0;

            return Json(new {  totalPayment = totalPayment }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetEmployeesOnLeaveCount()
        {
            DateTime today = DateTime.Today;

            var count = _dbContext.receivedLeaveRequests
                                 .Where(l => l.Date == today && l.Decision=="Approved")
                                 .Count();

            return Json(new { employeeOnLeaveCount = count }, JsonRequestBehavior.AllowGet);
        }

    }
}