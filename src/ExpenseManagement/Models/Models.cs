namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ReceiptImageUrl { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseCreate
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? ReceiptImageUrl { get; set; }
}

public class ExpenseUpdate
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? ReceiptImageUrl { get; set; }
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ExpenseSummary
{
    public string StatusName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "GBP";
}

public class CategorySummary
{
    public string CategoryName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "GBP";
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
