
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
       
        private readonly IUnitOfWork _unitOfWork;
       
        public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
           
        }

        public IActionResult Index()
        {
            List<Company> objCategoryList = _unitOfWork.Company.GetAll().ToList();
           
            return View(objCategoryList);
        }

        //creating a new controller for Create new category
        //public IActionResult Create()
        //{
        //    // before sending the list of Companys to the view I want to first retrieve the data of category from the database and the send it to the view.
        //    //In order to retrieve the data from database dynamically we will make use of Projections concept of EF Core
        //    IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
        //        .GetAll().Select(u => new SelectListItem
        //        {
        //            Text = u.Name,
        //            Value = u.Id.ToString()
        //        });
        //    //Inorder to transfer this category data retrieved to the view we will use ViewBag(which is used to transer data from controller to the view and it will have a key value pair)
        //    //ViewBag.CategoryList = CategoryList;


        //    //Another way of sending this data to the view is using ViewData
        //    ViewData["CategoryList"]= CategoryList;
        //    return View();
        //}

        public IActionResult Upsert(int? id)
        {
            
            if(id==null || id == 0)
            {
                // that means it is create
                return View(new Company());
            }
            else
            {
                //update functionality
                Company companyObj = _unitOfWork.Company.Get(u => u.Id == id);
                return View(companyObj);
            }
            
        }

        // adding a new category by user onto our database 
        [HttpPost]
        public IActionResult Upsert(Company CompanyObj)
        {
           

            if (ModelState.IsValid)
            {
                

                if(CompanyObj.Id==0) // createcwali functinality
                {
                    _unitOfWork.Company.Add(CompanyObj); //addiing the new category to the databse

                }
                else// update wali functionality
                {
                    _unitOfWork.Company.Update(CompanyObj);
                }
                _unitOfWork.Save(); // saving the new changes onto the db
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index"); //redirecting to the Index action of the controller Category
            }
            else// if our ModelState is not not valid then while returning we need to populate the dropdown once again
            {
              
                return View(CompanyObj);
                    
            };
            
           

        }

        //Actions for Edit
        //public IActionResult Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }

        //    Company? CompanyFromDb = _unitOfWork.Company.Get(u => u.Id == id);
        //    //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u => u.Id == id);
        //    //Category? categoryFromDb2 = _db.Categories.Where(u => u.Id == id).FirstOrDefault();

        //    if (CompanyFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(CompanyFromDb);
        //}

        //[HttpPost]
        //public IActionResult Edit(Company obj)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Company.Update(obj);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Category edited successfully";
        //        return RedirectToAction("Index");
        //    }

        //    return View();
        //}

        // Action method for Delete
        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }

        //    Company? CompanyFromDb = _unitOfWork.Company.Get(u => u.Id == id);

        //    if (CompanyFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(CompanyFromDb);
        //}

        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Company? obj = _unitOfWork.Company.Get(u => u.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.Company.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Category deleted successfully";
        //    return RedirectToAction("Index");


        //}

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll() 
        {
            List<Company> objCategoryList = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data=objCategoryList});
        }

        // Delete API
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
            if(CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            //if Company to be deleted is not null then first we will have to remove its image and then remove the Company from the db
           

            _unitOfWork.Company.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
