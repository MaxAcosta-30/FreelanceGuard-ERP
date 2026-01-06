namespace FreelanceGuard.Api.Models;

/// <summary>
/// Represents an employee entity in the FreelanceGuard ERP system.
/// This model is used for data persistence and API responses.
/// </summary>
public class Employee
{
    /// <summary>
    /// Gets or sets the unique identifier for the employee.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the employee.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job position or role of the employee.
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the salary amount for the employee.
    /// </summary>
    public decimal Salary { get; set; }
}