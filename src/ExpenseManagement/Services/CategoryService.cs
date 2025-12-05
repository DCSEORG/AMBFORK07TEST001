using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public interface ICategoryService
{
    Task<List<ExpenseCategory>> GetAllCategoriesAsync();
    Task<List<ExpenseStatus>> GetAllExpenseStatusesAsync();
}

public class CategoryService : ICategoryService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IDatabaseService databaseService, ILogger<CategoryService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", Description = "Travel expenses" },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", Description = "Meal expenses" },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Office Supplies", Description = "Office supply expenses" },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Equipment", Description = "Equipment purchases" }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Pending", Description = "Awaiting approval" },
            new ExpenseStatus { StatusId = 2, StatusName = "Approved", Description = "Approved by manager" },
            new ExpenseStatus { StatusId = 3, StatusName = "Rejected", Description = "Rejected by manager" }
        };
    }

    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                return GetDummyCategories();
            }

            using var command = new SqlCommand("dbo.GetAllCategories", connection);
            command.CommandType = CommandType.StoredProcedure;

            var categories = new List<ExpenseCategory>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetAllExpenseStatusesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                return GetDummyStatuses();
            }

            using var command = new SqlCommand("dbo.GetAllExpenseStatuses", connection);
            command.CommandType = CommandType.StoredProcedure;

            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all expense statuses");
            return GetDummyStatuses();
        }
    }
}
