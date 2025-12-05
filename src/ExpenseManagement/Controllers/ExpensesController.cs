using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAllExpenses()
    {
        var expenses = await _expenseService.GetAllExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpenseById(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} not found" });
        }
        return Ok(expense);
    }

    /// <summary>
    /// Get expenses by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Expense>>> GetExpensesByUserId(int userId)
    {
        var expenses = await _expenseService.GetExpensesByUserIdAsync(userId);
        return Ok(expenses);
    }

    /// <summary>
    /// Get expenses by status ID
    /// </summary>
    [HttpGet("status/{statusId}")]
    public async Task<ActionResult<List<Expense>>> GetExpensesByStatus(int statusId)
    {
        var expenses = await _expenseService.GetExpensesByStatusAsync(statusId);
        return Ok(expenses);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] ExpenseCreate expense)
    {
        var expenseId = await _expenseService.CreateExpenseAsync(expense);
        if (expenseId == 0)
        {
            return StatusCode(500, new { message = "Failed to create expense" });
        }
        return CreatedAtAction(nameof(GetExpenseById), new { id = expenseId }, new { expenseId });
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] ExpenseUpdate expense)
    {
        var success = await _expenseService.UpdateExpenseAsync(id, expense);
        if (!success)
        {
            return NotFound(new { message = $"Expense with ID {id} not found or update failed" });
        }
        return NoContent();
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] int approvedBy)
    {
        var success = await _expenseService.ApproveExpenseAsync(id, approvedBy);
        if (!success)
        {
            return NotFound(new { message = $"Expense with ID {id} not found or approval failed" });
        }
        return NoContent();
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] int rejectedBy)
    {
        var success = await _expenseService.RejectExpenseAsync(id, rejectedBy);
        if (!success)
        {
            return NotFound(new { message = $"Expense with ID {id} not found or rejection failed" });
        }
        return NoContent();
    }

    /// <summary>
    /// Delete an expense
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        var success = await _expenseService.DeleteExpenseAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Expense with ID {id} not found or deletion failed" });
        }
        return NoContent();
    }

    /// <summary>
    /// Get expense summary by user
    /// </summary>
    [HttpGet("summary/user/{userId}")]
    public async Task<ActionResult<List<ExpenseSummary>>> GetExpenseSummaryByUser(int userId)
    {
        var summary = await _expenseService.GetExpenseSummaryByUserAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Get expense summary by category
    /// </summary>
    [HttpGet("summary/category")]
    public async Task<ActionResult<List<CategorySummary>>> GetExpenseSummaryByCategory()
    {
        var summary = await _expenseService.GetExpenseSummaryByCategoryAsync();
        return Ok(summary);
    }
}
