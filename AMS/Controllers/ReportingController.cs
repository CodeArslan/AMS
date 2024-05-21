using AMS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportingController : Controller
    {
        // GET: Reporting
        private ApplicationDbContext _dbContext;

        public ReportingController()
        {
            _dbContext = new ApplicationDbContext();
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetAttendanceData()
        {
            // Retrieve attendance data from the database
            var attendanceData = _dbContext.Attendance.ToList();

            // Group attendance data by month
            var groupedAttendance = attendanceData.GroupBy(a => a.date.Month)
                                                  .Select(g => new
                                                  {
                                                      Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                                                      AverageAttendanceRate = g.Average(a => CalculateAttendanceRate(a))
                                                  });

            // Extract months and average attendance rates
            var months = groupedAttendance.Select(a => a.Month).ToList();
            var averageAttendanceRates = groupedAttendance.Select(a => Math.Round(a.AverageAttendanceRate, 2)).ToList();

            // Create an anonymous object to hold the data
            var jsonData = new
            {
                Months = months,
                AverageAttendanceRates = averageAttendanceRates
            };

            // Serialize the data to JSON format
            var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

            // Return JSON result
            return jsonResult;
        }

        // Method to calculate attendance rate for an attendance record
        private double CalculateAttendanceRate(Attendance attendance)
        {
            // Calculate total expected working hours (assuming standard working hours)
            double totalExpectedHours = 8; // Assuming 8 hours as standard working hours

            // Calculate actual worked hours for the attendance record
            TimeSpan workedHours = attendance.timeOut - attendance.timeIn;
            double totalWorkedHours = workedHours.TotalHours;

            // Calculate attendance rate as percentage of hours worked out of total expected hours
            double attendanceRate = (totalWorkedHours / totalExpectedHours) * 100;

            // Ensure attendance rate is within 0 to 100 range
            attendanceRate = Math.Min(Math.Max(attendanceRate, 0), 100);

            return attendanceRate;
        }
        //public ActionResult GetAttendanceCountByMonth(string employeeId)
        //{
        //    // Splitting the employeeId to get the type
        //    var employeeIdParts = employeeId.Split('-');

        //    // Check if the employeeId format is valid
        //    if (employeeIdParts.Length != 3)
        //    {
        //        return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
        //    }

        //    // Retrieving the employee from the database based on the type
        //    var employeeType = employeeIdParts[1];
        //    employeeType = employeeType.ToLower();
        //    ApplicationUser employee = null;
        //    Labour labour = null;
        //    if (employeeType == "la")
        //    {
        //        labour = _dbContext.Labours.FirstOrDefault(e => e.labourNumber == employeeId && e.isActive);
        //    }
        //    else if (employeeType == "em")
        //    {
        //        employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
        //    }

        //    // Check if either employee or labour is null
        //    if (employee == null && labour == null)
        //    {
        //        return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
        //    }

        //    var monthlyData = new List<object>();

        //    // Iterate over each month
        //    for (int month = 1; month <= 12; month++)
        //    {
        //        // Get the number of days in the selected month
        //        var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);

        //        // Calculate the number of Sundays in the selected month
        //        var sundaysCount = Enumerable.Range(1, daysInMonth)
        //                                      .Count(day => new DateTime(DateTime.Now.Year, month, day).DayOfWeek == DayOfWeek.Sunday);

        //        // Calculate the maximum possible working days for the employee
        //        var maxWorkingDays = daysInMonth - sundaysCount;

        //        var presentCount = 0;
        //        if (employee != null)
        //        {
        //            presentCount = _dbContext.Attendance.Count(a => a.employeeId == employee.Id && a.date.Month == month);
        //        }
        //        else if (labour != null)
        //        {
        //            presentCount = _dbContext.Attendance.Count(a => a.labourId == labour.Id && a.date.Month == month);
        //        }

        //        // Calculate the attendance percentage for the employee
        //        var attendancePercentage = (double)presentCount / maxWorkingDays * 100;

        //        // Create an anonymous object to hold the data for this month
        //        var monthData = new
        //        {
        //            Month = month,
        //            EmployeeId = employeeId,
        //            EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown", // Assuming the user has a Name property
        //            MaxWorkingDays = maxWorkingDays,
        //            PresentCount = presentCount,
        //            AttendancePercentage = attendancePercentage
        //        };

        //        monthlyData.Add(monthData);
        //    }

        //    // Serialize the data to JSON format
        //    var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

        //    // Return JSON result
        //    return jsonResult;
        //}
        public ActionResult GetAttendanceCountByMonth(string employeeId = null)
        {
            // Check if employeeId is null or contains "cactus-"
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Fetch all records
                var allEmployees = _dbContext.Users.Where(e => e.isActive).ToList();
                var allLabours = _dbContext.Users.Where(e => e.isActive && e.shiftId != null&&e.isLabour==true).ToList();

                var monthlyData = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    var totalPresentCount = 0;
                    var totalMaxWorkingDays = 0;

                    // Iterate over each employee
                    foreach (var employee in allEmployees)
                    {
                        var monthlyEmployeeData = GetMonthlyDataForEmployee(employee.Id, month);
                        totalPresentCount += monthlyEmployeeData.PresentCount;
                        totalMaxWorkingDays += monthlyEmployeeData.MaxWorkingDays;
                    }

                    // Iterate over each labour
                    foreach (var labour in allLabours)
                    {
                        var monthlyLabourData = GetMonthlyDataForLabour(labour.Id, month);
                        totalPresentCount += monthlyLabourData.PresentCount;
                        totalMaxWorkingDays += monthlyLabourData.MaxWorkingDays;
                    }

                    // Calculate the attendance percentage for the month
                    var attendancePercentage = totalMaxWorkingDays == 0 ? 0 : (double)totalPresentCount / totalMaxWorkingDays * 100;

                    // Create an anonymous object to hold the data for this month
                    var monthData = new
                    {
                        Month = month,
                        TotalEmployees = allEmployees.Count,
                        TotalLabours = allLabours.Count,
                        PresentCount = totalPresentCount,
                        MaxWorkingDays = totalMaxWorkingDays,
                        AttendancePercentage = attendancePercentage
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
            else
            {
                //Splitting the employeeId to get the type
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive&&e.isLabour==true);
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                var monthlyData = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the number of days in the selected month
                    var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);

                    // Calculate the number of Sundays in the selected month
                    var sundaysCount = Enumerable.Range(1, daysInMonth)
                                                  .Count(day => new DateTime(DateTime.Now.Year, month, day).DayOfWeek == DayOfWeek.Sunday);

                    // Calculate the maximum possible working days for the employee
                    var maxWorkingDays = daysInMonth - sundaysCount;

                    var presentCount = 0;
                    if (employee != null)
                    {
                        presentCount = _dbContext.Attendance.Count(a => a.employeeId == employee.Id && a.date.Month == month);
                    }
                    else if (labour != null)
                    {
                        presentCount = _dbContext.Attendance.Count(a => a.labourId == labour.Id && a.date.Month == month);
                    }

                    // Calculate the attendance percentage for the employee
                    var attendancePercentage = (double)presentCount / maxWorkingDays * 100;

                    // Create an anonymous object to hold the data for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeId = employeeId,
                        EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown", // Assuming the user has a Name property
                        MaxWorkingDays = maxWorkingDays,
                        PresentCount = presentCount,
                        AttendancePercentage = attendancePercentage
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
        }

        private (int PresentCount, int MaxWorkingDays) GetMonthlyDataForEmployee(string employeeId, int month)
        {
            // Get the number of days in the selected month
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);

            // Calculate the number of Sundays in the selected month
            var sundaysCount = Enumerable.Range(1, daysInMonth)
                                          .Count(day => new DateTime(DateTime.Now.Year, month, day).DayOfWeek == DayOfWeek.Sunday);

            // Calculate the maximum possible working days for the employee
            var maxWorkingDays = daysInMonth - sundaysCount;

            var presentCount = _dbContext.Attendance.Count(a => a.employeeId == employeeId && a.date.Month == month);

            return (presentCount, maxWorkingDays);
        }

        private (int PresentCount, int MaxWorkingDays) GetMonthlyDataForLabour(string labourId, int month)
        {
            // Get the number of days in the selected month
            var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);

            // Calculate the number of Sundays in the selected month
            var sundaysCount = Enumerable.Range(1, daysInMonth)
                                          .Count(day => new DateTime(DateTime.Now.Year, month, day).DayOfWeek == DayOfWeek.Sunday);

            // Calculate the maximum possible working days for the labour
            var maxWorkingDays = daysInMonth - sundaysCount;

            var presentCount = _dbContext.Attendance.Count(a => a.labourId == labourId && a.date.Month == month);

            return (presentCount, maxWorkingDays);
        }


        public ActionResult GetLateEarlyCountsByMonth(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Get all attendance records for each month
                var monthlyData = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the number of late-ins and early-exits for all employees in the current month
                    // Construct the TimeSpan object outside of the LINQ query
                    TimeSpan lateInThreshold = TimeSpan.FromHours(9) + TimeSpan.FromMinutes(15);

                    // Now use lateInThreshold inside the LINQ query
                    var lateInCount = _dbContext.Attendance
                        .Count(a => a.date.Month == month && a.timeIn > lateInThreshold);

                    TimeSpan thresholdTimeforEarlyExit = TimeSpan.FromHours(15) + TimeSpan.FromMinutes(45);

                    // Now use thresholdTimeforEarlyExit inside the LINQ query
                    var earlyExitCount = _dbContext.Attendance
                        .Count(a => a.date.Month == month && a.timeOut < thresholdTimeforEarlyExit);

                    // Create an anonymous object to hold the late-in and early-exit counts for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeName = "All Employees",
                        LateInCount = lateInCount,
                        EarlyExitCount = earlyExitCount
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
            else
            {
                // Splitting the employeeId to get the type
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive&&e.isLabour==true);
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                var monthlyData = new List<object>();
                string employeeName = "";
                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the number of late-ins and early-exits for the employee in the current month
                    var lateInCount = 0;
                    var earlyExitCount = 0;
                    if (employee != null)
                    {
                        // Get the late-in count
                        TimeSpan thresholdTime = new TimeSpan(7, 15, 0); // Represents 7:15 AM
                        TimeSpan lateInThreshold = TimeSpan.FromHours(9) + TimeSpan.FromMinutes(15);

                        lateInCount = _dbContext.Attendance
                            .Where(a => a.employeeId == employee.Id &&
                                        a.date.Month == month &&
                                        a.timeIn > thresholdTime)
                            .Count();

                        // Get the early-exit count
                        TimeSpan thresholdTimeforEarlyExit = new TimeSpan(15, 45, 0); // Represents 4:00 PM

                        earlyExitCount = _dbContext.Attendance
                            .Where(a => a.employeeId == employee.Id &&
                                        a.date.Month == month &&
                                        a.timeOut < thresholdTimeforEarlyExit).Count();
                        employeeName = employee.FirstName + " " + employee.LastName;
                    }
                    else if (labour != null)
                    {
                        var laborShift = _dbContext.Users.Where(l => l.Id == labour.Id).Select(l => l.Shift).FirstOrDefault();

                        if (laborShift != null)
                        {
                            var shiftStartTime = laborShift.startTime; // Assuming StartTime is the shift start time in TimeSpan format
                            var shiftEndTime = laborShift.endTime;     // Assuming EndTime is the shift end time in TimeSpan format

                            var thresholdTime = TimeSpan.FromMinutes(15); // Calculate the threshold time (15 minutes in this case)

                            // Calculate the late-in threshold time
                            var lateInThreshold = shiftStartTime.Add(thresholdTime);

                            // Get the late-in count
                            lateInCount = _dbContext.Attendance
                                .Count(a => a.labourId == labour.Id &&
                                            a.date.Month == month &&
                                            (a.timeIn > lateInThreshold));

                            // Get the early-exit count
                            earlyExitCount = _dbContext.Attendance
                                .Count(a => a.labourId == labour.Id &&
                                            a.date.Month == month &&
                                            a.timeOut < shiftEndTime);
                            employeeName = labour.FirstName + " " + labour.LastName;
                        }

                    }

                    // Create an anonymous object to hold the late-in and early-exit counts for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeName = employeeName,
                        LateInCount = lateInCount,
                        EarlyExitCount = earlyExitCount
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }

        }

        public ActionResult LeavesByMonth()
        {
            var allMonths = Enumerable.Range(1, 12); // List of all months (1 to 12)

            var leavesByMonth = _dbContext.receivedLeaveRequests
                .Where(l => l.Date.HasValue) // Only consider leaves with a valid date
                .GroupBy(l => new { l.Date.Value.Year, l.Date.Value.Month }) // Group by year and month
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    ApprovedCount = g.Count(l => l.Decision == "Approved"), // Count approved leaves
                    RejectedCount = g.Count(l => l.Decision == "Rejected"), // Count rejected leaves
                    TotalCount = g.Count() // Total count of leaves for the month
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            var monthsWithData = leavesByMonth.Select(g => g.Month).ToList(); // Months with leave data
            var monthsWithoutData = allMonths.Except(monthsWithData); // Months without leave data

            // Create list of all months with corresponding leave counts (including 0 for months with no leaves)
            var mergedData = leavesByMonth
                .Concat(monthsWithoutData.Select(month => new { Year = DateTime.Now.Year, Month = month, ApprovedCount = 0, RejectedCount = 0, TotalCount = 0 }))
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            var months = mergedData.Select(g => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Month)).ToList(); // Using abbreviated month names
            var approvedLeaveCounts = mergedData.Select(g => g.ApprovedCount).ToList();
            var rejectedLeaveCounts = mergedData.Select(g => g.RejectedCount).ToList();
            var totalLeaveCounts = mergedData.Select(g => g.TotalCount).ToList();

            return Json(new { Months = months, ApprovedLeaveCounts = approvedLeaveCounts, RejectedLeaveCounts = rejectedLeaveCounts, TotalLeaveCounts = totalLeaveCounts, MergedData = mergedData }, JsonRequestBehavior.AllowGet);
        }


        //public ActionResult GetLeaveCountsByMonth(string employeeId)
        //{
        //    // Splitting the employeeId to get the type
        //    var employeeIdParts = employeeId.Split('-');

        //    // Check if the employeeId format is valid
        //    if (employeeIdParts.Length != 3)
        //    {
        //        return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
        //    }

        //    // Retrieving the employee from the database based on the type
        //    var employeeType = employeeIdParts[1];
        //    employeeType = employeeType.ToLower();
        //    ApplicationUser employee = null;
        //    Labour labour = null;
        //    if (employeeType == "la")
        //    {
        //        labour = _dbContext.Labours.FirstOrDefault(e => e.labourNumber == employeeId && e.isActive);
        //    }
        //    else if (employeeType == "em")
        //    {
        //        employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
        //    }

        //    // Check if either employee or labour is null
        //    if (employee == null && labour == null)
        //    {
        //        return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
        //    }

        //    var monthlyData = new List<object>();

        //    // Iterate over each month
        //    for (int month = 1; month <= 12; month++)
        //    {
        //        int leaveCount = 0;
        //        if (employee != null)
        //        {
        //            // Get the leave count for the employee in the current month
        //            leaveCount = _dbContext.receivedLeaveRequests
        //                .Where(l => l.employeeId == employee.Id && l.Date != null && l.Date.Value.Month == month)
        //                .Count();
        //        }

        //        else if (labour != null)
        //        {
        //            // Get the leave count for the labour in the current month
        //            leaveCount = _dbContext.receivedLeaveRequests
        //                .Where(l => l.labourId == labour.Id && l.Date != null && l.Date.Value.Month == month)
        //                .Count();
        //        }

        //        // Create an anonymous object to hold the leave count data for this month
        //        var monthData = new
        //        {
        //            Month = month,
        //            EmployeeId = employeeId,
        //            EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown",
        //            LeaveCount = leaveCount
        //        };

        //        monthlyData.Add(monthData);
        //    }

        //    // Serialize the data to JSON format
        //    var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

        //    // Return JSON result
        //    return jsonResult;
        //}
        public ActionResult GetLeaveDetailsByMonth(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Get leave details for all employees for each month
                var monthlyData = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the leave data for all employees in the current month
                    var leaveDataForMonth = _dbContext.receivedLeaveRequests
                        .Where(l => l.Date != null && l.Date.Value.Month == month)
                        .ToList();

                    // Count the number of approved, rejected, and total leaves for the month
                    var approvedCount = leaveDataForMonth.Count(l => l.Decision == "Approved");
                    var rejectedCount = leaveDataForMonth.Count(l => l.Decision == "Rejected");
                    var totalLeaves = leaveDataForMonth.Count;

                    // Get reasons for approved leaves
                    var approvedLeaveReasons = leaveDataForMonth
                        .Where(l => l.Decision == "Approved")
                        .Select(l => l.Reason)
                        .ToList();

                    // Create an anonymous object to hold the leave details for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeName = "All Employees",
                        ApprovedCount = approvedCount,
                        RejectedCount = rejectedCount,
                        TotalLeaves = totalLeaves,
                        ApprovedLeaveReasons = approvedLeaveReasons,
                        LeaveDetails = leaveDataForMonth
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
            else
            {
                // Splitting the employeeId to get the type
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive && e.isLabour==true );
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                var monthlyData = new List<object>();
                var employeesId = (employee != null) ? employee.Id : null;
                var labourId = (labour != null) ? labour.Id :null;
                for (int month = 1; month <= 12; month++)
                {
                    // Get the leave data for the employee/labour in the current month
                    int currentMonth = month; // Capture the month variable in a local variable
                    var currentEmployeesId = employeesId;
                    var currentLabourId = labourId;

                    // Get the leave data for the employee/labour in the current month
                    var leaveDataForMonth = _dbContext.receivedLeaveRequests
     .Where(l =>
         ((currentEmployeesId != null && l.employeeId == currentEmployeesId) ||
         (!string.IsNullOrEmpty(currentLabourId) && l.labourId == currentLabourId))
         && l.Date != null && l.Date.Value.Month == currentMonth)
     .ToList();

                    // Count the number of approved, rejected, and total leaves for the month
                    var approvedCount = leaveDataForMonth.Count(l => l.Decision == "Approved");
                    var rejectedCount = leaveDataForMonth.Count(l => l.Decision == "Rejected");
                    var totalLeaves = leaveDataForMonth.Count;

                    // Get reasons for approved leaves
                    var approvedLeaveReasons = leaveDataForMonth
                        .Where(l => l.Decision == "Approved")
                        .Select(l => l.Reason)
                        .ToList();

                    // Create an anonymous object to hold the leave details for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeId = employeeId,
                        EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown",
                        ApprovedCount = approvedCount,
                        RejectedCount = rejectedCount,
                        TotalLeaves = totalLeaves,
                        ApprovedLeaveReasons = approvedLeaveReasons,
                        LeaveDetails = leaveDataForMonth
                    };

                    monthlyData.Add(monthData);
                }


                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }

        }


        public ActionResult GetLeaveReasonCounts(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Get leave reason counts for all employees
                var leaveReasonCounts = _dbContext.receivedLeaveRequests
                    .GroupBy(l => l.Reason)
                    .Select(group => new
                    {
                        Reason = group.Key,
                        Count = group.Count()
                    })
                    .ToList();

                // Extract counts for specific reasons
                var healthIssuesCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Health Issues")?.Count ?? 0;
                var familyResponsibilitiesCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Family Responsibilities")?.Count ?? 0;
                var vacationCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Vacation")?.Count ?? 0;
                var legalObligationsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Legal Obligations")?.Count ?? 0;
                var emergencySituationsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Emergency Situations")?.Count ?? 0;
                var otherReasonsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Other")?.Count ?? 0;

                // Create custom response object
                var response = new
                {
                    EmployeeName = "All Employees",
                    HealthIssuesCount = healthIssuesCount,
                    FamilyResponsibilitiesCount = familyResponsibilitiesCount,
                    VacationCount = vacationCount,
                    LegalObligationsCount = legalObligationsCount,
                    EmergencySituationsCount = emergencySituationsCount,
                    OtherReasonsCount = otherReasonsCount
                };

                // Create JSON result with leave reason counts
                return Json(response, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive&&e.isLabour==true);
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                // Get the leave reasons for the employee/labour along with their counts
                var employeesId = (employee != null) ? employee.Id : null;
                var labourId = (labour != null) ? labour.Id : null;

                var leaveReasonCounts = _dbContext.receivedLeaveRequests
     .Where(l =>
         ((employeesId != null && l.employeeId == employeesId) ||
         (!string.IsNullOrEmpty(labourId) && l.labourId == labourId)))
     .ToList() // Execute the query to retrieve all matching records
     .GroupBy(l => l.Reason)
     .Select(group => new
     {
         Reason = group.Key,
         Count = group.Count()
     })
     .ToList();


                // Extract counts for specific reasons
                var healthIssuesCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Health Issues")?.Count ?? 0;
                var familyResponsibilitiesCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Family Responsibilities")?.Count ?? 0;
                var vacationCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Vacation")?.Count ?? 0;
                var legalObligationsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Legal Obligations")?.Count ?? 0;
                var emergencySituationsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Emergency Situations")?.Count ?? 0;
                var otherReasonsCount = leaveReasonCounts.FirstOrDefault(r => r.Reason == "Other")?.Count ?? 0;
                var employeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown";
                // Create custom response object
                var response = new
                {
                    EmployeeName = employeeName,
                    HealthIssuesCount = healthIssuesCount,
                    FamilyResponsibilitiesCount = familyResponsibilitiesCount,
                    VacationCount = vacationCount,
                    LegalObligationsCount = legalObligationsCount,
                    EmergencySituationsCount = emergencySituationsCount,
                    OtherReasonsCount = otherReasonsCount
                };

                // Create JSON result with leave reason counts
                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }
        // Splitting the employeeId to get the type
        #region payrollfunctions
        public ActionResult GetPayrollData()
        {
            // Retrieve payroll data from the database
            var payrollData = _dbContext.Payroll.ToList();

            // Group payroll data by month and year
            var groupedPayroll = payrollData.GroupBy(p => new { p.Year, p.Month })
                                            .Select(g => new
                                            {
                                                Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                                                Year = g.Key.Year,
                                                TotalSalary = g.Sum(p => p.TotalSalary)
                                            });

            // Extract months, years, and total salaries
            var months = groupedPayroll.Select(p => p.Month).ToList();
            var years = groupedPayroll.Select(p => p.Year).ToList();
            var totalSalaries = groupedPayroll.Select(p => p.TotalSalary).ToList();

            // Create an anonymous object to hold the data
            var jsonData = new
            {
                Months = months,
                Years = years,
                TotalSalaries = totalSalaries
            };

            // Serialize the data to JSON format
            var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

            // Return JSON result
            return jsonResult;
        }
        public ActionResult GetPayrollByMonth(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Get payroll data for all employees for each month
                var monthlyData = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the payroll data for all employees in the current month
                    var payrollDataForMonth = _dbContext.Payroll
                        .Where(p => p.Month == month)
                        .ToList();

                    // Calculate total salary and bonus for the month
                    decimal totalSalary = payrollDataForMonth.Sum(p => p.TotalSalary);
                    decimal totalBonus = payrollDataForMonth.Sum(p => p.Bonus ?? 0);

                    // Create an anonymous object to hold the payroll details for this month
                    var monthData = new
                    {
                        Month = month,
                        TotalSalary = totalSalary,
                        TotalBonus = totalBonus,
                        PayrollDetails = payrollDataForMonth
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
            else
            {
                // Splitting the employeeId to get the type
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive&&e.isLabour==true);
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                var monthlyData = new List<object>();
                var employeesId = (employee != null) ? employee.Id : null;
                var labourId = (labour != null) ? labour.Id : null;
                for (int month = 1; month <= 12; month++)
                {
                    // Get the payroll data for the employee/labour in the current month
                    int currentMonth = month; // Capture the month variable in a local variable
                    var currentEmployeesId = employeesId;
                    var currentLabourId = labourId;

                    // Get the payroll data for the employee/labour in the current month
                    var payrollDataForMonth = _dbContext.Payroll
     .Where(p =>
         ((currentEmployeesId != null && p.employeeId == currentEmployeesId) ||
         (!string.IsNullOrEmpty(currentLabourId) && p.labourId == currentLabourId))
         && p.Month == currentMonth)
     .ToList();


                    // Calculate total salary and bonus for the month
                    decimal totalSalary = payrollDataForMonth.Sum(p => p.TotalSalary);
                    decimal totalBonus = payrollDataForMonth.Sum(p => p.Bonus ?? 0);

                    // Create an anonymous object to hold the payroll details for this month
                    var monthData = new
                    {
                        Month = month,
                        EmployeeId = employeeId,
                        EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown",
                        TotalSalary = totalSalary,
                        TotalBonus = totalBonus,
                        PayrollDetails = payrollDataForMonth
                    };

                    monthlyData.Add(monthData);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlyData, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
        }

        #endregion




        #region Summary
        public ActionResult GetSummaryByMonth(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId) || employeeId == "Cactus-")
            {
                // Initialize monthly summary data
                var monthlySummary = new List<object>();

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the payroll data for the month
                    var payrollDataForMonth = _dbContext.Payroll
                        .Where(p => p.Month == month)
                        .ToList();

                    // Get the leave data for the month
                    var leaveDataForMonth = _dbContext.receivedLeaveRequests
                        .Where(l => l.Date != null && l.Date.Value.Month == month)
                        .ToList();

                    // Get the attendance data for the month (assuming there's a corresponding model)
                    var attendanceDataForMonth = _dbContext.Attendance
                        .Where(a => a.date != null && a.date.Month == month)
                        .ToList();

                    // Calculate total salary and bonus for the month
                    decimal totalSalary = payrollDataForMonth.Sum(p => p.TotalSalary);
                    decimal totalBonus = payrollDataForMonth.Sum(p => p.Bonus ?? 0);

                    // Count the number of leaves for the month
                    var totalLeaves = leaveDataForMonth.Count;

                    // Calculate total attendance count for the month
                    var totalAttendanceCount = attendanceDataForMonth.Count;

                    // Create an anonymous object to hold the summary details for this month
                    var monthSummary = new
                    {
                        Month = month,
                        TotalSalary = totalSalary,
                        TotalBonus = totalBonus,
                        TotalLeaves = totalLeaves,
                        TotalAttendanceCount = totalAttendanceCount
                    };

                    monthlySummary.Add(monthSummary);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlySummary, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
            else
            {
                // Splitting the employeeId to get the type
                var employeeIdParts = employeeId.Split('-');

                // Check if the employeeId format is valid
                if (employeeIdParts.Length != 3)
                {
                    return Json(new { error = "Invalid employeeId format" }, JsonRequestBehavior.AllowGet);
                }

                // Retrieving the employee from the database based on the type
                var employeeType = employeeIdParts[1];
                employeeType = employeeType.ToLower();
                ApplicationUser employee = null;
                ApplicationUser labour = null;
                if (employeeType == "la")
                {
                    labour = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive&&e.isLabour==true);
                }
                else if (employeeType == "em")
                {
                    employee = _dbContext.Users.FirstOrDefault(e => e.employeeNumber == employeeId && e.isActive);
                }

                // Check if either employee or labour is null
                if (employee == null && labour == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                // Initialize monthly summary data
                var monthlySummary = new List<object>();

                var employeesId = (employee != null) ? employee.Id : null;
                var labourId = (labour != null) ? labour.Id : null;

                // Iterate over each month
                for (int month = 1; month <= 12; month++)
                {
                    // Get the payroll data for the employee/labour in the current month
                    int currentMonth = month; // Capture the month variable in a local variable
                    var currentEmployeesId = employeesId;
                    var currentLabourId = labourId;

                    // Get the payroll data for the month
                    var payrollDataForMonth = _dbContext.Payroll
     .Where(p =>
         ((currentEmployeesId != null && p.employeeId == currentEmployeesId) ||
         (!string.IsNullOrEmpty(currentLabourId) && p.labourId == currentLabourId))
         && p.Month == currentMonth)
     .ToList();


                    // Get the leave data for the employee/labour in the current month
                    var leaveDataForMonth = _dbContext.receivedLeaveRequests
        .Where(l =>
            ((currentEmployeesId != null && l.employeeId == currentEmployeesId) ||
            (!string.IsNullOrEmpty(currentLabourId) && l.labourId == currentLabourId))
            && l.Date != null && l.Date.Value.Month == currentMonth)
        .ToList();



                    // Get the attendance data for the employee/labour in the current month (assuming there's a corresponding model)
                    var attendanceDataForMonth = _dbContext.Attendance
                        .Where(a =>
                            ((currentEmployeesId != null && a.employeeId == currentEmployeesId) ||
                            (!string.IsNullOrEmpty(currentLabourId) && a.labourId == currentLabourId))
                            && a.date != null && a.date.Month == currentMonth)
                        .ToList();

                    // Calculate total salary and bonus for the month
                    decimal totalSalary = payrollDataForMonth.Sum(p => p.TotalSalary);
                    decimal totalBonus = payrollDataForMonth.Sum(p => p.Bonus ?? 0);

                    // Count the number of leaves for the month
                    var totalLeaves = leaveDataForMonth.Count;

                    // Calculate total attendance count for the month
                    var totalAttendanceCount = attendanceDataForMonth.Count;

                    // Create an anonymous object to hold the summary details for this month
                    var monthSummary = new
                    {
                        Month = month,
                        EmployeeId = employeeId,
                        EmployeeName = (employee != null) ? employee.FirstName + " " + employee.LastName : (labour != null) ? labour.FirstName + " " + labour.LastName : "Unknown",
                        TotalSalary = totalSalary,
                        TotalBonus = totalBonus,
                        TotalLeaves = totalLeaves,
                        TotalAttendanceCount = totalAttendanceCount
                    };

                    monthlySummary.Add(monthSummary);
                }

                // Serialize the data to JSON format
                var jsonResult = Json(monthlySummary, JsonRequestBehavior.AllowGet);

                // Return JSON result
                return jsonResult;
            }
        }

        #endregion
    }
}