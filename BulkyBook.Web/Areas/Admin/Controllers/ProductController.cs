using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nest;

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
        public IActionResult UpSert(int? id)
        {
            Product product = new();
            ViewData["categories"] = new SelectList(_unitOfWork.Category.GetAll(), "Id", "Name");
            ViewData["coverTypes"] = new SelectList(_unitOfWork.CoverType.GetAll(), "Id", "Name");
            if (id == null || id == 0)
            {
                return View(product);
            }
            else
            {
                return View(_unitOfWork.Product.GetFirstOrDefault(x=>x.Id ==id));
            }
            
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
                    if(obj.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\Images\Products\" + fileName + extension;
                }
                if(obj.Id == 0)
                {
                    _unitOfWork.Product.Add(obj);
                    TempData["success"] = "Product Created Successfuly";
                }
                else
                {
                    _unitOfWork.Product.Update(obj);
                    TempData["success"] = "Product Updated Successfuly";
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        
        
        
        #region API CALLS
        [HttpGet]
        public IActionResult GetALl()
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties : "Category,CoverType");
            return Json(new {data = productList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);
            if (obj == null)
            {
                return Json(new { success = false , message ="Error While Deleting"});
            }
            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
