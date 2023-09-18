using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.Model.ViewModels
{
    public class ProductVM
    {
        //object of type product in order to access all the product data
        public Product Product { get; set; }

        //object of type IEnumerable in order to get the category List of items in order to populate the dropdown on our products page
        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }

        
    }
}
