﻿using System;
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
            var leaveList = await _dbContext.receivedLeaveRequests.AsNoTracking().ToListAsync();
            return Json(leaveList, JsonRequestBehavior.AllowGet);
        }

        // GET: Leave
        public ActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public JsonResult GetLeaveBalance(string Email)
        {
            var user = _dbContext.Users.Where(u => u.Email == Email).FirstOrDefault();
            var labour=_dbContext.Labours.Where(l=>l.Email == Email).FirstOrDefault();
            if (user != null)
            {
                // Employee found, return their data
                return Json(new { success = true, user=user },JsonRequestBehavior.AllowGet);
            }
            else if (labour!=null)
            {
                return Json(new { success = true, user = labour }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Employee not found, return appropriate response
                return Json(new { success = false, message = "Employee not found." });
            }
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
                        if (emailData.Body != null) // Check if message body is not null
                        {
                            var isEmailRegistered = _dbContext.Users.FirstOrDefault(c => c.Email == emailData.Sender);
                            if (isEmailRegistered != null)
                            {
                                var name = isEmailRegistered.FirstName + " " + isEmailRegistered.LastName;
                                var ReceivedEmailRequests = new ReceivedLeaveRequests
                                {
                                    From = emailData.Sender,
                                    Subject = emailData.Subject,
                                    Message = emailData.Body,
                                    Name = name,
                                    Reason="No"
                                };
                                _dbContext.receivedLeaveRequests.Add(ReceivedEmailRequests);
                                _dbContext.SaveChanges();
                                inbox.AddFlags(uid, MessageFlags.Seen, true);
                            }
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
                        var fromDate = response.From;
                        var toDate = response.To;

                       
                        var message = response.Message;
                        var decision = response.Decision;
                        var Email = _dbContext.receivedLeaveRequests.Where(e => e.Id == response.rlrId).FirstOrDefault();
                        var leaveBalance = 0;
                        if(fromDate!= null&&toDate!=null) {
                            var days = (toDate.Value - fromDate.Value).Days;
                            if (toDate.Value == fromDate.Value)
                            {
                                days = 1; // Sirf 1 count karein
                            }
                            var employee = _dbContext.Users.Where(l => l.Email == Email.From).FirstOrDefault();
                            var labour=_dbContext.Labours.Where(l=>l.Email==Email.From).FirstOrDefault();
                            if(employee!=null)
                            {
                                leaveBalance = employee.leaveBalance;
                                employee.leaveBalance = employee.leaveBalance - days;
                                Email.employeeId = employee.Id;
                                Email.Reason = response.ReceivedLeaveRequests.Reason;
                                _dbContext.SaveChanges();
                            }
                            else
                            {
                                leaveBalance = labour.leaveBalance;
                                labour.leaveBalance = labour.leaveBalance - days;
                                Email.labourId = labour.Id;
                                Email.Reason = response.ReceivedLeaveRequests.Reason;
                                _dbContext.SaveChanges();
                            }
                           
                        }
                        sendLeaveEmail(fromDate, toDate, message, Email.From,decision, leaveBalance);
                        Email.Decision = response.Decision;
                        Email.isRead = true;
                        response.ReceivedLeaveRequests = null;
                        _dbContext.LeaveResponses.Add(response);
                        _dbContext.SaveChanges();
                        return Json(new { success = true, message = "Leave Sent Successfully." });
                    }
                }
            }
            catch(Exception)
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
                var labours=_dbContext.Labours.ToList();
                foreach (var user in users)
                {
                    user.leaveBalance += 2;
                }
                foreach(var labour in labours)
                {
                    labour.leaveBalance += 2;
                }
                _dbContext.SaveChanges();
            }
            return Content("");
        }
        public ActionResult Delete(int id)
        {
            var leaveInDb = _dbContext.receivedLeaveRequests.SingleOrDefault(c => c.Id == id);
            if (leaveInDb == null)
            {
                return Json(new { success = false, message = "Leave Record Doesnot Found" });
            }

            else
            {
                _dbContext.receivedLeaveRequests.Remove(leaveInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Leave Record Successfully Deleted" });
            }
        }
        public ActionResult LeaveDetails(LeaveResponseViewModel viewModel)
        {
            try
            {
                var employee = _dbContext.Users.FirstOrDefault(u => u.Email == viewModel.ReceivedLeaveRequests.From);
                var labour = _dbContext.Labours.FirstOrDefault(u => u.Email == viewModel.ReceivedLeaveRequests.From);

                if (employee != null || labour != null)
                {
                    var receivedleaveRequests = new ReceivedLeaveRequests()
                    {
                        From = viewModel.ReceivedLeaveRequests.From,
                        Decision = "Approved",
                        Message = viewModel.ReceivedLeaveRequests.Message,
                        Name = viewModel.ReceivedLeaveRequests.Name,
                        employeeId = employee != null ? employee.Id.ToString() : null,
                        labourId = labour != null ? labour.Id : (int?)null,
                        Reason = viewModel.ReceivedLeaveRequests.Reason,
                    };

                    _dbContext.receivedLeaveRequests.Add(receivedleaveRequests);
                    _dbContext.SaveChanges();
                    var fkrlr = receivedleaveRequests.Id;

                    var leaveResponse = new LeaveResponse()
                    {
                        To = viewModel.LeaveResponse.To,
                        From = viewModel.LeaveResponse.From,
                        Decision = "Approved",
                        rlrId = fkrlr,
                    };

                    _dbContext.LeaveResponses.Add(leaveResponse);
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
                return Json(new { success = false, message = "An error occured while processing your request. Please try again!" });
            }

        }

    }
}