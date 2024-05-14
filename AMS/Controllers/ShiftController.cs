using AMS.Models;
using AMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
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
                    // Check if a shift with same client name, shift type, and location already exists
                    var existingShift = _dbContext.Shifts.FirstOrDefault(s =>
                        s.clientName == shift.clientName &&
                        s.shiftType == shift.shiftType &&
                        s.location == shift.location);

                    if (existingShift != null)
                    {
                        return Json(new { success = false, message = "A shift with same Client Name, Shift Type, and Location already exists." });
                    }

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
                        // Check if any shift with same client name, shift type, and location exists
                        var existingShift = _dbContext.Shifts.FirstOrDefault(s =>
                            s.clientName == shift.clientName &&
                            s.shiftType == shift.shiftType &&
                            s.location == shift.location &&
                            s.Id != shift.Id); // Exclude current shift from check

                        if (existingShift != null)
                        {
                            return Json(new { success = false, message = "A shift with same Client Name, Shift Type, and Location already exists." });
                        }

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
            var shiftInDb = _dbContext.Shifts.SingleOrDefault(s => s.Id == id);
            if (shiftInDb == null)
            {
                return Json(new { success = false, message = "Shift Record Does Not Exist" });
            }

            // Find all Labour records associated with this Shift
            var laboursWithShiftId = _dbContext.Users.Where(l => l.shiftId == id&&l.isLabour==true);

            // Remove the ShiftId association from Labour records
            foreach (var labour in laboursWithShiftId)
            {
                labour.shiftId = null; // Or you can set it to any default value as per your requirement
            }

            // Now remove the Shift record
            _dbContext.Shifts.Remove(shiftInDb);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Shift Successfully Deleted" });
        }

        public ActionResult AssignShift()
        {
            var shiftList = _dbContext.Shifts.ToList();
            var viewModel=new LabourShiftViewModel { Shifts = shiftList };
            return View(viewModel);
        }
        public ActionResult GetLabourList()
        {
           
            var labourList=_dbContext.Users.Where(l=>l.isLabour==true && l.isActive==true && l.shiftId==null).Select(a => new {
                FullName = a.FirstName + " " + a.LastName,
                Id = a.Id
            }).ToList();
            return Json(labourList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetTimeValues(int shiftId)
        {
            var shiftInDb = _dbContext.Shifts.Where(s => s.Id == shiftId).FirstOrDefault();
            var timeIn = shiftInDb.startTime;
            var timeOut = shiftInDb.endTime;

           return Json(new { timeIn = timeIn, timeOut = timeOut }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AssignShiftToLabours(List<string> assignedLabourIds, int shiftId)
        {
            try
            {
                // Find the shift from the database
                var shiftInDb = _dbContext.Shifts.SingleOrDefault(s => s.Id == shiftId);

                if (shiftInDb == null)
                {
                    return Json(new { success = false, message = "Shift not found" });
                }

                // Assign shift to labours
                foreach (var labourId in assignedLabourIds)
                {
                    var labourInDb = _dbContext.Users.SingleOrDefault(l => l.Id == labourId);

                    if (labourInDb != null)
                    {
                        // Set the shift ID for the labour
                        labourInDb.shiftId = shiftId;
                    }
                }

                // Save changes to the database
                _dbContext.SaveChanges();

                return Json(new { success = true, message = "Shift assigned successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error occurred: " + ex.Message });
            }
        }
        public async Task<ActionResult> GetAssignedShiftData(int? shiftId)
        {
            try
            {
                var assignedShiftData = await _dbContext.Users
                    .Where(l => !shiftId.HasValue || l.shiftId == shiftId)
                    .Join(_dbContext.Shifts,
                        labour => labour.shiftId,
                        shift => shift.Id,
                        (labour, shift) => new
                        {
                            LabourName = labour.FirstName + " " + labour.LastName,
                            ShiftName = shift.clientName,
                            Location = shift.location,
                            shiftType = shift.shiftType,
                            id = labour.Id
                        })
                    .ToListAsync();

                // Return the JSON result
                return Json(assignedShiftData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                // logger.LogError(ex, "Error occurred while fetching assigned shift data.");
                return Json(new { success = false, errorMessage = "An error occurred while fetching assigned shift data." });
            }
        }


        public ActionResult withdrawLabourFromShift(string[] ids)
        {
            try
            {
                foreach (string id in ids)
                {
                    var labourInDb = _dbContext.Users.SingleOrDefault(l => l.Id == id);

                    if (labourInDb != null)
                    {
                        labourInDb.shiftId = null;
                    }
                    else
                    {
                        // If a labour with the given ID is not found, continue to the next ID
                        continue;
                    }
                }

                _dbContext.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                // logger.LogError(ex, "Error occurred while withdrawing labour from shift.");
                return Json(new { success = false, errorMessage = "An error occurred while withdrawing labour from shift." });
            }
        }

    }
}