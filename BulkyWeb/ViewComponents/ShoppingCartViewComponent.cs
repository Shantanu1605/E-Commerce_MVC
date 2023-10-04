using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


// this would be the backend file for our view component
namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        //Thus we will have to go to our shopping cart database and fetch the shopping cart for our logged in user
        //we will do that using dependency injection
        private readonly IUnitOfWork _unitOfWork;   
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // now we will create a method that will handle the backend functionality of our shopping cart view component

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null) //that means the user is logged in
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) == null) {

                    // therefore in this case we will get the session from the databse and set that
                    HttpContext.Session.SetInt32(SD.SessionCart,
                      _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count());
                }
                // else there is a session already therefore databse se laane ki zarurat nhi hai directly session cart se hi count ko view mein bhej denge
                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
