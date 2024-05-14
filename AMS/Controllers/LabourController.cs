using AMS.Models;
using AMS.ViewModels;
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
    public class LabourController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext _dbContext;
        public LabourController()
        {
            _dbContext = new ApplicationDbContext();
        }

        public LabourController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        public ActionResult Index()
        {
            var shiftList = _dbContext.Shifts.ToList();
            var departmentList=_dbContext.Departments.ToList();
            var viewModel = new RegisterViewModel {
            Department=departmentList,
            Shift=shiftList
            };
            return View(viewModel);
        }
        public async Task<ActionResult> GetLabourData()
        {
            var labourList = await _dbContext.Users.Where(l=>l.isLabour==true).Include(l=>l.Department).AsNoTracking().ToListAsync();
            return Json(labourList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetLabourById(string id)
        {
            var labour = _dbContext.Users.AsNoTracking().SingleOrDefault(l => l.Id == id);

            if (labour == null)
            {
                // Handle not found case
                return Json(new { success = false, message = "Labour Record Does Not Found" });
            }

            return Json(labour, JsonRequestBehavior.AllowGet);
        }
        public int getEmployeeNumber()
        {
            // Retrieve the highest employee number from the database
            var highestLabourNumber = _dbContext.Users
                .Select(u => u.employeeNumber)
                .Where(en => en.StartsWith("Cactus-LA-"))
                .OrderByDescending(en => en)
                .FirstOrDefault();

            int highestNumber;
            if (highestLabourNumber != null && int.TryParse(highestLabourNumber.Replace("Cactus-LA-", ""), out highestNumber))
            {
                return highestNumber;
            }
            else
            {
                // If no employee number exists yet, return 0
                return 0;
            }
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            ModelState.Remove("employeeNumber");
            if(model.Id!=null)
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }
            if (ModelState.IsValid)
            {
                // Check if user exists for update
                if (model.Id != null)
                {
                    var existingUser = await UserManager.FindByIdAsync(model.Id);
                    if (existingUser != null)
                    {
                        existingUser.Email = model.FirstName+existingUser.employeeNumber.Replace("Cactus-LA-", "");
                        existingUser.Email = model.Email;
                        existingUser.CNIC = model.CNIC;
                        existingUser.DepartmentId = model.DepartmentId;
                        existingUser.perHour = model.perHour;
                        existingUser.FirstName = model.FirstName;
                        existingUser.LastName = model.LastName;
                        existingUser.CardId = model.CardId;
                        existingUser.isActive = model.isActive;
                        existingUser.Phone = model.Phone;
                        existingUser.Address = model.Address;
                        existingUser.Gender = model.Gender;
                        existingUser.Designation = model.Designation;
                        existingUser.Role = model.Role;

                        var userRoles = await UserManager.GetRolesAsync(existingUser.Id);

                        // Remove user from all existing roles
                        foreach (var role in userRoles)
                        {
                            await UserManager.RemoveFromRoleAsync(existingUser.Id, role);
                        }

                        await UserManager.AddToRoleAsync(existingUser.Id, model.Role);

                        // Update user
                        var updateResult = await UserManager.UpdateAsync(existingUser);
                        if (updateResult.Succeeded)
                        {
                            return Json(new { success = true, message = "Profile updated successfully.", user = existingUser });

                        }
                        var erroring = updateResult.Errors.ToList();
                        return Json(new { success = false, message = "Updation Failed. Please Check Following Errors", errors = erroring });

                    }
                    else
                    {
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }
                else
                {
                    // New user registration logic
                    int highestNumber = getEmployeeNumber();

                    // Increment the highest number to ensure uniqueness
                    highestNumber++;

                    // Format the number as a four-digit string
                    string uniqueNumber = highestNumber.ToString("D4");

                    // Concatenate with "Cactus-EM-" to form the new employee number
                    string newEmployeeNumber = "Cactus-LA-" + uniqueNumber;

                    // Now assign this newEmployeeNumber to the employee being registered
                    var user = new ApplicationUser
                    {
                        UserName = model.FirstName+uniqueNumber,
                        Email = model.Email,
                        CNIC = model.CNIC,
                        DepartmentId = model.DepartmentId,
                        perHour = model.perHour,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CardId = null,
                        isActive = model.isActive,
                        Phone = model.Phone,
                        Address = model.Address,
                        leaveBalance = 2,
                        Designation = model.Designation,
                        Gender = model.Gender,
                        Role = model.Role,
                        isLabour=true,
                        employeeNumber = newEmployeeNumber // Assign the new employee number here
                    };
                    var result = await UserManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await UserManager.AddToRoleAsync(user.Id, model.Role);
                        //var roleStore = new RoleStore<IdentityRole>(new ApplicationDbContext());
                        //var roleManager = new RoleManager<IdentityRole>(roleStore);
                        //await roleManager.CreateAsync(new IdentityRole("Admin"));
                        //await roleManager.CreateAsync(new IdentityRole("Employee"));
                        //await roleManager.CreateAsync(new IdentityRole("HR"));
                        //await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return Json(new { success = true, message = "Registration successful.", user = user });
                    }
                    var erroring = result.Errors.ToList();
                    return Json(new { success = false, message = "Registration Failed. Please Check Following Errors", errors = erroring }) ;
                }
            }
            return Json(new { success = false, message = "Registration Failed. Please Check Following Errors", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
          
        }
        //public ActionResult LabourDetails(Labour labour)
        //{
        //    try
        //    {
        //        if (labour.Id == 0)
        //        {
        //            int highestNumber = getEmployeeNumber();

        //            // Increment the highest number to ensure uniqueness
        //            highestNumber++;

        //            // Format the number as a four-digit string
        //            string uniqueNumber = highestNumber.ToString("D4");

        //            // Concatenate with "Cactus-EM-" to form the new employee number
        //            string newEmployeeNumber = "Cactus-LA-" + uniqueNumber;

        //            // Adding a new labour
        //            labour.labourNumber = newEmployeeNumber;
        //            _dbContext.Labours.Add(labour);
        //            _dbContext.SaveChanges();
        //            return Json(new { success = true, message = "Labour Added Successfully." });
        //        }
        //        else
        //        {
        //            // Updating an existing labour
        //            var labourInDb = _dbContext.Labours.FirstOrDefault(l => l.Id == labour.Id);

        //            if (labourInDb != null)
        //            {
        //                labourInDb.FirstName = labour.FirstName;
        //                labourInDb.LastName = labour.LastName;
        //                labourInDb.Email = labour.Email;
        //                labourInDb.CNIC = labour.CNIC;
        //                labourInDb.perHour = labour.perHour;
        //                labourInDb.totalPay = labour.totalPay;
        //                labourInDb.departmentId = labour.departmentId;
        //                labourInDb.isActive = labour.isActive;
        //                _dbContext.SaveChanges();
        //                return Json(new { success = true, message = "Labour Updated Successfully." });
        //            }
        //            else
        //            {
        //                return Json(new { success = false, message = "Labour not found." });
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        // Log the exception or handle it as needed
        //        return Json(new { success = false, message = "An error occurred while processing your request." });
        //    }
        //}
        public ActionResult Delete(string id)
        {
            var labourInDb = _dbContext.Users.SingleOrDefault(l => l.Id == id);
            if (labourInDb == null)
            {
                return Json(new { success = false, message = "Labour Record Does Not Found" });
            }

            try
            {
                _dbContext.Users.Remove(labourInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Labour Successfully Deleted" });
            }
            catch (Exception)
            {
                // Log the exception or handle it appropriately
                return Json(new { success = false, message = "An error occurred while deleting the Labour record" });
            }
        }
        //[HttpPost]
        //public JsonResult IsEmailAvailable(string email, int? id, bool isUpdate)
        //{
        //    bool isEmailAvailable;

        //    if (isUpdate && id.HasValue)
        //    {
        //        // For update, exclude the current email with the specified ID from both tables
        //        isEmailAvailable = !_dbContext.Labours.AsNoTracking().Any(l => l.Email == email && l.Id != id.Value) &&
        //                           !_dbContext.Users.AsNoTracking().Any(u => u.Email == email);
        //    }
        //    else
        //    {
        //        // For add operation or if the ID is not provided, check for duplicates without exclusion
        //        isEmailAvailable = !_dbContext.Labours.AsNoTracking().Any(l => l.Email == email) &&
        //                           !_dbContext.Users.AsNoTracking().Any(u => u.Email == email);
        //    }

        //    return Json(isEmailAvailable);
        //}


    }
}