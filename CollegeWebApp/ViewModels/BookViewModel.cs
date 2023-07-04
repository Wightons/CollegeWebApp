using CollegeWebApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CollegeWebApp.ViewModels
{
    public class BookViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100)]
        public string Title { get; set; }
        [Required(ErrorMessage = "Author is required")]
        [MaxLength(100)]
        public string Author { get; set; }
        public IFormFile CoverImageFile { get; set; }
        [Required(ErrorMessage = "Pdf File is required")]
        public IFormFile PdfFile { get; set; }
        [Required]
        public List<string> SelectedGroups { get; set; }

    }
}
