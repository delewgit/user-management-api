using System.ComponentModel.DataAnnotations;
using UserManagementApi.Models;

namespace UserManagementApi.DTOs;

public record UserDto(int Id, string FirstName, string LastName, string Email, string Password = null)
{
    public UserDto(User u) : this(u.Id, u.FirstName, u.LastName, u.Email, u.Password) { }
}

public class UserCreateDto
{
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string Password { get; set; }
}

    