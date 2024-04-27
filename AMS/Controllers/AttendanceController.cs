using AMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult AttendanceForLabours()
        {
            return View();
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
            var attList = _dbContext.Attendance.Include(c => c.ApplicationUser).AsNoTracking().ToList();

            // Format timeIn and timeOut values to string representation
            var formattedAttList = attList.Select(att => new
            {
                Id = att.Id,
                ApplicationUser = att.ApplicationUser,
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
                        TimeSpan workedHours = existingAttendance.timeOut - existingAttendance.timeIn;
                        // Calculate total worked hours and minutes
                        int totalWorkedHours = (int)workedHours.TotalHours;
                        int totalWorkedMinutes = workedHours.Minutes;

                        // Format the total worked hours and minutes
                        string formattedTotalWorkedTime = $"{totalWorkedHours} Hours {totalWorkedMinutes} Minutes";

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
                        };
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
            double totalHoursWorked = 0;
            int totalMinutesWorked = 0;
            decimal totalSalary = 0;

            // Assuming hourly rate is $10 for simplicity
            decimal hourlyRate = _dbContext.Users.Where(e => e.Id == employeeId).Select(e => e.perHour).FirstOrDefault();

            // Calculate total hours worked and total minutes worked
            foreach (var attendance in attendances)
            {
                // Calculate total time worked for this attendance record
                TimeSpan workedTime = attendance.timeOut - attendance.timeIn;

                // Add total hours and minutes worked
                totalHoursWorked += workedTime.TotalHours;
                totalMinutesWorked += workedTime.Minutes;
            }

            // Convert total minutes to hours
            totalHoursWorked += totalMinutesWorked / 60.0;

            // Calculate total salary based on total hours worked
            totalSalary = (decimal)totalHoursWorked * hourlyRate;

            // Format totalSalary to two decimal places
            totalSalary = Math.Round(totalSalary, 2);

            // Format totalHoursWorked to two decimal places
            totalHoursWorked = Math.Round(totalHoursWorked, 2);

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
                    employeeId = employeeId
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
    }
}