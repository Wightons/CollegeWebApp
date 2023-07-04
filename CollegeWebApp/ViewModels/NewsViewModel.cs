using System.ComponentModel.DataAnnotations;

namespace CollegeWebApp.ViewModels
{
    public class NewsViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }
        [Required]
        public string Body { get; set; }
    }
}
