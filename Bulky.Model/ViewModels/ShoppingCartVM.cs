using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Model.ViewModels
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }     

        public OrderHeader OrderHeader { get; set; }

        // we are also having OrderTotal inside our OrderHeader Thus we will be accesiing that orderTotal 
        //public double OrderTotal { get; set; } 
    }
}
