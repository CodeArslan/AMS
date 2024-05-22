using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using MimeKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MailKit;
using MailKit.Search;
using AMS.Models;
using System.Configuration;
using static AMS.Controllers.LeaveController;
using System.Web.Helpers;
using static System.Data.Entity.Infrastructure.Design.Executor;
using System.Data.Entity;
using System.Threading.Tasks;
using AMS.ViewModels;
using Microsoft.AspNet.Identity;
using System.Web.Configuration;
using System.Globalization;

namespace AMS.Controllers
{
    public class LeaveController : Controller
    {
        private ApplicationDbContext _dbContext;
        public LeaveController()
        {
            _dbContext = new ApplicationDbContext();
           
        }

        public async Task<ActionResult> GetLeaveData()
        {
            var leaveList = await _dbContext.LeaveResponses
                   .Include(l => l.ReceivedLeaveRequests)
                   .Include(l => l.ReceivedLeaveRequests.ApplicationUser)
                   .Include(l => l.ReceivedLeaveRequests.Labour)
                   .AsNoTracking()
                   .ToListAsync(); 
            return Json(leaveList, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetLeaveDataByUser()
        {
            var loggedInUser = User.Identity.GetUserId();
            var leaveList = await _dbContext.LeaveResponses
                   .Include(l => l.ReceivedLeaveRequests)
                   .Include(l => l.ReceivedLeaveRequests.ApplicationUser)
                   .Include(l => l.ReceivedLeaveRequests.Labour).Where(l=>l.ReceivedLeaveRequests.employeeId == loggedInUser||l.ReceivedLeaveRequests.labourId==loggedInUser)
                   .AsNoTracking().ToListAsync();
            return Json(leaveList, JsonRequestBehavior.AllowGet);
        }

        // GET: Leave
        [Authorize(Roles = "Admin")]

        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Employee,HR,Labour,Admin")]
        public ActionResult EmployeeLeave()
        {
            return View();
        }
        public ActionResult getLeaveBalanceByUser()
        {
            var loggedInUser = User.Identity.GetUserId();
            var leaveCount = _dbContext.Users.Where(e => e.Id == loggedInUser).Select(e => e.leaveBalance).FirstOrDefault();
            return Json(leaveCount, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLeaveBalance(string Email)
        {
            var user = _dbContext.Users.Where(u => u.Email == Email).FirstOrDefault();
            if (user != null)
            {
                // Employee found, return their data
                return Json(new { success = true, user=user },JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Employee not found, return appropriate response
                return Json(new { success = false, message = "Employee not found." });
            }
        }

        public ActionResult getLastLeaveDetails()
        {
            var loggedInUser = User.Identity.GetUserId();
            var latestLeave = _dbContext.receivedLeaveRequests
                                .Where(e => e.employeeId == loggedInUser && (e.Decision == "Approved" || e.Decision == "Rejected"))
                                .OrderByDescending(e => e.Date)
                                .FirstOrDefault();


            // Create JSON object with all details
            var leaveDetails = new
            {
                Date = latestLeave != null ? (DateTime?)latestLeave.Date : null,
                Decision = latestLeave?.Decision, 
            };

            // Return leave details in JSON format
            return Json(leaveDetails, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LeavesByMonthByUser()
        {
            var loggedInUser = User.Identity.GetUserId();

            var allMonths = Enumerable.Range(1, 12); // List of all months (1 to 12)

            var leavesByMonth = _dbContext.receivedLeaveRequests
                .Where(l => l.Date.HasValue && l.employeeId==loggedInUser) // Only consider leaves with a valid date
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

        public ActionResult Inbox()
        {
            //ReceiveUnreadEmailsFromGmail();
            return View();

        }

      
        [HttpGet]
        public JsonResult GetInboxCount()
        {
            //ReceiveUnreadEmailsFromGmail();
            var chats = _dbContext.receivedLeaveRequests.Where(c=>c.isRead==false && c.Subject!=null).ToList(); 
            return Json(chats.Count(), JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetChatData()
        {
            try
            {
                ReceiveUnreadEmailsFromGmail();
                var chatList = _dbContext.receivedLeaveRequests.Where(c=>c.Subject!=null).OrderByDescending(c => c.Date);
                return Json(chatList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred while fetching chat data: " + ex.Message;
                return Json(new { error = errorMessage });
            }
        }
        [HttpGet]
        public JsonResult GetChatMessage(int chatId)
        {
            // Assuming you have a ChatMessage model and DbContext setup
            var chatMessage = _dbContext.receivedLeaveRequests.FirstOrDefault(c => c.Id == chatId);
            if (chatMessage != null)
            {
                var leaveCount = _dbContext.Users
                                    .Where(l => l.Email == chatMessage.From)
                                    .Select(l => l.leaveBalance)
                                    .FirstOrDefault();

                var responseData = new
                {
                    ChatMessage = chatMessage,
                    LeaveCount = leaveCount
                };

                return Json(responseData, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }
        public class EmailData
        {
            public string Subject { get; set; }
            public string Sender { get; set; }
            public string Body { get; set; }
        }
        public EmailData ParseEmail(MimeMessage message)
        {
            var emailData = new EmailData
            {
                Subject = message.Subject,
                Sender = message.From.Mailboxes.FirstOrDefault()?.Address
            };

            if (message.TextBody != null)
            {
                emailData.Body = message.TextBody;
            }
            else if (message.HtmlBody != null)
            {
                emailData.Body = message.HtmlBody;
            }
            return emailData;
        }

        public void ReceiveUnreadEmailsFromGmail()
        {
            using (var client = new ImapClient())
            {
                client.Connect("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
                client.Authenticate(ConfigurationManager.AppSettings["Email"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());

                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadWrite);
                string specificSubject = "Leave Request";

                var searchQuery = SearchQuery.NotSeen.And(SearchQuery.SubjectContains(specificSubject));

                var uids = inbox.Search(searchQuery);
                foreach (var uid in uids)
                {
                    var message = inbox.GetMessage(uid);
                    var subject = message.Subject;

                    if (subject.Contains(specificSubject))
                    {
                        var emailData = ParseEmail(message);
                        if (emailData.Body != null) 
                        {
                            var isEmailRegisteredInUsers = _dbContext.Users.FirstOrDefault(c => c.Email == emailData.Sender);
                                string name = "";
                                if (isEmailRegisteredInUsers != null)
                                {
                                    name = isEmailRegisteredInUsers.FirstName + " " + isEmailRegisteredInUsers.LastName;
                                }
                                var ReceivedEmailRequests = new ReceivedLeaveRequests
                                {
                                    From = emailData.Sender,
                                    Subject = emailData.Subject,
                                    Message = emailData.Body,
                                    Name = name,
                                    Reason = "No"
                                };

                                // Add the object to the receivedLeaveRequests DbSet and save changes to the database
                                _dbContext.receivedLeaveRequests.Add(ReceivedEmailRequests);
                                _dbContext.SaveChanges();

                                // Mark the email as seen
                                inbox.AddFlags(uid, MessageFlags.Seen, true);
                            
                        }



                    }
                }
                client.Disconnect(true);
            }
        }
        public ActionResult AddLeave(LeaveResponse response)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (response.Id == 0)
                    {
                        var fromDate = response.FromDate;
                        var toDate = response.ToDate;
                        var message = response.Message;
                        var decision = response.Decision;
                        var Email = _dbContext.receivedLeaveRequests.Where(e => e.Id == response.rlrId).FirstOrDefault();

                        if (fromDate != null && toDate != null)
                        {
                            var days = (toDate.Value - fromDate.Value).Days;
                            if (toDate.Value == fromDate.Value)
                            {
                                days = 1; // Count as 1 day if the dates are the same
                            }

                            var employee = _dbContext.Users.Where(l => l.Email == Email.From && l.isLabour == false).FirstOrDefault();
                            var labour = _dbContext.Users.Where(l => l.Email == Email.From && l.isLabour == true).FirstOrDefault();

                            if (employee != null || labour != null)
                            {
                                // Check if there is already a leave request for the same date span
                                bool leaveExists = _dbContext.LeaveResponses
                                    .Any(lr => lr.FromDate <= toDate && lr.ToDate >= fromDate && lr.ReceivedLeaveRequests.From == Email.From);

                                if (leaveExists)
                                {
                                    return Json(new { success = false, message = "Leave for this date span already exists." });
                                }

                                var leaveBalance = (employee != null) ? employee.leaveBalance : labour.leaveBalance;

                                if (employee != null)
                                {
                                    employee.leaveBalance -= days;
                                    Email.employeeId = employee.Id;
                                }
                                else
                                {
                                    labour.leaveBalance -= days;
                                    Email.labourId = labour.Id;
                                }

                                Email.Reason = response.ReceivedLeaveRequests.Reason;
                                _dbContext.SaveChanges();

                                sendLeaveEmail(fromDate, toDate, message, Email.From, decision, leaveBalance);
                                Email.Decision = response.Decision;
                                Email.isRead = true;
                                response.ReceivedLeaveRequests = null;
                                _dbContext.LeaveResponses.Add(response);
                                _dbContext.SaveChanges();

                                return Json(new { success = true, message = "Leave Sent Successfully." });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }

            return View(response);
        }
        public ActionResult sendLeaveEmail(DateTime? from, DateTime? to, string message, string email, string decision, int newLeaveBalance)
        {
            try
            {
                string senderEmail = ConfigurationManager.AppSettings["Email"].ToString(); // Sender's email address
                string senderPassword = ConfigurationManager.AppSettings["Password"].ToString(); // Sender's email password
                string smtpHost = "smtp.gmail.com";
                int smtpPort = 587;
                bool enableSSL = true;

                // Create a new MailMessage instance
                MailMessage mail = new MailMessage();

                // Set sender and recipient email addresses
                mail.From = new MailAddress(senderEmail);
                mail.To.Add(email);
                string body;
                int finalLeaveBalance=0;
                int totalDays=0;
                if (from!=null&&to!=null)
                {
                    // Calculate total days of leave
                     totalDays = (to.Value - from.Value).Days;

                    // Calculate final leave balance
                     finalLeaveBalance = newLeaveBalance - totalDays;
                }    
             
                // Set email subject based on decision
                if (decision.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    mail.Subject = "Leave Application Approved";
                    body = $"Your leave has been approved from {from:d} to {to:d} for {totalDays} days.\nYour new leave balance is {finalLeaveBalance}.";
                    mail.Body = body;

                }
                else if (decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    mail.Subject = "Leave Application Rejected";
                    body = $"Unfortunately! Your leave application has been rejected.\nYour current leave balance is {newLeaveBalance}.";
                    mail.Body = body;

                }



                // Create SMTP client
                SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort);
                smtpClient.EnableSsl = enableSSL;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);

                // Send the email
                smtpClient.Send(mail);

                // Optionally, return a success message
                return Content("Email sent successfully!");
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return Content("Error: " + ex.Message);
            }
        }
        public ActionResult LeaveBalanceReferesh()
        {
            if(DateTime.Today.Day==1)
            {
                var users =  _dbContext.Users.ToList();
                foreach (var user in users)
                {
                    user.leaveBalance += 2;
                }
                _dbContext.SaveChanges();
            }
            return Content("");
        }
        public ActionResult Delete(int id)
        {
            var leaveResponse = _dbContext.LeaveResponses.SingleOrDefault(lr => lr.Id == id);
            if (leaveResponse == null)
            {
                return Json(new { success = false, message = "Leave Response Record Does Not Exist" });
            }

            var leaveRequest = _dbContext.receivedLeaveRequests.FirstOrDefault(rlr => rlr.Id == leaveResponse.rlrId);
            if (leaveRequest == null)
            {
                return Json(new { success = false, message = "Associated Leave Request Record Not Found" });
            }
            int leaveDays = (leaveResponse.ToDate.Value - leaveResponse.FromDate.Value).Days;
            if(leaveResponse.ToDate.Value== leaveResponse.FromDate.Value)
            {
                leaveDays = 1;
            }
            var user = leaveRequest.employeeId != null ? _dbContext.Users.FirstOrDefault(u => u.Id.ToString() == leaveRequest.employeeId) :
                                                              _dbContext.Users.FirstOrDefault(u => u.Id.ToString() == leaveRequest.labourId);

            if (user != null)
            {
                // Update leave balance
                user.leaveBalance += leaveDays;
                _dbContext.SaveChanges();
            }

            // Remove leave response
            _dbContext.LeaveResponses.Remove(leaveResponse);
            _dbContext.SaveChanges();

            // Remove leave request
            _dbContext.receivedLeaveRequests.Remove(leaveRequest);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Leave Record Successfully Deleted. Leave Balance Updated." });
        }
        public ActionResult LeaveDetails(LeaveResponseViewModel viewModel)
        {
            try
            {
                var employee = _dbContext.Users.FirstOrDefault(u => u.Email == viewModel.ReceivedLeaveRequests.From && u.isLabour == false);
                var labour = _dbContext.Users.FirstOrDefault(u => u.Email == viewModel.ReceivedLeaveRequests.From && u.isLabour == true);

                if (employee != null || labour != null)
                {
                    var fromDate = viewModel.LeaveResponse.FromDate;
                    var toDate = viewModel.LeaveResponse.ToDate;

                    // Check if there is already a leave request for the same date span
                    bool leaveExists = _dbContext.LeaveResponses
                        .Any(lr => lr.FromDate <= toDate && lr.ToDate >= fromDate && lr.ReceivedLeaveRequests.From == viewModel.ReceivedLeaveRequests.From);

                    if (leaveExists)
                    {
                        return Json(new { success = false, message = "Leave for this date span already exists." });
                    }

                    var receivedleaveRequests = new ReceivedLeaveRequests()
                    {
                        From = viewModel.ReceivedLeaveRequests.From,
                        Decision = "Approved",
                        Message = viewModel.ReceivedLeaveRequests.Message,
                        Name = viewModel.ReceivedLeaveRequests.Name,
                        employeeId = employee != null ? employee.Id.ToString() : null,
                        labourId = labour != null ? labour.Id.ToString() : null,
                        Reason = viewModel.ReceivedLeaveRequests.Reason,
                    };

                    _dbContext.receivedLeaveRequests.Add(receivedleaveRequests);
                    _dbContext.SaveChanges();
                    var fkrlr = receivedleaveRequests.Id;

                    var leaveResponse = new LeaveResponse()
                    {
                        ToDate = viewModel.LeaveResponse.ToDate,
                        FromDate = viewModel.LeaveResponse.FromDate,
                        Decision = "Approved",
                        rlrId = fkrlr,
                    };

                    _dbContext.LeaveResponses.Add(leaveResponse);
                    _dbContext.SaveChanges();
                    int leaveDays = (viewModel.LeaveResponse.ToDate.Value - viewModel.LeaveResponse.FromDate.Value).Days;
                    if (viewModel.LeaveResponse.ToDate.Value == viewModel.LeaveResponse.FromDate.Value)
                    {
                        leaveDays = 1;
                    }
                    if (employee != null)
                    {
                        employee.leaveBalance -= leaveDays;
                    }
                    else if (labour != null)
                    {
                        labour.leaveBalance -= leaveDays;
                    }

                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Leave added successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Error Occured In Adding Leave" });
                }

            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while processing your request. Please try again!" });
            }
        }

    }
}