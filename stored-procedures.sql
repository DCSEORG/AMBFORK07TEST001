-- stored-procedures.sql
-- Stored procedures for Expense Management System
-- Using CREATE OR ALTER for idempotency

SET NOCOUNT ON;
GO

-- Get all expenses
CREATE OR ALTER PROCEDURE dbo.GetAllExpenses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        ec.CategoryName,
        e.Amount,
        e.Currency,
        e.Description,
        e.ExpenseDate,
        e.StatusId,
        es.StatusName,
        e.ReceiptImageUrl,
        e.ApprovedBy,
        e.ApprovedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    ORDER BY e.CreatedAt DESC;
END
GO

-- Get expense by ID
CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        ec.CategoryName,
        e.Amount,
        e.Currency,
        e.Description,
        e.ExpenseDate,
        e.StatusId,
        es.StatusName,
        e.ReceiptImageUrl,
        e.ApprovedBy,
        e.ApprovedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- Get expenses by user ID
CREATE OR ALTER PROCEDURE dbo.GetExpensesByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        ec.CategoryName,
        e.Amount,
        e.Currency,
        e.Description,
        e.ExpenseDate,
        e.StatusId,
        es.StatusName,
        e.ReceiptImageUrl,
        e.ApprovedBy,
        e.ApprovedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    WHERE e.UserId = @UserId
    ORDER BY e.CreatedAt DESC;
END
GO

-- Get expenses by status
CREATE OR ALTER PROCEDURE dbo.GetExpensesByStatus
    @StatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        ec.CategoryName,
        e.Amount,
        e.Currency,
        e.Description,
        e.ExpenseDate,
        e.StatusId,
        es.StatusName,
        e.ReceiptImageUrl,
        e.ApprovedBy,
        e.ApprovedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    WHERE e.StatusId = @StatusId
    ORDER BY e.CreatedAt DESC;
END
GO

-- Create expense
CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId INT,
    @CategoryId INT,
    @Amount DECIMAL(18,2),
    @Currency NVARCHAR(3),
    @Description NVARCHAR(500),
    @ExpenseDate DATE,
    @ReceiptImageUrl NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StatusId INT;
    
    -- Get 'Pending' status ID
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Pending';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, Amount, Currency, Description, ExpenseDate, StatusId, ReceiptImageUrl)
    VALUES (@UserId, @CategoryId, @Amount, @Currency, @Description, @ExpenseDate, @StatusId, @ReceiptImageUrl);
    
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

-- Update expense
CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId INT,
    @CategoryId INT,
    @Amount DECIMAL(18,2),
    @Currency NVARCHAR(3),
    @Description NVARCHAR(500),
    @ExpenseDate DATE,
    @ReceiptImageUrl NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET 
        CategoryId = @CategoryId,
        Amount = @Amount,
        Currency = @Currency,
        Description = @Description,
        ExpenseDate = @ExpenseDate,
        ReceiptImageUrl = @ReceiptImageUrl
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Approve expense
CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId INT,
    @ApprovedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StatusId INT;
    
    -- Get 'Approved' status ID
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ApprovedBy = @ApprovedBy,
        ApprovedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Reject expense
CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId INT,
    @RejectedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StatusId INT;
    
    -- Get 'Rejected' status ID
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        ApprovedBy = @RejectedBy,
        ApprovedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Delete expense
CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Get all users
CREATE OR ALTER PROCEDURE dbo.GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    ORDER BY u.UserName;
END
GO

-- Get user by ID
CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.UserId = @UserId;
END
GO

-- Get all categories
CREATE OR ALTER PROCEDURE dbo.GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, Description
    FROM dbo.ExpenseCategories
    ORDER BY CategoryName;
END
GO

-- Get all expense statuses
CREATE OR ALTER PROCEDURE dbo.GetAllExpenseStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName, Description
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- Get expense summary by user
CREATE OR ALTER PROCEDURE dbo.GetExpenseSummaryByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        es.StatusName,
        COUNT(*) AS ExpenseCount,
        SUM(e.Amount) AS TotalAmount,
        e.Currency
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseStatus es ON e.StatusId = es.StatusId
    WHERE e.UserId = @UserId
    GROUP BY es.StatusName, e.Currency
    ORDER BY es.StatusName;
END
GO

-- Get expense summary by category
CREATE OR ALTER PROCEDURE dbo.GetExpenseSummaryByCategory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        ec.CategoryName,
        COUNT(*) AS ExpenseCount,
        SUM(e.Amount) AS TotalAmount,
        e.Currency
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseCategories ec ON e.CategoryId = ec.CategoryId
    GROUP BY ec.CategoryName, e.Currency
    ORDER BY TotalAmount DESC;
END
GO
