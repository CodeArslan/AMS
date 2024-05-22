using AMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
namespace AMS.Controllers
{
    public class PayrollController : Controller
    {
        // GET: Payroll
        private ApplicationDbContext _dbContext;
        public PayrollController()
        {
            _dbContext = new ApplicationDbContext();
        }
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "HR")]
        public ActionResult RequestedSalaries()
        {
            return View();
        }
        [Authorize(Roles = "Employee,Labour,HR,Admin")]
        public ActionResult EmployeePayroll()
        {
            return View();
        }
        public ActionResult GetPayrollData(int? year, int? month)
        {
            IQueryable<Payroll> payrollQuery = _dbContext.Payroll
                                                .Include(c => c.ApplicationUser)
                                                .Include(c => c.Labour);

            if (!year.HasValue && !month.HasValue)
            {
                // Get current year and month
                int currentYear = DateTime.Now.Year;
                int currentMonth = DateTime.Now.Month;

                // Filter by current year and month
                payrollQuery = payrollQuery.Where(p => p.Year == currentYear && p.Month == currentMonth);
            }
            else
            {
                if (year.HasValue)
                {
                    payrollQuery = payrollQuery.Where(p => p.Year == year);
                }

                if (month.HasValue)
                {
                    payrollQuery = payrollQuery.Where(p => p.Month == month);
                }
            }

            var payrollList = payrollQuery.ToList();
            return Json(payrollList, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult GetPayrollDataByUser(int? year, int? month)
        {
            var loggedInUser = User.Identity.GetUserId();
            IQueryable<Payroll> payrollQuery = _dbContext.Payroll
     .Include(c => c.ApplicationUser)
     .Include(c => c.Labour)
     .Where(e => e.employeeId == loggedInUser || e.labourId == loggedInUser);


            if (!year.HasValue && !month.HasValue)
            {
                // Get current year and month
                int currentYear = DateTime.Now.Year;
                int currentMonth = DateTime.Now.Month;

                // Filter by current year and month
                payrollQuery = payrollQuery.Where(p => p.Year == currentYear && p.Month == currentMonth);
            }
            else
            {
                if (year.HasValue)
                {
                    payrollQuery = payrollQuery.Where(p => p.Year == year);
                }

                if (month.HasValue)
                {
                    payrollQuery = payrollQuery.Where(p => p.Month == month);
                }
            }

            var payrollList = payrollQuery.ToList();
            return Json(payrollList, JsonRequestBehavior.AllowGet);
        }



        public ActionResult sendForApproval(int[] id)
        {

            try
            {
                foreach (var itemId in id)
                {
                    // Fetch the record from the data source based on the ID
                    var item = _dbContext.Payroll.FirstOrDefault(i => i.Id == itemId); 

                    if (item != null)
                    {
                        // Update the 'isSendForApproval' field to true
                        item.isSendForApproval = true;
                        item.isApproved = false;
                        item.requestedSalary= item.TotalSalary - item.salaryApproved;
                    }
                }

                // Save changes to the database
                _dbContext.SaveChanges();

                // Return a JSON response indicating success
                return Json(new { success = true, message = "Records sent for approval successfully." });
            }
            catch (Exception ex)
            {
                // Return a JSON response indicating failure
                return Json(new { success = false, message = "Failed to send records for approval. Please try again later.", error = ex.Message });
            }
        }
        public ActionResult ApproveSalary(Payroll payroll)
        {
            var existingPay = _dbContext.Payroll.Where(p => p.Id == payroll.Id).FirstOrDefault();
            if (existingPay != null)
            {
                existingPay.isSendForApproval = false;
                existingPay.isApproved= true;
                existingPay.Bonus += payroll.Bonus;
                existingPay.salaryApproved += payroll.requestedSalary;
                existingPay.requestedSalary = 0;
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Salary has been approved" });

            }
            return Json(new { success = false, message = "An Error Occured While Approving. Please Try Again Later" });

        }
        public ActionResult getPayrollGraphData()
        {
            var payroll = _dbContext.Payroll
      .GroupBy(p => p.Month)
      .Select(g => new
      {
          Month = g.Key,
          Office = g.Where(p => p.employeeId != null).Sum(p => (decimal?)p.TotalSalary) ?? 0,
          Labour = g.Where(p => p.labourId != null).Sum(p => (decimal?)p.TotalSalary) ?? 0
      })
      .ToList();


            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec" };

            var officeData = new decimal[12];
            var labourData = new decimal[12];

            foreach (var item in payroll)
            {
                officeData[item.Month - 1] = item.Office;
                labourData[item.Month - 1] = item.Labour;
            }

            return Json(new { officeData, labourData }, JsonRequestBehavior.AllowGet);
        }
    }

}    
