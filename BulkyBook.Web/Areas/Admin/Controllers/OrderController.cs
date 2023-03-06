using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
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
			OrderVM = new OrderVM()
			{
				OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id == orderId ,includeProperties:"User"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(x=>x.OrderHeaderId == orderId , includeProperties:"Product")
			};
			return View(OrderVM);

        }
		[ValidateAntiForgeryToken]
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
		public IActionResult Details(OrderVM OrderVM)
		{
			var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id);
			orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
			if(OrderVM.OrderHeader.Carrier != null)
			{
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
			}
			if(OrderVM.OrderHeader.TrackingNumber != null)
			{
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}
			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order Details Update Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });

		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Details_Pay_Now(OrderVM OrderVM)
		{
			OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, includeProperties: "User");
			OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(x => x.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

			//stripe setting
			var domain = "https://localhost:44354/";
			var options = new SessionCreateOptions
			{
				PaymentMethodTypes = new List<string>
				{
				  "card",
				},
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId",
			};

			foreach (var item in OrderVM.OrderDetail)
			{

				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)(item.Price * 100),//20.00 -> 2000
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.Title
						},

					},
					Quantity = item.Count,
				};
				options.LineItems.Add(sessionLineItem);

			}

			var service = new SessionService();
			Session session = service.Create(options);
			_unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
			

		}


		public IActionResult PaymentConfirmation(int orderHeaderId)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderId);
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				if (session.PaymentStatus.ToLower() == "paid")
				{

					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			
			return View(orderHeaderId);


		}


		[ValidateAntiForgeryToken]
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
		public IActionResult StartProcessing(OrderVM OrderVM)
		{
			_unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id,SD.StatusInProcess);
			_unitOfWork.Save();
			TempData["Success"] = "Order Status Update Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });

		}
		[ValidateAntiForgeryToken]
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
		public IActionResult ShipOrder(OrderVM OrderVM)
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id==OrderVM.OrderHeader.Id);
			orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipping;
			orderHeader.ShippingDate = DateTime.Now;
			if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
			}
			_unitOfWork.OrderHeader.Update(orderHeader);
			_unitOfWork.Save();
			TempData["Success"] = "Order Shipped Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });

		}
		[ValidateAntiForgeryToken]
		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
		public IActionResult CancelOrder(OrderVM OrderVM)
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id);
			if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};
				var service = new RefundService();
				Refund refund = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}


			_unitOfWork.OrderHeader.Update(orderHeader);
			_unitOfWork.Save();
			TempData["Success"] = "Order Cancelld Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });

		}

		#region API CALLS
		[HttpGet]
		public IActionResult GetALl(string status)
		{
			IEnumerable<OrderHeader> OrderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Emoloyee)) {
				 OrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "User");
			}
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			    OrderHeaders = _unitOfWork.OrderHeader.GetAll(x => x.UserId == claim.Value, includeProperties: "User");

            }
			

            switch (status)
            {
                case "pending":
					OrderHeaders = OrderHeaders.Where(x => x.OrderStatus == SD.StatusPending);
                    break;
				case "inprocess":
                    OrderHeaders = OrderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    OrderHeaders = OrderHeaders.Where(x => x.OrderStatus == SD.StatusShipping);
                    break;
                case "approved":
                    OrderHeaders = OrderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }

            return Json(new { data = OrderHeaders });
		}
		#endregion
	}
}
