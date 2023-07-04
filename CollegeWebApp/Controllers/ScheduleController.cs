using CollegeWebApp.BLL;
using CollegeWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Xml;

namespace CollegeWebApp.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly Repository _repository;

        public ScheduleController(Repository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var groupId = HttpContext.Session.GetString("profileInfo")?.Split(';')[3];

            if (!String.IsNullOrEmpty(groupId))
            {
                string code = await _repository.GetGroupCodeByIdAsync(int.Parse(groupId));
                ViewBag.GroupCode = code;
                byte[] schedule = await _repository.GetScheduleByIdAsync(int.Parse(groupId));
                if (schedule == null)
                {
                    ViewBag.NoScheduleMsg = $"No Schedule For {code}";
                }
                else
                {
                string htmlNode = ExcelToHtmlTable(schedule);
                ViewBag.Schedule = htmlNode;
                }
            }
            else
            {
                var claim = User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
                bool isPermit = bool.Parse(claim.Value.Split(';')[3]); // user.ScheduleManagement
                ViewBag.Permit = isPermit;
                ViewBag.GroupList = await _repository.GetGroupsListAsync();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.GroupList = await _repository.GetGroupsListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(string groupSelect, IFormFile excelFile)
        {
            _repository.AddScheduleById(new ScheduleDTO { GroupId = int.Parse(groupSelect), ExcelDoc = ParseExcelFileToByteArray(excelFile) });

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ItemSelected(int groupId)
        {
            if (groupId != 0)
            {
                byte[] schedule = await _repository.GetScheduleByIdAsync(groupId);
                if (schedule != null)
                {
                    string htmlNode = ExcelToHtmlTable(schedule);
                    return PartialView("_SchedulePartial", htmlNode);
                }
                else
                {
                    return PartialView("_SchedulePartial", "<h4>No Schedule</h4>");
                }
            }

            return RedirectToAction("Index");
        }

        public byte[] ParseExcelFileToByteArray(IFormFile excelFile)
        {
            using (var memoryStream = new MemoryStream())
            {
                excelFile.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static string ExcelToHtmlTable(byte[] excelBytes)
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            using (var stream = new MemoryStream(excelBytes))
            {
                using (var package = new ExcelPackage(stream))
                {
                    var workbook = package.Workbook;
                    var worksheet = workbook.Worksheets[0]; // Assuming the data is in the first worksheet

                    var htmlTable = "<table class='table table-striped'>";

                    // Add table head
                    htmlTable += "<thead>";
                    htmlTable += "<tr>";

                    // Iterate through the header row
                    for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? "";
                        htmlTable += $"<th>{headerValue}</th>";
                    }

                    htmlTable += "</tr>";
                    htmlTable += "</thead>";

                    // Add table body
                    htmlTable += "<tbody>";

                    // Iterate through the rows (excluding the header row)
                    for (int row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
                    {
                        htmlTable += "<tr>";

                        // Iterate through the columns
                        for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            if (cellValue is DateTime dateTimeValue)
                            {
                                cellValue = dateTimeValue.ToShortTimeString();
                            }
                            var formattedValue = cellValue?.ToString() ?? "";
                            htmlTable += $"<td>{formattedValue}</td>";
                        }

                        htmlTable += "</tr>";
                    }

                    htmlTable += "</tbody>";
                    htmlTable += "</table>";

                    return htmlTable;
                }
            }
        }



    }
}
