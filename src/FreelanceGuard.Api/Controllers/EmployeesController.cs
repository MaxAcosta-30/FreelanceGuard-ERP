using FreelanceGuard.Api.Models;
using FreelanceGuard.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreelanceGuard.Api.Controllers;

/// <summary>
/// REST API controller for managing employee resources.
/// This controller provides endpoints for retrieving and creating employee records.
/// All endpoints are protected by the <see cref="Middleware.LicenseGuardMiddleware"/>,
/// which validates the license status before allowing access.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeesController"/> class.
    /// </summary>
    /// <param name="repository">The employee repository for data access operations.</param>
    public EmployeesController(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Retrieves all employees from the system.
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><strong>200 OK:</strong> Returns a list of all employees.</description></item>
    /// <item><description><strong>402 Payment Required:</strong> Returned by the license guard middleware if license validation fails.</description></item>
    /// </list>
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _repository.GetAllAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Creates a new employee record in the system.
    /// </summary>
    /// <param name="employee">The employee entity to be created.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><strong>201 Created:</strong> Employee successfully created. Returns the created employee with the generated ID.</description></item>
    /// <item><description><strong>400 Bad Request:</strong> Invalid employee data provided.</description></item>
    /// <item><description><strong>402 Payment Required:</strong> Returned by the license guard middleware if license validation fails.</description></item>
    /// </list>
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Create(Employee employee)
    {
        var id = await _repository.CreateAsync(employee);
        return CreatedAtAction(nameof(GetAll), new { id }, employee);
    }
}