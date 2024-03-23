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

namespace AMS.Controllers
{
    public class LeaveController : Controller
    {
        private ApplicationDbContext _dbContext;
        private System.Timers.Timer timer;
        public LeaveController()
        {
            _dbContext = new ApplicationDbContext();
            timer = new System.Timers.Timer(120000); // 60000 milliseconds = 1 minute
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
        }
        // GET: Leave
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Inbox()
        {
            ReceiveUnreadEmailsFromGmail();
            timer.Start();
            return View();

        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Execute the method to receive unread emails
            ReceiveUnreadEmailsFromGmail();
            //timer.Start();
        }
        [HttpGet]
        public JsonResult GetChatData()
        {
            var chats = _dbContext.receivedLeaveRequests.ToList(); // Assuming you have a Chat model and DbContext setup
            return Json(chats, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetChatMessage(int chatId)
        {
            // Assuming you have a ChatMessage model and DbContext setup
            var chatMessage = _dbContext.receivedLeaveRequests.FirstOrDefault(c => c.Id == chatId);
            if (chatMessage != null)
            {
                return Json(chatMessage, JsonRequestBehavior.AllowGet);
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
                                    Name = name
                                };
                                _dbContext.receivedLeaveRequests.Add(ReceivedEmailRequests);
                                _dbContext.SaveChanges();
                            }
                        }

                        inbox.AddFlags(uid, MessageFlags.Seen, true);
                    }
                }
                client.Disconnect(true);
            }
        }
    }
}