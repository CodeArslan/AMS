﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AMS.Models;
using System.Configuration;

namespace AMS.Controllers
{
    public class EmailsController : Controller
    {
        // GET: Emails
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SendEmail(Email model, HttpPostedFileBase attachment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(ConfigurationManager.AppSettings["Email"].ToString());
                    mail.To.Add(model.Recipient);
                    mail.Subject = model.Subject;
                    mail.Body = model.Message;

                    if (attachment != null && attachment.ContentLength > 0)
                    {
                        // Get the file name
                        string fileName = System.IO.Path.GetFileName(attachment.FileName);

                        // Attach the file
                        mail.Attachments.Add(new Attachment(attachment.InputStream, fileName));
                    }

                    // Configure SMTP client
                    SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                    smtp.Port = 587;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Email"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                    smtp.EnableSsl = true;

                    // Send the email
                    smtp.Send(mail);

                    ViewBag.Message = "Email sent successfully.";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error sending email: " + ex.Message;
                }
            }

            // If model state is not valid, return the view with validation errors
            return View(model);
        }
    }
}