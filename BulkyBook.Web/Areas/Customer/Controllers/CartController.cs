using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using NuGet.Protocol.Plugins;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBook.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
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
		public IActionResult Summary(ShoppingCartVM obj)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			obj.ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.UserId == claim.Value, includeProperties: "Product");
			obj.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			obj.OrderHeader.OrderStatus = SD.StatusPending;
			obj.OrderHeader.OrderDate = DateTime.Now;
			obj.OrderHeader.UserId = claim.Value;


			foreach (var cart in obj.ListCart)
			{
				cart.Price = GetPriceBaseOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
				obj.OrderHeader.OrderTotal += (cart.Price * cart.Count);

			}
            _unitOfWork.OrderHeader.Add(obj.OrderHeader);
            _unitOfWork.Save();

			foreach (var cart in obj.ListCart)
			{
                OrderDetail OrderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = obj.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count

				};
                _unitOfWork.OrderDetail.Add(OrderDetail); 
                _unitOfWork.Save();

			}
            _unitOfWork.ShoppingCart.RemoveRange(obj.ListCart);
            _unitOfWork.Save();
            return RedirectToAction("Index", "Home");
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
			return RedirectToAction(nameof(Index));
		}

	}
}