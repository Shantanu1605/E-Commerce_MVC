using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer
{
    public interface IDbInitializer
    {
        // this method would be responsible for creating the admin user and assigning roles to our website
        void Initialize();
    }
}
