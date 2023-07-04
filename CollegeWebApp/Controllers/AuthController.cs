using CollegeWebApp.BLL;
using CollegeWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollegeWebApp.Controllers
{
    
    public class AuthController : Controller
    {
        private static string ErrMsg = "Wrong Credentials";
        private readonly Repository _repository;
        public AuthController(Repository repository)
        {
            _repository = repository;
        }
        public async Task<IActionResult> Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {

            UserDTO user = await _repository.GetUserIfExistAsync(email, password);

            if (user == null)
            {
                ViewBag.ErrorMessage = ErrMsg;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, $"{user.Id};{user.Email};{user.PasswordHash};{user.ScheduleManagement};{user.UserManagement};{user.NewsManagement};{user.BooksManagment}")
            };

            ClaimsIdentity id = new ClaimsIdentity(claims, "auth", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

            HttpContext.Session.SetString("pfp", user.ProfilePic);
            HttpContext.Session.SetString("profileInfo", $"{user.FirstName};{user.Surname};{user.Patronymic};{user.GroupId}");

            return RedirectToAction("Index", "Profile");
        }


        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("session");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }



        

    }
}
