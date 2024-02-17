using AMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class ShiftController : Controller
    {
        // GET: Shift
        private ApplicationDbContext _dbContext;
        public ShiftController()
        {
            _dbContext = new ApplicationDbContext();
        }
        // GET: Shift
        public ActionResult Index()
        {
            return View();
        }
        //GetShift List
        public async Task<ActionResult> GetShiftData()
        {
            var shiftList = await _dbContext.Shifts.ToListAsync();
            // Format TimeSpan values to string representation
            var formattedShiftList = shiftList.Select(shift => new
            {
                Id = shift.Id,
                shiftType = shift.shiftType,
                startTime = shift.startTime.ToString(@"hh\:mm\:ss"), // Format as HH:mm:ss
                endTime = shift.endTime.ToString(@"hh\:mm\:ss"),     // Format as HH:mm:ss
                duration = (shift.endTime - shift.startTime).ToString(@"hh\:mm\:ss"), // Format as HH:mm:ss
                location = shift.location,
                clientName = shift.clientName,
                isActive = shift.isActive
            });

            return Json(formattedShiftList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ShiftDetails(Shift shift)
        {
            try
            {
                if (shift.Id == 0)
                {
                    // Adding a new shift
                    _dbContext.Shifts.Add(shift);
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Shift Added Successfully." });
                }
                else
                {
                    // Updating an existing shift
                    var shiftInDb = _dbContext.Shifts.FirstOrDefault(s => s.Id == shift.Id);

                    if (shiftInDb != null)
                    {
                        shiftInDb.shiftType = shift.shiftType;
                        shiftInDb.startTime = shift.startTime;
                        shiftInDb.endTime = shift.endTime;
                        shiftInDb.location = shift.location;
                        shiftInDb.clientName = shift.clientName;
                        shiftInDb.isActive = shift.isActive;

                        _dbContext.SaveChanges();
                        return Json(new { success = true, message = "Shift Updated Successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Shift not found." });
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception or handle it as needed
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }
        public ActionResult GetShiftById(int id)
        {
            var shift = _dbContext.Shifts.AsNoTracking().SingleOrDefault(s => s.Id == id);

            if (shift == null)
            {
                // Handle not found case
                return Json(new { success = false, message = "Shift Record Does Not Found" });
            }

            return Json(shift, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Delete(int id)
        {
            var shitInDb = _dbContext.Shifts.SingleOrDefault(s => s.Id == id);
            if (shitInDb == null)
            {
                return Json(new { success = false, message = "Shift Record Doesnot Found" });
            }

            else
            {
                _dbContext.Shifts.Remove(shitInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Shift Successfully Deleted" });
            }
        }

    }
}