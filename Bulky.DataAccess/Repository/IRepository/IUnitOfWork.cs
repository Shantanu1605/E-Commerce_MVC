using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        // Here we can have all the repositories that we will need 
        // like as of now we will have the category repository

        ICategoryRepository Category { get; }
        IProductRepository Product { get; }

        void Save();
        

    }
}
