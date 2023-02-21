using BulkyBook.Web.Data;
using BulkyBook.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var categories = _db.Categories.ToList();
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
            if(input.Name == input.DisplayOrder.ToString())
            {
                ModelState.AddModelError("", "The DisplayOrder cannt exactly match the Name");
            }
            if(ModelState.IsValid)
            {
                _db.Categories.Add(input);
                _db.SaveChanges();
                TempData["success"] = "Category Created Successfuly";
                return RedirectToAction("Index");
            }
            return View(input);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            }
            var category = _db.Categories.Find(id);
            if(category == null)
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
                _db.Categories.Update(input);
                _db.SaveChanges();
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
            var category = _db.Categories.Find(id);
            if (category == null)
            {
                return NotFound();
            }
            _db.Categories.Remove(category);
            _db.SaveChanges();
            TempData["success"] = "Category Deleted Successfuly";
            return RedirectToAction("Index");
        }
    }
}
