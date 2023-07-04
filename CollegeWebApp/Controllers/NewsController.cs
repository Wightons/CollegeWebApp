using CollegeWebApp.BLL;
using CollegeWebApp.Models;
using CollegeWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollegeWebApp.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {
        private readonly Repository _repository;

        private bool GetClaim()
        {
            var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
            bool isPermit = bool.Parse(claim.Value.Split(';')[5]);
            return isPermit;
        }
        

        public NewsController(Repository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsPermit = GetClaim();
            
            List<NewsDTO> list = await _repository.GetNewsAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            if (GetClaim())
                return View();
            else
                return RedirectToAction("Index");
            
        }

        [HttpPost]
        public async Task<IActionResult> Add(NewsViewModel model)
        {
            if (GetClaim())
            {
                NewsDTO dto = new NewsDTO { Title = model.Title, Body = model.Body };
                _repository.AddNews(dto);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (GetClaim())
            {
                _repository.DeleteNewsById(id);
            }

            return RedirectToAction("Index");
        }

    }
}
