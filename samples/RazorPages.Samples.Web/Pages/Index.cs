using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPages.Samples.Web.Data;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        public Index(AppDbContext db)
        {
            Db = db;
        }

        public AppDbContext Db { get; }

        public IEnumerable<Customer> GetCustomers() => Db.Customers.ToList();

        public void AddCustomer(Customer customer)
        {
            Db.Add(customer);
            Db.SaveChanges();
        }

        public void DeleteCustomer(int id)
        {
            Db.Remove(new Customer { Id = id });
            Db.SaveChanges();
        }
    }
}
