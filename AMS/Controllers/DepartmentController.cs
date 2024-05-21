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
    [Authorize(Roles = "Admin")]

    public class DepartmentController : Controller
    {
        // GET: Department
        private ApplicationDbContext _dbContext;
        public DepartmentController()
        {
            _dbContext = new ApplicationDbContext();
        }
        // GET: Department

        public ActionResult Index()
        {
            return View();
        }
        //This function will return departmentlist to show in dataTabe
        public async Task<ActionResult> GetDepartmentData()
        {
            var deptList = await _dbContext.Departments.AsNoTracking().ToListAsync();
            return Json(deptList, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DepartmentDetails(Department department)
        {
            try
            {
                if (department.Id == 0)
                {
                    _dbContext.Departments.Add(department);
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Department Added Successfully." });
                }
                else
                {
                    var deptInDb = _dbContext.Departments.FirstOrDefault(d => d.Id == department.Id);
                    deptInDb.deptName = department.deptName;
                    deptInDb.isActive = department.isActive;
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Department Updated Successfully." });
                }
            }
            catch (Exception)
            {
                // Log the exception or handle it as needed
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }
        public ActionResult GetDeptById(int id)
        {
            var department = _dbContext.Departments.AsNoTracking().SingleOrDefault(d => d.Id == id);

            if (department == null)
            {
                // Handle not found case
                return Json(new { success = false, message = "Department Record Doesnot Found" });
            }

            return Json(department, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Delete(int id)
        {
            var deptInDb = _dbContext.Departments.SingleOrDefault(d => d.Id == id);
            if (deptInDb == null)
            {
                return Json(new { success = false, message = "Department Record Doesnot Found" });
            }

            else
            {
                _dbContext.Departments.Remove(deptInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Department Successfully Deleted" });
            }
        }

        //this function is used to check duplicate name for department
        [HttpPost]
        public JsonResult IsNameAvailable(string name, int? id, bool isUpdate)
        {
            bool isNameAvailable;

            if (isUpdate && id.HasValue)
            {
                // For update, exclude the current department name with the specified ID
                isNameAvailable = !_dbContext.Departments.AsNoTracking().Any(x => x.deptName == name && x.Id != id.Value);
            }
            else
            {
                // For add operation or if the ID is not provided, check for duplicates without exclusion
                isNameAvailable = !_dbContext.Departments.AsNoTracking().Any(x => x.deptName == name);
            }

            return Json(isNameAvailable);
        }
    }
}