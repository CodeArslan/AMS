using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace AMS.Models
{
    public class Card
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please Provide Card Code")]
        [DisplayName("Card Code:")]
        public int cardCode { get; set; }

        public bool isActive { get; set; }
    }
}