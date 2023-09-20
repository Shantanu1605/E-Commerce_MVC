
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
       
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> objCategoryList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
           
            return View(objCategoryList);
        }

        //creating a new controller for Create new category
        //public IActionResult Create()
        //{
        //    // before sending the list of products to the view I want to first retrieve the data of category from the database and the send it to the view.
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
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()

            };
            if(id==null || id == 0)
            {
                // that means it is create
                return View(productVM);
            }
            else
            {
                //update functionality
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }
            
        }

        // adding a new category by user onto our database 
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM,IFormFile? file)
        {
           

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;//this will give the path to wwwRoot folder
                if(file!=null)// if file is not null
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // this is used to give a random name to our files that were uploaded
                    string productPath = Path.Combine(wwwRootPath, @"images\product");
                    
                    //if file is not null and if imageUrl is present--> then we will have to delete the already present image and then we will save this new image to the database
                    if(!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        // first delete the old image
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // finally saving our file to the correct folder
                    using (var fileStream = new FileStream(Path.Combine(productPath,fileName),FileMode.Create) )
                    {
                        file.CopyTo(fileStream);// this will copy the file to the new location that we have configured
                    }

                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if(productVM.Product.Id==0) // createcwali functinality
                {
                    _unitOfWork.Product.Add(productVM.Product); //addiing the new category to the databse

                }
                else// update wali functionality
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }
                _unitOfWork.Save(); // saving the new changes onto the db
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index"); //redirecting to the Index action of the controller Category
            }
            else// if our ModelState is not not valid then while returning we need to populate the dropdown once again
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
                    
            };
            
           

        }

        //Actions for Edit
        //public IActionResult Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }

        //    Product? productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
        //    //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u => u.Id == id);
        //    //Category? categoryFromDb2 = _db.Categories.Where(u => u.Id == id).FirstOrDefault();

        //    if (productFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(productFromDb);
        //}

        //[HttpPost]
        //public IActionResult Edit(Product obj)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Product.Update(obj);
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

        //    Product? productFromDb = _unitOfWork.Product.Get(u => u.Id == id);

        //    if (productFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(productFromDb);
        //}

        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Product? obj = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.Product.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Category deleted successfully";
        //    return RedirectToAction("Index");


        //}

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll() 
        {
            List<Product> objCategoryList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new {data=objCategoryList});
        }

        // Delete API
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            //if product to be deleted is not null then first we will have to remove its image and then remove the product from the db
            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
