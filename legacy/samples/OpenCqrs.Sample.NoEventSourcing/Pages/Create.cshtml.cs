using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCqrs.Sample.NoEventSourcing.Domain;
using OpenCqrs.Sample.NoEventSourcing.Domain.Commands;

namespace OpenCqrs.Sample.NoEventSourcing.Pages
{
    public class CreateModel : PageModel
    {
        private readonly IDispatcher _dispatcher;

        public CreateModel(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [BindProperty]
        public Product Product { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var command = new CreateProduct
            {
                Name = Product.Name,
                Description = Product.Description,
                Price = Product.Price,
                ValidateCommand = true
            };

            await _dispatcher.SendAsync(command);

            return RedirectToPage("/List");
        }
    }
}