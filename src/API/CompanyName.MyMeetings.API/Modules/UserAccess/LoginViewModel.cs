using System.ComponentModel.DataAnnotations;

namespace CompanyName.MyMeetings.API.Modules.UserAccess;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public string ReturnUrl { get; set; }
}
