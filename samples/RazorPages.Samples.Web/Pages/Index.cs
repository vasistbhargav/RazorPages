using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RazorPages.Samples.Web.Data;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        private readonly ILogger<Index> _logger;

        public Index(AppDbContext db, ILogger<Index> logger)
        {
            Db = db;
            _logger = logger;
        }

        public AppDbContext Db { get; }

        public IEnumerable<Customer> ExistingCustomers { get; set; }

        public Customer Customer { get; set; } = new Customer();

        public string SuccessMessage
        {
            get { return (string)TempData[nameof(SuccessMessage)]; }
            set { TempData[nameof(SuccessMessage)] = value; }
        }

        public bool ShowSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

        public string ErrorMessage
        {
            get { return (string)TempData[nameof(ErrorMessage)]; }
            set { TempData[nameof(ErrorMessage)] = value; }
        }

        public bool ShowErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public async Task OnGetAsync()
        {
            ExistingCustomers = Db.Customers.AsNoTracking().ToList();
        }

        public async Task<IActionResult> OnPost(int? deleteId)
        {
            if (deleteId.HasValue)
            {
                _logger.LogInformation("Performing delete of customer with ID {customerId}", deleteId.Value);
                Db.Remove(new Customer { Id = deleteId.Value });
                Db.SaveChanges();
                SuccessMessage = $"Customer {deleteId} deleted successfully!";
                return Redirect("/");
            }

            await TryUpdateModelAsync(Customer, nameof(Customer));

            if (!ModelState.IsValid)
            {
                // Model errors, populate customer list and show errors
                ExistingCustomers = Db.Customers.AsNoTracking().ToList();
                ErrorMessage = "Please correct the errors and try again";
                return View();
            }

            Db.Add(Customer);
            Db.SaveChanges();
            SuccessMessage = $"Customer {Customer.Id} successfully created!";
            return Redirect("/");
        }
    }
}
