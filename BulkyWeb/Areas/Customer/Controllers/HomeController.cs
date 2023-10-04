
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Model;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            //handling the seesion wala part i.e agar user logged in hai to usko cart mein values display honi chahiye
            // Therefore first getting the user id of the logged in user
            // This code is now shifted to the  shopping cart view controller
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //if (claim != null) //that means the user is logged in
            //{
            //    // therefore in this case we will get the session and set that
            //    HttpContext.Session.SetInt32(SD.SessionCart,
            //      _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count());
            //}

            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");

            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId

            };
            return View(shoppingCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId= userId;

            // working on if the user id and product id are already existing in our shopping cart table then in that case we will be updating only the count of the product
            // but if any of the user id or product id are not already existing then we will make a new entry to our database of shopping cart

            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId &&
            u.ProductId == shoppingCart.ProductId);

            if(cartFromDb != null) {
                //this means that cart already exists thus we will update only the count
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                //cart does not contain the same product, thus we will add a new product
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                //whenever we are adding a new item to the cart we will have to set that value to the session
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
            }
            TempData["success"] = "Cart Updated Successfully";

            
           

            return RedirectToAction(nameof(Index));
        }





        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
