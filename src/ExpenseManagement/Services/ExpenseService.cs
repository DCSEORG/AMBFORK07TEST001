using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int id);
    Task<List<Expense>> GetExpensesByUserIdAsync(int userId);
    Task<List<Expense>> GetExpensesByStatusAsync(int statusId);
    Task<int> CreateExpenseAsync(ExpenseCreate expense);
    Task<bool> UpdateExpenseAsync(int id, ExpenseUpdate expense);
    Task<bool> ApproveExpenseAsync(int id, int approvedBy);
    Task<bool> RejectExpenseAsync(int id, int rejectedBy);
    Task<bool> DeleteExpenseAsync(int id);
    Task<List<ExpenseSummary>> GetExpenseSummaryByUserAsync(int userId);
    Task<List<CategorySummary>> GetExpenseSummaryByCategoryAsync();
    bool IsUsingDummyData();
}

public class ExpenseService : IExpenseService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ExpenseService> _logger;
    private bool _useDummyData = false;

    public ExpenseService(IDatabaseService databaseService, ILogger<ExpenseService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public bool IsUsingDummyData() => _useDummyData;

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "John Doe",
                CategoryId = 1,
                CategoryName = "Travel",
                Amount = 150.00m,
                Currency = "GBP",
                Description = "Train ticket to London",
                ExpenseDate = DateTime.Now.AddDays(-5),
                StatusId = 1,
                StatusName = "Pending",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "John Doe",
                CategoryId = 2,
                CategoryName = "Meals",
                Amount = 45.50m,
                Currency = "GBP",
                Description = "Lunch with client",
                ExpenseDate = DateTime.Now.AddDays(-3),
                StatusId = 2,
                StatusName = "Approved",
                ApprovedBy = 2,
                ApprovedAt = DateTime.Now.AddDays(-2),
                CreatedAt = DateTime.Now.AddDays(-3)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 2,
                UserName = "Jane Smith",
                CategoryId = 3,
                CategoryName = "Office Supplies",
                Amount = 25.00m,
                Currency = "GBP",
                Description = "Printer paper and pens",
                ExpenseDate = DateTime.Now.AddDays(-1),
                StatusId = 1,
                StatusName = "Pending",
                CreatedAt = DateTime.Now.AddDays(-1)
            }
        };
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return GetDummyExpenses();
            }

            using var command = new SqlCommand("dbo.GetAllExpenses", connection);
            command.CommandType = CommandType.StoredProcedure;

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _useDummyData = false;
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all expenses");
            _useDummyData = true;
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == id);
            }

            using var command = new SqlCommand("dbo.GetExpenseById", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", id);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                _useDummyData = false;
                return MapExpenseFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense by ID: {ExpenseId}", id);
            _useDummyData = true;
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == id);
        }
    }

    public async Task<List<Expense>> GetExpensesByUserIdAsync(int userId)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return GetDummyExpenses().Where(e => e.UserId == userId).ToList();
            }

            using var command = new SqlCommand("dbo.GetExpensesByUserId", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", userId);

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _useDummyData = false;
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by user ID: {UserId}", userId);
            _useDummyData = true;
            return GetDummyExpenses().Where(e => e.UserId == userId).ToList();
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(int statusId)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return GetDummyExpenses().Where(e => e.StatusId == statusId).ToList();
            }

            using var command = new SqlCommand("dbo.GetExpensesByStatus", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@StatusId", statusId);

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _useDummyData = false;
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses by status: {StatusId}", statusId);
            _useDummyData = true;
            return GetDummyExpenses().Where(e => e.StatusId == statusId).ToList();
        }
    }

    public async Task<int> CreateExpenseAsync(ExpenseCreate expense)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return 0; // Return 0 to indicate failure
            }

            using var command = new SqlCommand("dbo.CreateExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", expense.UserId);
            command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
            command.Parameters.AddWithValue("@Amount", expense.Amount);
            command.Parameters.AddWithValue("@Currency", expense.Currency);
            command.Parameters.AddWithValue("@Description", expense.Description);
            command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
            command.Parameters.AddWithValue("@ReceiptImageUrl", (object?)expense.ReceiptImageUrl ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            _useDummyData = false;
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            _useDummyData = true;
            return 0;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int id, ExpenseUpdate expense)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return false;
            }

            using var command = new SqlCommand("dbo.UpdateExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", id);
            command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
            command.Parameters.AddWithValue("@Amount", expense.Amount);
            command.Parameters.AddWithValue("@Currency", expense.Currency);
            command.Parameters.AddWithValue("@Description", expense.Description);
            command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
            command.Parameters.AddWithValue("@ReceiptImageUrl", (object?)expense.ReceiptImageUrl ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var rowsAffected = reader.GetInt32(0);
                _useDummyData = false;
                return rowsAffected > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense: {ExpenseId}", id);
            _useDummyData = true;
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int id, int approvedBy)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return false;
            }

            using var command = new SqlCommand("dbo.ApproveExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", id);
            command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var rowsAffected = reader.GetInt32(0);
                _useDummyData = false;
                return rowsAffected > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense: {ExpenseId}", id);
            _useDummyData = true;
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(int id, int rejectedBy)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return false;
            }

            using var command = new SqlCommand("dbo.RejectExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", id);
            command.Parameters.AddWithValue("@RejectedBy", rejectedBy);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var rowsAffected = reader.GetInt32(0);
                _useDummyData = false;
                return rowsAffected > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense: {ExpenseId}", id);
            _useDummyData = true;
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return false;
            }

            using var command = new SqlCommand("dbo.DeleteExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var rowsAffected = reader.GetInt32(0);
                _useDummyData = false;
                return rowsAffected > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense: {ExpenseId}", id);
            _useDummyData = true;
            return false;
        }
    }

    public async Task<List<ExpenseSummary>> GetExpenseSummaryByUserAsync(int userId)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return new List<ExpenseSummary>();
            }

            using var command = new SqlCommand("dbo.GetExpenseSummaryByUser", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", userId);

            var summaries = new List<ExpenseSummary>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                summaries.Add(new ExpenseSummary
                {
                    StatusName = reader.GetString(0),
                    ExpenseCount = reader.GetInt32(1),
                    TotalAmount = reader.GetDecimal(2),
                    Currency = reader.GetString(3)
                });
            }

            _useDummyData = false;
            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary by user: {UserId}", userId);
            _useDummyData = true;
            return new List<ExpenseSummary>();
        }
    }

    public async Task<List<CategorySummary>> GetExpenseSummaryByCategoryAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                _useDummyData = true;
                return new List<CategorySummary>();
            }

            using var command = new SqlCommand("dbo.GetExpenseSummaryByCategory", connection);
            command.CommandType = CommandType.StoredProcedure;

            var summaries = new List<CategorySummary>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                summaries.Add(new CategorySummary
                {
                    CategoryName = reader.GetString(0),
                    ExpenseCount = reader.GetInt32(1),
                    TotalAmount = reader.GetDecimal(2),
                    Currency = reader.GetString(3)
                });
            }

            _useDummyData = false;
            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense summary by category");
            _useDummyData = true;
            return new List<CategorySummary>();
        }
    }

    private Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            UserName = reader.GetString(2),
            CategoryId = reader.GetInt32(3),
            CategoryName = reader.GetString(4),
            Amount = reader.GetDecimal(5),
            Currency = reader.GetString(6),
            Description = reader.GetString(7),
            ExpenseDate = reader.GetDateTime(8),
            StatusId = reader.GetInt32(9),
            StatusName = reader.GetString(10),
            ReceiptImageUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
            ApprovedBy = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            ApprovedAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            CreatedAt = reader.GetDateTime(14)
        };
    }
}
