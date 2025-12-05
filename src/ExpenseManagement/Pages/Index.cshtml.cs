using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IUserService _userService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<CategorySummary> CategorySummary { get; set; } = new();

    public IndexModel(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IUserService userService,
        IDatabaseService databaseService,
        ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _userService = userService;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Test database connection
            var isConnected = await _databaseService.TestConnectionAsync();
            
            if (!isConnected)
            {
                var error = _databaseService.GetConnectionError();
                ViewData["ErrorMessage"] = "Using dummy data because database is not available.";
                ViewData["ErrorDetails"] = $"Connection error: {error}";
                
                // Provide troubleshooting guidance
                if (error.Contains("Managed Identity") || error.Contains("Identity"))
                {
                    ViewData["ErrorDetails"] = "Managed Identity authentication issue. Ensure AZURE_CLIENT_ID is set and the managed identity has database permissions.";
                }
            }

            // Load all data
            Expenses = await _expenseService.GetAllExpensesAsync();
            Categories = await _categoryService.GetAllCategoriesAsync();
            Statuses = await _categoryService.GetAllExpenseStatusesAsync();
            Users = await _userService.GetAllUsersAsync();
            CategorySummary = await _expenseService.GetExpenseSummaryByCategoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading index page");
            ViewData["ErrorMessage"] = "An error occurred while loading the page.";
            ViewData["ErrorDetails"] = ex.Message;
        }
    }
}
