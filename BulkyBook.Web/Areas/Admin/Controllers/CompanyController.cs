using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nest;

namespace BulkyBook.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            
            return View();
        }
        [HttpGet]
        public IActionResult UpSert(int? id)
        {
            Company Company = new();
            if (id == null || id == 0)
            {
                return View(Company);
            }
            else
            {
                return View(_unitOfWork.Company.GetFirstOrDefault(x=>x.Id ==id));
            }
            
        }
        [HttpPost]
        public IActionResult UpSert(Company obj)
        {
            
            if (ModelState.IsValid)
            {
                if(obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company Created Successfuly";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company Updated Successfuly";
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
            var CompanyList = _unitOfWork.Company.GetAll();
            return Json(new {data = CompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
            if (obj == null)
            {
                return Json(new { success = false , message ="Error While Deleting"});
            }
            
            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
