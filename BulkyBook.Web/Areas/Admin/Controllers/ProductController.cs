using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            
            return View();
        }
        [HttpGet]
        public IActionResult UpSert()
        {
            Product product = new();
            ViewData["categories"] = new SelectList(_unitOfWork.Category.GetAll(), "Id", "Name");
            ViewData["coverTypes"] = new SelectList(_unitOfWork.CoverType.GetAll(), "Id", "Name");
            return View(product);
        }
        [HttpPost]
        public IActionResult UpSert(Product obj,IFormFile? file)
        {
            
            if (ModelState.IsValid)
            {
                var wwwRootPath = _hostEnvironment.WebRootPath;
                if(file != null)
                {
                    var fileName = Guid.NewGuid().ToString();
                    var upload = Path.Combine(wwwRootPath, @"Images\Products");
                    var extension = Path.GetExtension(file.FileName);
                    using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\Images\Products\" + fileName + extension;
                }
                _unitOfWork.Product.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Product Created Successfuly";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var category = _unitOfWork.Category.GetFirstOrDefault(x => x.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category input)
        {
            if (input.Name == input.DisplayOrder.ToString())
            {
                ModelState.AddModelError("", "The DisplayOrder cannt exactly match the Name");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(input);
                _unitOfWork.Save();
                TempData["success"] = "Category Updated Successfuly";
                return RedirectToAction("Index");
            }
            return View(input);
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var category = _unitOfWork.Category.GetFirstOrDefault(x => x.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            _unitOfWork.Category.Remove(category);
            _unitOfWork.Save();
            TempData["success"] = "Category Deleted Successfuly";
            return RedirectToAction("Index");
        }
        #region API CALLS
        [HttpGet]
        public IActionResult GetALl()
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties : "Category,CoverType");
            return Json(new {data = productList});
        }
        #endregion
    }
}
