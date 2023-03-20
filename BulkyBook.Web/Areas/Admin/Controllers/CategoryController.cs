using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var categories = _unitOfWork.Category.GetAll();
            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category input)
        {
            if (input.Name == input.DisplayOrder.ToString())
            {
                ModelState.AddModelError("", "The DisplayOrder cannt exactly match the Name");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(input);
                _unitOfWork.Save();
                TempData["success"] = "Category Created Successfuly";
                return RedirectToAction("Index");
            }
            return View(input);
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
    }
}
