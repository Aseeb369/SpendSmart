using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendSmart.Models;

namespace SpendSmart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SpendSmartDbContext _context;

        public HomeController(ILogger<HomeController> logger, SpendSmartDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/Expense
        public async Task<IActionResult> Expense()
        {
            var allExpenses = await _context.Expenses.ToListAsync();

            var totalExpenses = allExpenses.Sum(x => x.Value);

            ViewBag.Expenses = totalExpenses;
            return View(allExpenses);
        }

        // GET: /Home/CreateEditExpense or /Home/CreateEditExpense?id=5
        [HttpGet]
        public async Task<IActionResult> CreateEditExpense(int? id)
        {
            if (id == null)
                return View(new Expense()); // create

            var expenseInDb = await _context.Expenses.FindAsync(id.Value);
            if (expenseInDb == null)
                return NotFound();

            return View(expenseInDb); // edit
        }

        // POST: /Home/CreateEditExpenseForm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEditExpenseForm(Expense model)
        {
            if (!ModelState.IsValid)
                return View("CreateEditExpense", model);

            try
            {
                if (model.Id == 0)
                {
                    _context.Expenses.Add(model);
                }
                else
                {
                    var existing = await _context.Expenses.FindAsync(model.Id);

                    if (existing == null)
                        return NotFound();

                    existing.Value = model.Value;
                    existing.Description = model.Description;
                    existing.Category = model.Category;

                    _context.Expenses.Update(existing);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Expense));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed saving expense (Id={Id})", model.Id);
                ModelState.AddModelError(string.Empty, "An error occurred saving the expense.");
                return View("CreateEditExpense", model);
            }
        }

        // GET: /Home/DeleteExpense?id=5  (optional confirmation page)
        [HttpGet]
        public async Task<IActionResult> DeleteExpense(int? id)
        {
            if (id == null)
                return BadRequest();

            var expenseInDb = await _context.Expenses.FindAsync(id.Value);
            if (expenseInDb == null)
                return NotFound();

            return View(expenseInDb); // should show a confirmation UI
        }

        // POST: /Home/DeleteExpense (form posts back here)
        [HttpPost, ActionName("DeleteExpense")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpenseConfirmed(int id)
        {
            var expenseInDb = await _context.Expenses.FindAsync(id);
            if (expenseInDb == null)
                return NotFound();

            try
            {
                _context.Expenses.Remove(expenseInDb);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Expense));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed deleting expense (Id={Id})", id);
                ModelState.AddModelError(string.Empty, "Unable to delete the expense.");
                return RedirectToAction(nameof(Expense));
            }
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    
    public IActionResult Overview()
        {
            var categoryTotals = _context.Expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(e => e.Value)
                })
                .ToList();

            return View(categoryTotals);
        }
    }
    }