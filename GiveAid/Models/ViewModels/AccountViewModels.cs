using System.ComponentModel.DataAnnotations;
using GiveAid.Models;

namespace GiveAid.Models.ViewModels
{
    /// <summary>Member login form.</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your e-mail address.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    /// <summary>Admin login form.</summary>
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Enter the admin username or e-mail.")]
        [Display(Name = "Username or e-mail")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter the password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string? ReturnUrl { get; set; }
    }

    /// <summary>Registration form: personal + professional details.</summary>
    public class RegisterViewModel
    {
        // ----- personal -----
        [Required(ErrorMessage = "Enter your full name.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3 to 100 characters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z .'-]*$", ErrorMessage = "Name can contain letters, spaces, dots and hyphens only.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Enter your e-mail address.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [StringLength(150)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter your mobile number.")]
        [RegularExpression(@"^(\+92|0)3\d{2}-?\d{7}$", ErrorMessage = "Use a mobile number like 0300-1234567 or +923001234567.")]
        [Display(Name = "Mobile number")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Select your gender.")]
        public string Gender { get; set; } = "";

        [Required(ErrorMessage = "Enter your date of birth.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Enter your city.")]
        [StringLength(60)]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "Enter your country.")]
        [StringLength(60)]
        public string Country { get; set; } = "Pakistan";

        // ----- professional -----
        [Required(ErrorMessage = "Enter your occupation.")]
        [StringLength(100)]
        public string Occupation { get; set; } = "";

        [StringLength(150)]
        [Display(Name = "Organisation / institute")]
        public string? Organization { get; set; }

        [Required(ErrorMessage = "Select your highest qualification.")]
        [StringLength(100)]
        public string Qualification { get; set; } = "";

        [Display(Name = "Causes you care about")]
        public List<string> Interests { get; set; } = new();

        // ----- password -----
        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain letters and at least one number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Type the password again.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";

        /// <summary>Must be ticked - checked in AccountController.Register.</summary>
        public bool AcceptTerms { get; set; }
    }

    /// <summary>Data shown on the member's own dashboard.</summary>
    /// <summary>Enter the 6-digit code sent to the e-mail address.</summary>
    public class VerifyEmailViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter the 6-digit code.")]
        [RegularExpression(@"^\s*\d{3}\s?\d{3}\s*$", ErrorMessage = "The code has 6 digits.")]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = "";

        public bool Remember { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your e-mail address.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter the 6-digit code.")]
        [RegularExpression(@"^\s*\d{3}\s?\d{3}\s*$", ErrorMessage = "The code has 6 digits.")]
        [Display(Name = "Code from the e-mail")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Choose a new password.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain letters and at least one number.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; } = "";

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class MemberDashboardViewModel
    {
        public Member Member { get; set; } = new();
        public List<Donation> Donations { get; set; } = new();
        public List<SupportQuery> Queries { get; set; } = new();
        public List<ProgrammeInterest> Interests { get; set; } = new();
        public List<Invitation> Invitations { get; set; } = new();
        public decimal TotalGiven => Donations.Sum(d => d.Amount);
    }
}
