using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var coverTypes = _unitOfWork.CoverType.GetAll();
            return View(coverTypes);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(CoverType input)
        {
            
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Add(input);
                _unitOfWork.Save();
                TempData["success"] = "CoverType Created Successfuly";
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
            var coverType = _unitOfWork.CoverType.GetFirstOrDefault(x => x.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }
            return View(coverType);
        }
        [HttpPost]
        public IActionResult Edit(CoverType input)
        {
            
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Update(input);
                _unitOfWork.Save();
                TempData["success"] = "CoverType Updated Successfuly";
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
            var coverType = _unitOfWork.CoverType.GetFirstOrDefault(x => x.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }
            _unitOfWork.CoverType.Remove(coverType);
            _unitOfWork.Save();
            TempData["success"] = "CoverType Deleted Successfuly";
            return RedirectToAction("Index");
        }
    }
}
