﻿using AMS.Models;
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
                        results.Add("Attendance marked successfully!");
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
                        results.Add("Attendance marked successfully!");
                    }
                }
                else
                {
                    // Add result to the list
                    results.Add("Attendance marking failed!");
                    // Handle the case accordingly
                }
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