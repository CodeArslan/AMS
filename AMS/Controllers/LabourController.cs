using AMS.Models;
using AMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class LabourController : Controller
    {
        // GET: Labour
        private ApplicationDbContext _dbContext;
        public LabourController()
        {
            _dbContext = new ApplicationDbContext();
        }
        public ActionResult Index()
        {
            var shiftList = _dbContext.Shifts.ToList();
            var departmentList=_dbContext.Departments.ToList();
            var viewModel = new LabourViewModel {
            Department=departmentList,
            Shift=shiftList
            };
            return View(viewModel);
        }
        public async Task<ActionResult> GetLabourData()
        {
            var labourList = await _dbContext.Labours.Include(l=>l.Department).AsNoTracking().ToListAsync();
            return Json(labourList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetLabourById(int id)
        {
            var labour = _dbContext.Labours.AsNoTracking().SingleOrDefault(l => l.Id == id);

            if (labour == null)
            {
                // Handle not found case
                return Json(new { success = false, message = "Labour Record Does Not Found" });
            }

            return Json(labour, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LabourDetails(Labour labour)
        {
            try
            {
                if (labour.Id == 0)
                {
                    // Adding a new labour
                    _dbContext.Labours.Add(labour);
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Labour Added Successfully." });
                }
                else
                {
                    // Updating an existing labour
                    var labourInDb = _dbContext.Labours.FirstOrDefault(l => l.Id == labour.Id);

                    if (labourInDb != null)
                    {
                        labourInDb.FirstName = labour.FirstName;
                        labourInDb.LastName = labour.LastName;
                        labourInDb.Email = labour.Email;
                        labourInDb.CNIC = labour.CNIC;
                        labourInDb.perHour = labour.perHour;
                        labourInDb.totalPay = labour.totalPay;
                        labourInDb.departmentId = labour.departmentId;
                        labourInDb.isActive = labour.isActive;
                        _dbContext.SaveChanges();
                        return Json(new { success = true, message = "Labour Updated Successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Labour not found." });
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception or handle it as needed
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }
        public ActionResult Delete(int id)
        {
            var labourInDb = _dbContext.Labours.SingleOrDefault(l => l.Id == id);
            if (labourInDb == null)
            {
                return Json(new { success = false, message = "Labour Record Does Not Found" });
            }

            try
            {
                _dbContext.Labours.Remove(labourInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Labour Successfully Deleted" });
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                return Json(new { success = false, message = "An error occurred while deleting the Labour record" });
            }
        }


    }
}