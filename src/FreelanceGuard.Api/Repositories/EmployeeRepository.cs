using System.Data;
using Dapper;
using FreelanceGuard.Api.Models;
using Microsoft.Data.SqlClient;

namespace FreelanceGuard.Api.Repositories;

/// <summary>
/// Provides data access operations for employee entities using Dapper and SQL Server.
/// This repository implements the repository pattern to encapsulate database logic and provide
/// a clean abstraction layer for the business logic.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeRepository"/> class.
    /// </summary>
    /// <param name="config">The configuration instance to retrieve the database connection string.</param>
    /// <exception cref="ArgumentNullException">Thrown when the connection string is not found in configuration.</exception>
    public EmployeeRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
                            ?? throw new ArgumentNullException(nameof(config), "Connection string not found");
    }

    /// <summary>
    /// Creates a new SQL Server database connection using the configured connection string.
    /// </summary>
    /// <returns>An <see cref="IDbConnection"/> instance for database operations.</returns>
    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <inheritdoc/>
    public async Task InitDatabaseAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employees' AND xtype='U')
            CREATE TABLE Employees (
                Id INT PRIMARY KEY IDENTITY(1,1),
                FullName NVARCHAR(100),
                Position NVARCHAR(100),
                Salary DECIMAL(18,2)
            )";
        
        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<Employee>("SELECT * FROM Employees");
    }

    /// <inheritdoc/>
    public async Task<int> CreateAsync(Employee employee)
    {
        var sql = "INSERT INTO Employees (FullName, Position, Salary) VALUES (@FullName, @Position, @Salary); SELECT CAST(SCOPE_IDENTITY() as int)";
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, employee);
    }
}