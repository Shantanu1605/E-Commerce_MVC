using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        // the IcategoryRepository is an interface for our Category model, 
        // thus it will implement our base IRepository interface and will have access to aal of its basic methods
        // on top of it we will also have the update and saveChanges method for this interface

        // update method will update the entire order header
        void Update(OrderHeader obj);

        //we willl another method for Updating status i.e it will receive an order id, orderStatus and paymentStatus may be null 
        //because everytime the order status will get updated and payment status may remain same as the last one, therofore humne paymentstatus ko nullable liya iss function mein
        void updateStatus(int id, string orderStatus, string? paymentStatus =null);

        void updateStripePaymentID(int id, string sessionId, string paymentIntentId); 
        
    }
}
