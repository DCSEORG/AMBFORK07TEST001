using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
}

public class UserService : IUserService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<UserService> _logger;

    public UserService(IDatabaseService databaseService, ILogger<UserService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "John Doe",
                Email = "john.doe@example.com",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Jane Smith",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new User
            {
                UserId = 2,
                UserName = "Jane Smith",
                Email = "jane.smith@example.com",
                RoleId = 2,
                RoleName = "Manager",
                ManagerId = null,
                ManagerName = null,
                IsActive = true,
                CreatedAt = DateTime.Now.AddYears(-2)
            }
        };
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                return GetDummyUsers();
            }

            using var command = new SqlCommand("dbo.GetAllUsers", connection);
            command.CommandType = CommandType.StoredProcedure;

            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    RoleName = reader.GetString(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
                });
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return GetDummyUsers();
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            using var connection = await _databaseService.GetConnectionAsync();
            if (connection.State != ConnectionState.Open)
            {
                return GetDummyUsers().FirstOrDefault(u => u.UserId == id);
            }

            using var command = new SqlCommand("dbo.GetUserById", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", id);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    RoleName = reader.GetString(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return GetDummyUsers().FirstOrDefault(u => u.UserId == id);
        }
    }
}
