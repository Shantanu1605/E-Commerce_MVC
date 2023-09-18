using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        // the IProductRepository is an interface for our Product model, 
        // thus it will implement our base IRepository interface and will have access to aal of its basic methods
        // on top of it we will also have the update and saveChanges method for this interface

        void Update(Product obj);
        
    }
}
