using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UserManagementApi.DTOs;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
    {
        var users = await _userService.GetAllUsersAsync(page, pageSize, query, ct);
        return Ok(new { page, pageSize, total = users.Count(), items = users });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id, CancellationToken ct = default)
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(UserCreateDto userDto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var normalized = userDto?.Email?.Trim();

        if (string.IsNullOrEmpty(normalized))
            return BadRequest(new { email = new[] { "Email is required." } });

        // Simple case-insensitive uniqueness check against normalized email
        var users = await _userService.GetAllUsersAsync(1, 0, null, ct);

        if (users.Any(u => string.Equals(u.Email?.Trim(), normalized, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Email already in use." });

        var createdUser = await _userService.CreateUserAsync(userDto, ct);
        if (createdUser == null)
        {
            _logger.LogWarning("CreateUser returned null for payload {@Payload}", userDto);
            return StatusCode(500, "Could not create user.");
        }
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userDto, CancellationToken ct = default)
    {
        if (userDto == null) return BadRequest();
        if (string.IsNullOrEmpty(userDto.Email))
            return BadRequest(new { email = new[] { "Email is required." } });

        var normalized = userDto.Email.Trim();
        // Simple case-insensitive uniqueness check against normalized email
        var users = await _userService.GetAllUsersAsync(1, 0, null, ct);
        if (users.Any(u => string.Equals(u.Email?.Trim(), normalized, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Email already in use." });

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _userService.UpdateUserAsync(id, userDto, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct = default)
    {
        var deleted = await _userService.DeleteUserAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
    