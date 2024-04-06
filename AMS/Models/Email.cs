using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Email
    {
        [Required(ErrorMessage = "Please Provide Recipient email")]

        public string Recipient { get; set; }

        [Required(ErrorMessage = "Please Provide Subject")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please Provide Message")]
        public string Message { get; set; }
        public string Attachment { get; set; }
    }
}