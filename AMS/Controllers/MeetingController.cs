using AMS.Models;
using AMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
namespace AMS.Controllers
{
    [Authorize(Roles = "Admin")]

    public class MeetingController : Controller
    {
        // GET: Meeting
        private ApplicationDbContext _dbContext;
        public MeetingController()
        {
            _dbContext = new ApplicationDbContext();
        }
        public ActionResult Index(int? id)
        {
            var departmentList = _dbContext.Departments.Where(d => d.isActive == true).ToList();
            var viewModel = new EmployeeMeetingViewModel
            {
                Departments = departmentList,
                Meeting=new Meeting(),
            };

            if (id != null)
            {
                // Retrieve meeting and related employeeHasMeeting data based on the provided id
                var employeeMeeting = _dbContext.employeeHasMeetings
                                                .Include(ehm => ehm.Meeting)
                                                .Include(ehm => ehm.ApplicationUser)
                                                .Where(ehm => ehm.Meeting.Id == id)
                                                .ToList();

                // Add retrieved data to the viewModel
                viewModel.Meeting = employeeMeeting.Select(e=>e.Meeting).FirstOrDefault();
                viewModel.EmployeeHasMeeting = employeeMeeting.FirstOrDefault();
                viewModel.EmployeeIds = string.Join(",", employeeMeeting.Select(ehm => ehm.ApplicationUser.Id));
                viewModel.User = employeeMeeting.Select(ehm => ehm.ApplicationUser).ToList();
            }

            return View(viewModel);
        }

        public ActionResult MeetingList()
        {
            var meetingList = _dbContext.Meetings.ToList();
            return View(meetingList);
        }
        public ActionResult filteredMeeting(string status)
        {
            IQueryable<Meeting> filteredOrders = _dbContext.Meetings;

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredOrders = filteredOrders.Where(o => o.Status == status);
            }
            var result = filteredOrders.ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }



        public ActionResult Create(EmployeeMeetingViewModel viewModel)
        {
            string[] attendeeIds = viewModel.EmployeeIds.Split(',');
            if (ModelState.IsValid)
            {
                // Check if this is a new meeting or an existing one
                if (viewModel.Meeting.Id == 0)
                {
                    // Create a new meeting
                    Meeting newMeeting = new Meeting
                    {
                        Date = viewModel.Meeting.Date,
                        StartTime = viewModel.Meeting.StartTime,
                        EndTime = viewModel.Meeting.EndTime,
                        Agenda = viewModel.Meeting.Agenda,
                        Location = viewModel.Meeting.Location,
                        Status = viewModel.Meeting.Status
                    };

                    _dbContext.Meetings.Add(newMeeting);
                    _dbContext.SaveChanges();
                    foreach (string employeeId in attendeeIds)
                    {
                        EmployeeHasMeeting employeeMeeting = new EmployeeHasMeeting
                        {
                            meetingId = newMeeting.Id,
                            employeeId = employeeId
                        };

                        _dbContext.employeeHasMeetings.Add(employeeMeeting);
                    }

                    _dbContext.SaveChanges();

                    // Send email to attendees about the new meeting
                    var attendees = _dbContext.Users.Where(e => attendeeIds.Contains(e.Id)).ToList();
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(ConfigurationManager.AppSettings["Email"].ToString());
                    foreach (var attendee in attendees)
                    {
                        mail.To.Add(attendee.Email);
                    }

                    mail.Subject = "New Meeting Details";
                    string meetingDate = newMeeting.Date.ToString("dd MMM, yyyy");
                    mail.Body = $"Hello,\n\nYou are invited to a new meeting.\n\nDate: {meetingDate}\nTime: {newMeeting.StartTime} - {newMeeting.EndTime}\nAgenda: {newMeeting.Agenda}\nLocation: {newMeeting.Location}\n\nBest regards,\nCactus General Transport";

                    SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                    smtp.Port = 587;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Email"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                    smtp.EnableSsl = true;

                    try
                    {
                        smtp.Send(mail);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                    }

                    return Json(new { success = true, message = "Meeting created successfully." });
                }
                else
                {
                    // Update existing meeting
                    Meeting existingMeeting = _dbContext.Meetings.FirstOrDefault(m => m.Id == viewModel.Meeting.Id);

                    if (existingMeeting != null)
                    {
                        // Check if any details other than status are being updated
                        if (existingMeeting.Date != viewModel.Meeting.Date ||
                            existingMeeting.StartTime != viewModel.Meeting.StartTime ||
                            existingMeeting.EndTime != viewModel.Meeting.EndTime ||
                            existingMeeting.Agenda != viewModel.Meeting.Agenda ||
                            existingMeeting.Location != viewModel.Meeting.Location)
                        {
                            // If details other than status are being updated
                            existingMeeting.Date = viewModel.Meeting.Date;
                            existingMeeting.StartTime = viewModel.Meeting.StartTime;
                            existingMeeting.EndTime = viewModel.Meeting.EndTime;
                            existingMeeting.Agenda = viewModel.Meeting.Agenda;
                            existingMeeting.Location = viewModel.Meeting.Location;
                            existingMeeting.Status = viewModel.Meeting.Status;

                            _dbContext.SaveChanges();

                            // Send email to attendees with updated details
                            var attendees = _dbContext.Users.Where(e => attendeeIds.Contains(e.Id)).ToList();
                            MailMessage mail = new MailMessage();
                            mail.From = new MailAddress(ConfigurationManager.AppSettings["Email"].ToString());
                            foreach (var attendee in attendees)
                            {
                                mail.To.Add(attendee.Email);
                            }

                            mail.Subject = "Updated Meeting Details";
                            string prevMeetingDate = existingMeeting.Date.ToString("dd MMM, yyyy");
                            string newMeetingDate = viewModel.Meeting.Date.ToString("dd MMM, yyyy");
                            TimeSpan newStartTime = viewModel.Meeting.StartTime;
                            TimeSpan newEndTime = viewModel.Meeting.EndTime;
                            mail.Body = $"Hello,\n\nThe meeting details have been updated.\n\nPrevious Date: {prevMeetingDate}\nNew Date: {newMeetingDate}\nPrevious Time: {existingMeeting.StartTime} - {existingMeeting.EndTime}\nNew Time: {newStartTime} - {newEndTime}\nPrevious Agenda: {existingMeeting.Agenda}\nNew Agenda: {viewModel.Meeting.Agenda}\nPrevious Location: {existingMeeting.Location}\nNew Location: {viewModel.Meeting.Location}\n\nBest regards,\nCactus General Transport";

                            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                            smtp.Port = 587;
                            smtp.UseDefaultCredentials = false;
                            smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Email"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                            smtp.EnableSsl = true;

                            try
                            {
                                smtp.Send(mail);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error sending email: {ex.Message}");
                            }

                            return Json(new { success = true, message = "Meeting Details Updated successfully." });
                        }
                        else if (existingMeeting.Status != viewModel.Meeting.Status)
                        {
                            // If only status is being updated
                            existingMeeting.Status = viewModel.Meeting.Status;
                            _dbContext.SaveChanges();
                            return Json(new { success = true, message = "Meeting Status Changed Successfully." });
                        }
                        else
                        {
                            return Json(new { success = true, message = "No Changes Made." });
                        }
                    }
                }
            }

            return View(viewModel);
        }

    }
}