using AMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
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
        public ActionResult GetPayrollData()
        {
            var payrollList=_dbContext.Payroll.Include(c=>c.ApplicationUser).ToList();
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
    }

}    
