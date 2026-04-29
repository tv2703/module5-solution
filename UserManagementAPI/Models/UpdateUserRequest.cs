using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    /// <summary>
    /// DTO used when updating an existing user.
    /// Bug fixes applied (Copilot analysis):
    ///   - Added AllowEmptyStrings=false to block whitespace-only strings
    ///   - Added MinLength to prevent single-character names
    ///   - Added StringLength cap on Email, Department, Role (were unlimited)
    /// </summary>
    public class UpdateUserRequest
    {
        // BUG FIX: AllowEmptyStrings=false rejects "   " (whitespace-only)
        [Required(ErrorMessage = "First name is required.", AllowEmptyStrings = false)]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters.")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.", AllowEmptyStrings = false)]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters.")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.", AllowEmptyStrings = false)]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        // BUG FIX: Email had no upper-bound
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.", AllowEmptyStrings = false)]
        // BUG FIX: Department and Role had no length constraints at all
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 100 characters.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.", AllowEmptyStrings = false)]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Role must be between 2 and 100 characters.")]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
