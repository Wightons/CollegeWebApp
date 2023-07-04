using System.ComponentModel.DataAnnotations;

namespace CollegeWebApp.ViewModels
{
    public class UserViewModel
    {
        [Required]
        [MaxLength(500)]
        public string Email { get; set; }
        [Required]
        [MaxLength(256)]
        public string Password { get; set; }
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string Surname { get; set; }
        [Required]
        [MaxLength(100)]
        public string Patronymic { get; set; }
        public IFormFile ProfilePic { get; set; }
        public int? SelectedGroupId { get; set; }
        public int SelectedRoleId { get; set; }
    }
}
