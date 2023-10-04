using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;

namespace ContactPro.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailService;

        public CategoriesController(ApplicationDbContext context,
                                    UserManager<AppUser> userManager,
                                    IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            List<Category> categories = await _context.Categories
                                                      .Where(c => c.AppUserId == userId)
                                                      .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User)!;

            Category? category = await _context.Categories
                                               .Include(c => c.AppUser)
                                               .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // EmailCategory Get Action 
        public async Task<IActionResult> EmailCategory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve Category info from DB
            string? appUserId = _userManager?.GetUserId(User);
            Category? category = await _context.Categories
                                               .Where(c => c.AppUserId == appUserId)
                                               .Include(c => c.Contacts)
                                               .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Prep Data for the View
            // - Instantiate and Hydrate the Model
            IEnumerable<string> emails = category.Contacts.Select(c => c.Email)!;

            EmailData emailData = new EmailData()
            {
                GroupName = category.Name,
                EmailAddress = string.Join(";", emails),
                EmailSubject = $"Group Message: {category.Name}"
            };

            ViewData["EmailContacts"] = category.Contacts.ToList();

            // Return the View along with the Model
            return View(emailData);
        }

        // EmailCategory Post Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailCategory(EmailData emailData)
        {
            if (ModelState.IsValid)
            {
                string? swalMessage = string.Empty;

                try
                {
                    string? email = emailData.EmailAddress;
                    string? subject = emailData.EmailSubject;
                    string? htmlMessage = emailData.EmailBody;

                    await _emailService.SendEmailAsync(email!, subject!, htmlMessage!);

                    swalMessage = "Success: Email Sent!";
                    return RedirectToAction(nameof(Index), "Contacts", new { swalMessage = swalMessage });

                }
                catch (Exception)
                {
                    swalMessage = "Error: Email Failed to Send!";
                    return RedirectToAction(nameof(EmailCategory), new { swalMessage = swalMessage });

                    throw;
                }

            }

            return View(emailData);
        }



        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                category.AppUserId = _userManager.GetUserId(User);

                _context.Add(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User)!;

            Category? category = await _context.Categories
                                               .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                                         .Include(c => c.Contacts)
                                         .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return (_context.Categories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
