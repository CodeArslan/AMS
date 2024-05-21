using AMS.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class AttendanceController : Controller
    {
        private ApplicationDbContext _dbContext;
        private SerialPort port;
        private bool isListening;

        public AttendanceController()
        {
            _dbContext = new ApplicationDbContext();
            isListening = false;
        }
        // GET: Attendance
        [Authorize(Roles = "Admin")]

        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Employee,HR,Labour,Admin")]
        public ActionResult EmployeeAttendance()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]

        public ActionResult AttendanceForLabours()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]

        public ActionResult AttendanceForCardHolders()
        {
            return View();
        }
        [HttpGet]
        public ActionResult GetEmployeeByCard(string employeeNumber)
        {
            var employeeIdParts = employeeNumber.Split('-');

            // Check if the employeeId format is valid
            if (employeeIdParts.Length != 3)
            {
                return Json(new { error = "Invalid Employee Number format" }, JsonRequestBehavior.AllowGet);
            }

            // Retrieving the employee from the database based on the type
            var employeeType = employeeIdParts[1];
            employeeType = employeeType.ToLower();
            var employee = _dbContext.Users.Where(e => e.employeeNumber == employeeNumber && e.isActive).ToList();
            // Check if either employee or labour is null
            if (employee == null || employee.Count == 0)
            {
                // Return error message
                return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Return the employee data
                return Json(employee, JsonRequestBehavior.AllowGet);
            }



        }
        [HttpGet]
        public ActionResult GetLabourList(DateTime? date)
        {
            // Get the list of labours for whom attendance has not been marked today
            var selectedDate = date ?? DateTime.Now.Date;
            var attendedLaboursToday = _dbContext.Attendance
                .Where(a => a.date == selectedDate && a.labourId != null)
                .Select(a => a.labourId)
                .ToList();

            // Get the list of labours excluding those for whom attendance has been marked today
            var labourList = _dbContext.Users
                .Include(l => l.Shift)
                .AsNoTracking()
                .Where(l =>l.isLabour==true && l.shiftId != null && !attendedLaboursToday.Contains(l.Id))
                .ToList();

            // Format the labour list
            var formattedLabourList = labourList.Select(labList => new
            {
                Id = labList.Id,
                FirstName = labList.FirstName,
                LastName = labList.LastName,
                TimeIn = labList.Shift != null && labList.Shift.startTime != null ? labList.Shift.startTime.ToString(@"hh\:mm\:ss") : "00:00:00",
                TimeOut = labList.Shift != null && labList.Shift.endTime != null ? labList.Shift.endTime.ToString(@"hh\:mm\:ss") : "00:00:00"
                // Other properties as needed
            }).ToList();

            return Json(formattedLabourList, JsonRequestBehavior.AllowGet);
        }

        public void StartListening()
        {
            // Start listening for RFID card data in a background thread
            Thread listeningThread = new Thread(ListenForRFID);
            listeningThread.IsBackground = true;
            listeningThread.Start();
        }
        [HttpGet]
        public ActionResult GetEmployeeAttendanceData()
        {
            // Get the current date
            DateTime currentDate = DateTime.Today;

            // Retrieve records for the current date and order them by date in descending order
            var attList = _dbContext.Attendance
                .Include(c => c.ApplicationUser)
                .Include(c => c.Labour)
                .Where(att => att.date == currentDate && att.employeeId!=null && att.status!="Absent")
                .OrderBy(att => att.date)
                .AsNoTracking()
                .ToList();

            // Format timeIn and timeOut values to string representation
            var formattedAttList = attList.Select(att => new
            {
                Id = att.Id,
                ApplicationUser = att.ApplicationUser,
                Labour = att.Labour,
                date = att.date,
                timeIn = att.timeIn.ToString(@"hh\:mm\:ss"), // Format as HH:mm:ss
                timeOut = att.timeOut.ToString(@"hh\:mm\:ss"), // Format as HH:mm:ss
                totalWorkedTime = att.totalWorkedTime
            });

            return Json(formattedAttList, JsonRequestBehavior.AllowGet);
        }

       

        private void ListenForRFID()
        {
            port = new SerialPort("COM3", 9600); // Change COM port accordingly

            port.DataReceived += Port_DataReceived;
            port.Open();
            isListening = true;

            while (isListening)
            {
                Thread.Sleep(100); // Adjust sleep duration as needed
            }

            port.Close();
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(1000);
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();

            // Split the received data into individual strings based on the "\r\n" delimiter
            string[] dataArray = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            HashSet<string> uniqueData = new HashSet<string>();

            // Iterate through each string in dataArray
            foreach (string item in dataArray)
            {
                // Remove any additional characters from the data string
                string cleanedData = item.Replace("/", "");

                // Check if the cleaned data is not empty
                if (!string.IsNullOrEmpty(cleanedData))
                {
                    // Add the cleaned data to the HashSet
                    uniqueData.Add(cleanedData);
                }
            }
            ProcessUniqueData(uniqueData);
        }


        private void ProcessUniqueData(HashSet<string> uniqueData)
        {
            List<string> results = new List<string>();

            foreach (string uniqueItem in uniqueData)
            {
                // Query the database to check if the received card data exists and is active
                var cardEmployee = (from card in _dbContext.Cards
                                    join employee in _dbContext.Users on card.Id equals employee.CardId
                                    where card.cardCode == uniqueItem && card.isActive == true && employee.isActive == true
                                    select new
                                    {
                                        Card = card,
                                        EmployeeId = employee.Id
                                    }).FirstOrDefault();

                if (cardEmployee != null)
                {
                    string employeeId = cardEmployee.EmployeeId;

                    var currentDate = DateTime.Now.Date;
                    var existingAttendance = _dbContext.Attendance
                        .Where(a => a.employeeId == employeeId && DbFunctions.TruncateTime(a.date) == currentDate)
                        .FirstOrDefault();

                    if (existingAttendance != null)
                    {
                        // Employee has already checked in, update the timeout
                        existingAttendance.timeOut = DateTime.Now.TimeOfDay;

                        // Calculate total worked hours
                        TimeSpan workedHours;
                        if (existingAttendance.timeOut < existingAttendance.timeIn)
                        {
                            // Handle the case where time spans across two different days
                            workedHours = TimeSpan.FromHours(24) - (existingAttendance.timeIn - existingAttendance.timeOut);
                        }
                        else
                        {
                            workedHours = existingAttendance.timeOut - existingAttendance.timeIn;
                        }

                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)workedHours.TotalHours;
                        int totalWorkedMinutes = workedHours.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";
                        existingAttendance.status = "Present";
                        // Store the formatted total worked time in the database column
                        existingAttendance.totalWorkedTime = formattedTotalWorkedTime;
                        _dbContext.SaveChanges();

                        // Add result to the list
                        port.Write("G");
                        CalculatePayroll(employeeId);

                    }
                    else
                    {
                        // Employee is checking in for the first time today, mark time in
                        Attendance attendance = new Attendance
                        {
                            date = DateTime.Now.Date,
                            timeIn = DateTime.Now.TimeOfDay,
                            employeeId = employeeId,
                            status = "Present"
                        };
                        attendance.timeOut = TimeSpan.FromSeconds(5);
                        _dbContext.Attendance.Add(attendance);
                        _dbContext.SaveChanges();

                        // Add result to the list
                        port.Write("G");
                    }
                }
                else
                {
                    // Add result to the list
                    port.Write("R");
                    // Handle the case accordingly
                }
            }

        }


        private void CalculatePayroll(string employeeId)
        {
            // Get the current month and year
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            // Retrieve attendance records for the current month, year, and employee
            var attendances = _dbContext.Attendance
                .Where(a => a.employeeId == employeeId && a.date.Month == currentMonth && a.date.Year == currentYear)
                .ToList();

            // Initialize variables for total hours worked and total salary
            double totalNormalHoursWorked = 0;
            double totalOvertimeHoursWorked = 0;
            decimal totalSalary = 0;

            // Assuming hourly rate is retrieved from the database
            decimal hourlyRate = _dbContext.Users.Where(e => e.Id == employeeId).Select(e => e.perHour).FirstOrDefault();
            decimal overtimeRate = hourlyRate * 1.5m; // 50% extra for overtime

            // Calculate total hours worked
            foreach (var attendance in attendances)
            {
                // Calculate total time worked for this attendance record
                TimeSpan workedTime;
                if (attendance.timeOut < attendance.timeIn)
                {
                    // Handle the case where time spans across two different days
                    workedTime = TimeSpan.FromHours(24) - (attendance.timeIn - attendance.timeOut);
                }
                else
                {
                    workedTime = attendance.timeOut - attendance.timeIn;
                }

                // Calculate normal and overtime hours
                double hoursWorked = workedTime.TotalHours - 1;
                if (hoursWorked > 8)
                {
                    totalNormalHoursWorked += 8;
                    totalOvertimeHoursWorked += hoursWorked - 8;
                }
                else
                {
                    totalNormalHoursWorked += hoursWorked;
                }
            }

            // Calculate total salary based on total hours worked
            totalSalary = (decimal)totalNormalHoursWorked * hourlyRate + (decimal)totalOvertimeHoursWorked * overtimeRate;

            // Format totalSalary to two decimal places
            totalSalary = Math.Round(totalSalary, 2);

            // Format totalHoursWorked to two decimal places
            double totalHoursWorked = Math.Round(totalNormalHoursWorked + totalOvertimeHoursWorked, 2);

            // Check if there's already a payroll entry for the current month, year, and employee
            var existingPayroll = _dbContext.Payroll
                .FirstOrDefault(p => p.Month == currentMonth && p.Year == currentYear && p.employeeId == employeeId);

            if (existingPayroll != null)
            {
                // Update existing payroll entry
                existingPayroll.TotalHoursWorked = totalHoursWorked;
                existingPayroll.TotalSalary = totalSalary;
            }
            else
            {
                // Create new payroll entry
                var payroll = new Payroll
                {
                    Month = currentMonth,
                    Year = currentYear,
                    TotalHoursWorked = totalHoursWorked,
                    TotalSalary = totalSalary,
                    employeeId = employeeId,
                    Bonus = 0
                };

                _dbContext.Payroll.Add(payroll);
            }

            _dbContext.SaveChanges();
        }

        private void CalculatePayrollforLabour(string labourId)
        {
            // Get the current month and year
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            // Retrieve attendance records for the current month, year, and employee
            var attendances = _dbContext.Attendance
                .Where(a => a.labourId == labourId && a.date.Month == currentMonth && a.date.Year == currentYear)
                .ToList();

            // Initialize variables for total hours worked and total salary
            double totalNormalHoursWorked = 0;
            double totalOvertimeHoursWorked = 0;
            decimal totalSalary = 0;

            // Assuming hourly rate is retrieved from the database
            decimal hourlyRate = _dbContext.Users.Where(e => e.Id == labourId).Select(e => e.perHour).FirstOrDefault();
            decimal overtimeRate = hourlyRate * 1.5m; // 50% extra for overtime

            // Calculate total hours worked
            foreach (var attendance in attendances)
            {
                // Calculate total time worked for this attendance record
                TimeSpan workedTime;
                if (attendance.timeOut < attendance.timeIn)
                {
                    // Handle the case where time spans across two different days
                    workedTime = TimeSpan.FromHours(24) - (attendance.timeIn - attendance.timeOut);
                }
                else
                {
                    workedTime = attendance.timeOut - attendance.timeIn;
                }

                // Calculate normal and overtime hours
                double hoursWorked = workedTime.TotalHours - 1;
                if (hoursWorked > 8)
                {
                    totalNormalHoursWorked += 8;
                    totalOvertimeHoursWorked += hoursWorked - 8;
                }
                else
                {
                    totalNormalHoursWorked += hoursWorked;
                }
            }

            // Calculate total salary based on total hours worked
            totalSalary = (decimal)totalNormalHoursWorked * hourlyRate + (decimal)totalOvertimeHoursWorked * overtimeRate;

            // Format totalSalary to two decimal places
            totalSalary = Math.Round(totalSalary, 2);

            // Format totalHoursWorked to two decimal places
            double totalHoursWorked = Math.Round(totalNormalHoursWorked + totalOvertimeHoursWorked, 2);

            // Check if there's already a payroll entry for the current month, year, and employee
            var existingPayroll = _dbContext.Payroll
                .FirstOrDefault(p => p.Month == currentMonth && p.Year == currentYear && p.labourId == labourId);

            if (existingPayroll != null)
            {
                // Update existing payroll entry
                existingPayroll.TotalHoursWorked = totalHoursWorked;
                existingPayroll.TotalSalary = totalSalary;
            }
            else
            {
                // Create new payroll entry
                var payroll = new Payroll
                {
                    Month = currentMonth,
                    Year = currentYear,
                    TotalHoursWorked = totalHoursWorked,
                    TotalSalary = totalSalary,
                    labourId = labourId,
                    Bonus = 0
                };

                _dbContext.Payroll.Add(payroll);
            }

            _dbContext.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                isListening = false; // Stop listening thread
            }
            base.Dispose(disposing);
        }
        public ActionResult UploadEmployeeAttendance(List<Attendance> attendances)
        {
            foreach (var attendance in attendances)
            {
                var existingEmployee = _dbContext.Users.Where(l => l.Id == attendance.employeeId).FirstOrDefault();
                if (existingEmployee != null)
                {
                    string employeeId = existingEmployee.Id;
                    var currentDate = DateTime.Now.Date;
                    var existingAttendance = _dbContext.Attendance
                       .Where(a => a.employeeId == employeeId && DbFunctions.TruncateTime(a.date) == attendance.date)
                       .FirstOrDefault();
                    if (existingAttendance != null)
                    {
                        existingAttendance.timeIn = attendance.timeIn;
                        existingAttendance.timeOut = attendance.timeOut;
                        existingAttendance.status = "Present";
                        // Calculate the time difference between timeOut and timeIn
                        TimeSpan timeDifference = existingAttendance.timeOut - existingAttendance.timeIn;

                        // Check if timeOut is earlier than timeIn, indicating it spans across two days
                        if (timeDifference.TotalMinutes < 0)
                        {
                            // Adjust timeOut to be on the next day
                            timeDifference = TimeSpan.FromDays(1) + timeDifference;
                        }

                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)timeDifference.TotalHours;
                        int totalWorkedMinutes = timeDifference.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";

                        // Store the formatted total worked time in the database column
                        existingAttendance.totalWorkedTime = formattedTotalWorkedTime;
                        _dbContext.SaveChanges();
                        CalculatePayroll(employeeId);

                    }
                    else
                    {
                        // Calculate the time difference between timeOut and timeIn
                        TimeSpan timeDifference = existingAttendance.timeOut - existingAttendance.timeIn;

                        // Check if timeOut is earlier than timeIn, indicating it spans across two days
                        if (timeDifference.TotalMinutes < 0)
                        {
                            // Adjust timeOut to be on the next day
                            timeDifference = TimeSpan.FromDays(1) + timeDifference;
                        }

                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)timeDifference.TotalHours;
                        int totalWorkedMinutes = timeDifference.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";



                        // Employee is checking in for the first time today, mark time in
                        Attendance attendance1 = new Attendance
                        {
                            date = attendance.date,
                            timeIn = attendance.timeIn,
                            timeOut = attendance.timeOut,
                            employeeId = employeeId,
                            totalWorkedTime = formattedTotalWorkedTime,
                            status = "Present"
                        };
                        _dbContext.Attendance.Add(attendance1);
                        _dbContext.SaveChanges();
                        CalculatePayroll(employeeId);

                    }
                }

            }
            return Json(new { success = true, message = "Attendance Has Been Uploaded" });
        }

        public ActionResult UploadLabourAttendance(List<Attendance> attendances)
        {
            foreach (var attendance in attendances)
            {
                var existingLabour = _dbContext.Users.FirstOrDefault(l => l.Id == attendance.labourId);
                if (existingLabour != null)
                {
                    string labourId = existingLabour.Id;
                    var currentDate = DateTime.Now.Date;
                    var existingAttendance = _dbContext.Attendance
                       .FirstOrDefault(a => a.labourId == labourId && DbFunctions.TruncateTime(a.date) == attendance.date);
                    if (existingAttendance != null)
                    {
                        existingAttendance.timeIn = attendance.timeIn;
                        existingAttendance.timeOut = attendance.timeOut;
                        existingAttendance.status = "Present";
                        TimeSpan timeDifference = existingAttendance.timeOut - existingAttendance.timeIn;

                        // Check if timeOut is earlier than timeIn, indicating it spans across two days
                        if (timeDifference.TotalMinutes < 0)
                        {
                            // Adjust timeOut to be on the next day
                            timeDifference = TimeSpan.FromDays(1) + timeDifference;
                        }

                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)timeDifference.TotalHours;
                        int totalWorkedMinutes = timeDifference.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";

                        // Store the formatted total worked time in the database column
                        existingAttendance.totalWorkedTime = formattedTotalWorkedTime;
                        _dbContext.SaveChanges();
                        CalculatePayrollforLabour(existingLabour.Id);
                    }
                    else
                    {
                        TimeSpan timeDifference = existingAttendance.timeOut - existingAttendance.timeIn;

                        // Check if timeOut is earlier than timeIn, indicating it spans across two days
                        if (timeDifference.TotalMinutes < 0)
                        {
                            // Adjust timeOut to be on the next day
                            timeDifference = TimeSpan.FromDays(1) + timeDifference;
                        }

                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)timeDifference.TotalHours;
                        int totalWorkedMinutes = timeDifference.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";

                        // Employee is checking in for the first time today, mark time in
                        DateTime dateTime = attendance.date.Date;
                        Attendance attendance1 = new Attendance
                        {
                            date = dateTime,
                            timeIn = attendance.timeIn,
                            timeOut = attendance.timeOut,
                            labourId = labourId,
                            totalWorkedTime = formattedTotalWorkedTime,
                            status = "Present"
                        };
                        _dbContext.Attendance.Add(attendance1);
                        _dbContext.SaveChanges();
                        CalculatePayrollforLabour(labourId);
                    }
                }
            }
            return Json(new { success = true, message = "Attendance Has Been Uploaded" });
        }
        public ActionResult GetLabourAttendance(DateTime? date)
        {
            // Use the provided date or default to current date if not provided
            var selectedDate = date ?? DateTime.Now.Date;
            var allLabours = _dbContext.Users.Where(l=>l.shiftId!=null&&l.isActive==true&&l.isLabour==true).ToList(); // Assuming you have a Labour table in your database
            var presentLabours = _dbContext.Attendance
                .Include(a => a.Labour)
                .Where(a => a.date == selectedDate && a.labourId != null && a.status == "present")
                .OrderBy(att => att.date)
                .AsNoTracking()
                .ToList();

            var formattedAttList = allLabours.Select(labour =>
            {
                var attendance = presentLabours.FirstOrDefault(att => att.labourId == labour.Id);
                return new
                {
                    Id = labour.Id,
                    Labour = labour,
                    date = attendance != null ? (DateTime?)attendance.date : null,
                    timeIn = attendance != null ? (attendance.timeIn != null ? attendance.timeIn.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    timeOut = attendance != null ? (attendance.timeOut != null ? attendance.timeOut.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    totalWorkedTime = attendance != null ? attendance.totalWorkedTime : null,
                    status = attendance != null ? "Present" : "Absent"
                };
            });

            return Json(formattedAttList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetEmployeeAttendance(DateTime? date)
        {
            // Use the provided date or default to current date if not provided
            var selectedDate = date ?? DateTime.Now.Date;
            var allEmployees = _dbContext.Users.Where(e=>e.isActive == true&&e.isLabour==false).ToList(); 
            var presentEmployees = _dbContext.Attendance
                .Include(a => a.ApplicationUser)
                .Where(a => a.date == selectedDate && a.employeeId != null && a.status == "Present")
                .OrderBy(att => att.date)
                .AsNoTracking()
                .ToList();

            var formattedAttList = allEmployees.Select(employee =>
            {
                var attendance = presentEmployees.FirstOrDefault(att => att.employeeId == employee.Id);
                return new
                {
                    Id = employee.Id,
                    Employee = employee,
                    date = attendance != null ? (DateTime?)attendance.date : null,
                    timeIn = attendance != null ? (attendance.timeIn != null ? attendance.timeIn.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    timeOut = attendance != null ? (attendance.timeOut != null ? attendance.timeOut.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    totalWorkedTime = attendance != null ? attendance.totalWorkedTime : null,
                    status = attendance != null ? "Present" : "Absent"
                };
            });

            return Json(formattedAttList, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetEmployeeAttendanceByUser(DateTime? date)
        {
            // Use the provided date or default to current date if not provided
            var selectedDate = date ?? DateTime.Now.Date;
            var loggedInUser=User.Identity.GetUserId();
            var allEmployees = _dbContext.Users.Where(e => e.Id==loggedInUser&&e.isActive==true).ToList();
            var presentEmployees = _dbContext.Attendance
                .Include(a => a.ApplicationUser)
                .Where(a => a.date == selectedDate && a.employeeId != null && a.status == "Present")
                .OrderBy(att => att.date)
                .AsNoTracking()
                .ToList();

            var formattedAttList = allEmployees.Select(employee =>
            {
                var attendance = presentEmployees.FirstOrDefault(att => att.employeeId == employee.Id);
                return new
                {
                    Id = employee.Id,
                    Employee = employee,
                    date = attendance != null ? (DateTime?)attendance.date : null,
                    timeIn = attendance != null ? (attendance.timeIn != null ? attendance.timeIn.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    timeOut = attendance != null ? (attendance.timeOut != null ? attendance.timeOut.ToString(@"hh\:mm\:ss") : null) : null, // Format as HH:mm:ss
                    totalWorkedTime = attendance != null ? attendance.totalWorkedTime : null,
                    status = attendance != null ? "Present" : "Absent"
                };
            });

            return Json(formattedAttList, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult MarkAbsent()
        //{
        //    var activeLabours = _dbContext.Labours
        //        .Where(l => l.shiftId != null && l.isActive)
        //        .ToList();

        //    var activeEmployees = _dbContext.Users
        //        .Where(e => e.isActive)
        //        .ToList();

        //    var today = DateTime.Today;

        //    foreach (var labour in activeLabours)
        //    {
        //        // Check if attendance already exists for this labour on today's date
        //        var existingAttendance = _dbContext.Attendance
        //            .FirstOrDefault(a => a.labourId == labour.Id && a.date == today);

        //        if (existingAttendance == null)
        //        {
        //            var attendance = new Attendance
        //            {
        //                date = today,
        //                labourId = labour.Id,
        //                status = "Absent"
        //            };

        //            _dbContext.Attendance.Add(attendance);
        //        }
        //    }

        //    foreach (var employee in activeEmployees)
        //    {
        //        // Check if attendance already exists for this employee on today's date
        //        var existingAttendance = _dbContext.Attendance
        //            .FirstOrDefault(a => a.employeeId == employee.Id && a.date == today);

        //        if (existingAttendance == null)
        //        {
        //            var attendance = new Attendance
        //            {
        //                date = today,
        //                employeeId = employee.Id,
        //                status = "Absent"
        //            };

        //            _dbContext.Attendance.Add(attendance);
        //        }
        //    }

        //    _dbContext.SaveChanges();

        //    return Content("");
        //}

    }
}