using FreelanceGuard.Api.Models;

namespace FreelanceGuard.Api.Repositories;

/// <summary>
/// Defines the contract for employee data access operations.
/// This interface abstracts the data persistence layer, allowing for easy testing and future database migrations.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Retrieves all employees from the database asynchronously.
    /// </summary>
    /// <returns>A collection of all employees in the system.</returns>
    Task<IEnumerable<Employee>> GetAllAsync();

    /// <summary>
    /// Creates a new employee record in the database asynchronously.
    /// </summary>
    /// <param name="employee">The employee entity to be created.</param>
    /// <returns>The ID of the newly created employee record.</returns>
    Task<int> CreateAsync(Employee employee);

    /// <summary>
    /// Initializes the database schema by creating the Employees table if it does not exist.
    /// This method is useful for development and demo scenarios to ensure the database is properly set up.
    /// </summary>
    Task InitDatabaseAsync();
}