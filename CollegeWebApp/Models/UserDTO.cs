namespace CollegeWebApp.Models
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Patronymic { get; set; }
        public string ProfilePic { get; set; }
        public int? GroupId { get; set; }
        public int RoleId { get; set; }
        public bool ScheduleManagement { get; set; }
        public bool UserManagement { get; set; }
        public bool NewsManagement { get; set; }
        public bool BooksManagment { get; set; }
    }
}
