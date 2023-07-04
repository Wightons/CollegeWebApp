using CollegeWebApp.BLL;
using CollegeWebApp.Models;
using CollegeWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NuGet.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

namespace CollegeWebApp.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly Repository _repository;

        private bool GetClaim()
        {
            var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
            bool isPermit = bool.Parse(claim.Value.Split(';')[6]);
            return isPermit;
        }

        public BooksController(Repository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {

            var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);

            bool isPermit = bool.Parse(claim.Value.Split(';')[6]); // user.BooksManagment
            int userId = int.Parse(claim.Value.Split(';')[0]);

            List<BookDTO>? list = new List<BookDTO>();

            if (isPermit)
                list = await _repository.GetBooksListAsync();
            else
                list = await _repository.GetBooksListAsync(userId);

            ViewBag.Permit = isPermit;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            if (GetClaim())
            {
                ViewBag.GroupsList = await _repository.GetGroupsListAsync();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(BookViewModel model)
        {
            if (GetClaim())
            {
                string CoverImageFileBytes = Helpers.ConvertToBase64String(model.CoverImageFile);
                byte[] PdfFileBytes = Helpers.ConvertToByteArray(model.PdfFile);

                BookDTO book = new BookDTO { Title = model.Title, Author = model.Author, Cover = CoverImageFileBytes };

                List<int> intList = model.SelectedGroups.Select(int.Parse).ToList();

                _repository.AddBook(book, PdfFileBytes, intList);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int bookId)
        {
            if (GetClaim())
            {
                _repository.DeleteBookById(bookId);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int bookId)
        {
            if (GetClaim())
            { 
                BookDTO dto = await _repository.GetBookByIdAsync(bookId);
                return View(dto);
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Edit(BookDTO dto, IFormFile cover)
        {
            if (GetClaim())
            {

                if (cover != null)
                {
                    dto.Cover = Helpers.ConvertToBase64String(cover);
                }

                _repository.UpdateBook(dto);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DownloadPdf(int bookId)
        {
            byte[] pdfBytes = await _repository.GetBookBytesByIdAsync(bookId);

            if (pdfBytes != null)
            {

                HttpContext.Response.ContentType = "application/pdf";
                HttpContext.Response.Headers.Add("Content-Disposition", "attachment; filename=book.pdf");

                await HttpContext.Response.Body.WriteAsync(pdfBytes, 0, pdfBytes.Length);
            }

            return new EmptyResult();
        }

    }
}

