using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ContactPro.Controllers
{       
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailService;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, UserManager<AppUser> userManager, ApplicationDbContext context, IEmailSender emailService)
        {
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        [Authorize]
        public async Task<IActionResult> ContactMe(string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string? appUserId = _userManager.GetUserId(User);
            
            if (appUserId == null)
            {
                return NotFound();
            }

            AppUser? appUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == appUserId);

            return View(appUser);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactMe([Bind("FirstName,LastName,Email")] AppUser appUser, string? message)
        {
            string? swalMessage = string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    string? contactEmail = _configuration["ContactMeEmail"] ?? Environment.GetEnvironmentVariable("ContactMeEmail");
                    await _emailService.SendEmailAsync(contactEmail!, $"1st Contact Message From - {appUser.FullName} - {appUser.Email}", message!);
                    swalMessage = "Email sent successfully!";
                }
                catch (Exception)
                {

                    throw;
                }


            } else
            {
                swalMessage = "Error: Unable to send email.";

            }
            return RedirectToAction("ContactMe", new { swalMessage });
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}