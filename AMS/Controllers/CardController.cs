using AMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AMS.Controllers
{
    public class CardController : Controller
    {
        private ApplicationDbContext _dbContext;
        public CardController()
        {
            _dbContext = new ApplicationDbContext();
        }
        // GET: Department

        public ActionResult Index()
        {
            return View();
        }
        public async Task<ActionResult> GetCardData()
        {
            var cardList = await _dbContext.Cards.AsNoTracking().ToListAsync();
            return Json(cardList, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult CardDetails(Card card)
        {
            try
            {
                if (card.Id == 0)
                {
                    _dbContext.Cards.Add(card);
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Card Details Added Successfully." });
                }
                else
                {
                    var cardInDb = _dbContext.Cards.FirstOrDefault(d => d.Id == card.Id);
                    cardInDb.cardCode = card.cardCode;
                    cardInDb.isActive = card.isActive;
                    _dbContext.SaveChanges();
                    return Json(new { success = true, message = "Card Details Updated Successfully." });
                }
            }
            catch (Exception)
            {
                // Log the exception or handle it as needed
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }
        public ActionResult GetCardById(int id)
        {
            var card = _dbContext.Cards.AsNoTracking().SingleOrDefault(d => d.Id == id);

            if (card == null)
            {
                // Handle not found case
                return Json(new { success = false, message = "Card Details Doesnot Found" });
            }

            return Json(card, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Delete(int id)
        {
            var cardInDb = _dbContext.Cards.SingleOrDefault(d => d.Id == id);
            if (cardInDb == null)
            {
                return Json(new { success = false, message = "Card Code Doesnot Found" });
            }

            else
            {
                _dbContext.Cards.Remove(cardInDb);
                _dbContext.SaveChanges();
                return Json(new { success = true, message = "Card Code Successfully Deleted" });
            }
        }
        //this function is used to check duplicate name for department
        [HttpPost]
        public JsonResult IsNameAvailable(int name, int? id, bool isUpdate)
        {
            bool isNameAvailable;

            if (isUpdate && id.HasValue)
            {
                // For update, exclude the current department name with the specified ID
                isNameAvailable = !_dbContext.Cards.AsNoTracking().Any(x => x.cardCode == name && x.Id != id.Value);
            }
            else
            {
                // For add operation or if the ID is not provided, check for duplicates without exclusion
                isNameAvailable = !_dbContext.Cards.AsNoTracking().Any(x => x.cardCode == name);
            }

            return Json(isNameAvailable);
        }

    }
}