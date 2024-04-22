using AMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO.Ports;
using System.Linq;
using System.Threading;
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
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();
            data = data.Replace("\r\n", "");

            // Check if the received data is not null or empty before processing
            if (!string.IsNullOrEmpty(data))
            {
                // Query the database to check if the received card data exists and is active
                var cardEmployee = (from card in _dbContext.Cards
                                    join employee in _dbContext.Users on card.Id equals employee.CardId
                                    where card.cardCode == data && card.isActive == true && employee.isActive == true
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

                        // Send response back to Arduino
                        sp.Write("Attendance marked successfully!");
                    }
                    else
                    {
                        // Employee is checking in for the first time today, mark time in
                        Attendance attendance = new Attendance
                        {
                            date = DateTime.Now.Date,
                            timeIn = DateTime.Now.TimeOfDay,
                            timeOut = TimeSpan.Zero,
                            employeeId = employeeId,
                            totalWorkedTime = null // Assuming initial value
                        };
                        _dbContext.Attendance.Add(attendance);
                        _dbContext.SaveChanges();

                        // Send response back to Arduino
                        sp.Write("Attendance marked successfully!");
                    }
                }
                else
                {
                    // Send response back to Arduino
                    sp.Write("Attendance marking failed!");
                    // Handle the case accordingly
                }
            }
            else
            {
                // Send response back to Arduino
                sp.Write("No data received.");
            }
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