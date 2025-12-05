using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public interface IDatabaseService
{
    Task<SqlConnection> GetConnectionAsync();
    Task<bool> TestConnectionAsync();
    string GetConnectionError();
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private string _connectionError = string.Empty;
    private bool _connectionTested = false;
    private bool _isConnected = false;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync();
            _isConnected = true;
            _connectionError = string.Empty;
            return connection;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _connectionError = $"Database connection failed: {ex.Message}";
            _logger.LogError(ex, "Failed to connect to database");
            
            // Return connection anyway so callers can handle the error
            return connection;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        if (_connectionTested)
        {
            return _isConnected;
        }

        try
        {
            using var connection = await GetConnectionAsync();
            if (connection.State == ConnectionState.Open)
            {
                _isConnected = true;
                _connectionError = string.Empty;
            }
            else
            {
                _isConnected = false;
                _connectionError = "Unable to connect to database";
            }
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _connectionError = $"Database connection test failed: {ex.Message}";
            _logger.LogError(ex, "Database connection test failed");
        }

        _connectionTested = true;
        return _isConnected;
    }

    public string GetConnectionError()
    {
        return _connectionError;
    }
}
