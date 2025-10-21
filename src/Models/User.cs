using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.Models;

public class User
{
    public int Id { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    // Replace any plaintext Password property with PasswordHash
    // Do not expose the hash via APIs (use [JsonIgnore] if returning User directly)
    [JsonIgnore]
    public string Password { get; set; }
}