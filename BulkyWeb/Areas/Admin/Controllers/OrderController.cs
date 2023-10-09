using Bulky.DataAccess.Repository.IRepository;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties:"ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId==orderId,includeProperties:"Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+ SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            // first we will retrieve the orderHeader from database
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            //now we have to manuualy map what are the fields that we want to update here
            orderHeaderFromDb.Name= OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            // carrier and tracking we will update only if they are not null or empty
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier= OrderVM.OrderHeader.Carrier;
            }

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            // we will call the update and save methods in order to save the changes to the db
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "OrderDetails Updated Successfully";

            return RedirectToAction(nameof(Details), new {orderId= orderHeaderFromDb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.updateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();


            TempData["Success"] = "OrderDetails Updated Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            // before updating the status of order to be shipped, we need to update order header with the following information:
            //1. Tracking number
            //2. Carrier
            //3. OrderStatus
            //4. Shipping date
            //5. Payment due date- for comapny role wale order

            // therefore first we will fetch the orderHeader from the db
            var orderHeader = _unitOfWork.OrderHeader.Get(u=>u.Id==OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber=OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier=OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();


            TempData["Success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }


        // cancel order
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            // first we will retrieve the order header from the db
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            // if the payment status is approved then only we can initiate the refund
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                // doing stripe related task for the refund initiation
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                // adding the service to the stripe
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.updateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.updateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();


            TempData["Success"] = "Order Cancelled Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");

            OrderVM.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            //Stripe Logic
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            // creating a new session service
            var service = new SessionService();
            //when we create the session here we can expect the response back from the server here in the Session variable o fthe stripe
            // This Session variable will have both the id as well as paymentintentID 
            Session session = service.Create(options);

            //now we will have to update back to our table before we redirect
            _unitOfWork.OrderHeader.updateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);

            //Note that at this moment the paymentIntentId would be null as it will only be updated when the payment is successfully completed

            _unitOfWork.Save();

            //now we will redirect to the Stripe website for payment
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            // on coming to the this action, we will have to check whether the payment is done or not and for that we will have
            // to retrieve the session from the stripe and check it's status first.

            // first based on the id we will retrieve the complete order header
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);

            // now we will check that whether the payment status is delayed payment or not, if the payment is delayed payment then we will have to make sure that the payment actually went trhough with stripe payment right now
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is the order by Company
                // thus we will have to retrieve the stripe session
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    //that means the actual payment went through
                    _unitOfWork.OrderHeader.updateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);

                    // now we will not update the order status and we will update only the payment status kyuki order status purana hi rahega
                    _unitOfWork.OrderHeader.updateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

           
            return View(orderHeaderId);
        }


        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            // Applying the validations that if the user is an admin or an employee then he will be able to see all the orders
            if(User.IsInRole(SD.Role_Admin)|| User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
                
            }
            else // else the user will be able to see only his order
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");

            }


            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }



            return Json(new { data = objOrderHeaders });
        }

    }
}
