using System.Diagnostics;
using DealerDatabase.Data;
using DealerDatabase.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Web.Controllers;

public class HomeController(DealerDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? searchString, CancellationToken cancellationToken)
    {
        var dealersQuery = db.Dealers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            dealersQuery = dealersQuery.Where(d => d.Name.ToLower().Contains(searchString.ToLower()));
        }

        var dealers = await dealersQuery
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);

        // Pass the current filter back to the view to keep the input populated
        ViewData["CurrentFilter"] = searchString;

        return View(dealers);
    }


    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dealer = await db.Dealers
            .AsNoTracking()
            // .Include(d => d.Addresses) // Uncomment and include related navigation properties if applicable
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (dealer == null)
        {
            return NotFound();
        }

        return View(dealer);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
