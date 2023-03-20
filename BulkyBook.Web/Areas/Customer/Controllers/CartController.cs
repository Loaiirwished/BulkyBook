using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using NuGet.Protocol.Plugins;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;
using Session = Stripe.Checkout.Session;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using SessionService = Stripe.Checkout.SessionService;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public int OrderTotal { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

		public IActionResult Index()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == claim.Value, includeProperties:"Product"),
                OrderHeader = new()
            };
            foreach(var cart in ShoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBaseOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count); 

			}
			return View(ShoppingCartVM);
        }

		public IActionResult Summary()
		{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.User = _unitOfWork.User.GetFirstOrDefault(x => x.Id == claim.Value);
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.User.PhoneNumber;
			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.User.State;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.User.StreetAddress;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.User.Name;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.User.City;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.User.PostalCode;

			foreach (var cart in ShoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBaseOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);

            }
            return View(ShoppingCartVM);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Summary(ShoppingCartVM ShoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == claim.Value, includeProperties: "Product");
			ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			ShoppingCartVM.OrderHeader.UserId = claim.Value;


			foreach (var cart in ShoppingCartVM.ListCart)
			{
				cart.Price = GetPriceBaseOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);

			}
			User user = _unitOfWork.User.GetFirstOrDefault(x=>x.Id == claim.Value);
			if(user.CompanyId.GetValueOrDefault() == 0)
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

			foreach (var cart in ShoppingCartVM.ListCart)
			{
                OrderDetail OrderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count

				};
                _unitOfWork.OrderDetail.Add(OrderDetail); 
                _unitOfWork.Save();

			}
			if (user.CompanyId.GetValueOrDefault() == 0)
			{
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
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + $"customer/cart/index",
				};

				foreach (var item in ShoppingCartVM.ListCart)
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
				_unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
			else
			{
				return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });
			}


		/*_unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
            _unitOfWork.Save();
            return RedirectToAction("Index", "Home");*/
		}

		public IActionResult OrderConfirmation(int Id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id == Id);
			if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(Id, orderHeader.SessionId, session.PaymentIntentId);
					HttpContext.Session.Clear();
					_unitOfWork.OrderHeader.UpdateStatus(Id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}
			
			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == orderHeader.UserId).ToList();
			_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			return View(Id);


		}

		private double GetPriceBaseOnQuantity(double quantity,double price,double price50,double price100)
        {
            if(quantity <= 50)
            {
                return price;
            }
            else
            {
                if(quantity <= 100)
                {
					return price50;
				}
                return price100;
            }
        } 
        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x=>x.Id == cartId);
            _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
            _unitOfWork.ShoppingCart.Update(cart);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
		public IActionResult Minus(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
            if(cart.Count <= 1)
            {
				_unitOfWork.ShoppingCart.Remove(cart);
                var count = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == cart.UserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
				_unitOfWork.ShoppingCart.DecermentCount(cart, 1);
				_unitOfWork.ShoppingCart.Update(cart);
			}
			
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
			_unitOfWork.ShoppingCart.Remove(cart);
			_unitOfWork.Save();
			var count = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == cart.UserId).ToList().Count;
			HttpContext.Session.SetInt32(SD.SessionCart,count);
			return RedirectToAction(nameof(Index));
		}

	}
}