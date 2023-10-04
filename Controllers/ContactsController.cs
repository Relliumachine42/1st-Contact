﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using ContactPro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using ContactPro.Services.Interfaces;
using ContactPro.Enums;

namespace ContactPro.Controllers
{
    [Authorize]
    public class ContactsController : Controller

    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;


        public ContactsController(ApplicationDbContext context,
                                  UserManager<AppUser> userManager,
                                  IImageService imageService,
                                  IAddressBookService addressBookService,
                                  IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        public async Task<IActionResult> Index(int? categoryId, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;
            string? userId = _userManager.GetUserId(User);

            List<Contact> model = new List<Contact>();

            // if statement
            if (categoryId != null)
            {
                model = (await _context.Categories.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == categoryId))!.Contacts.ToList();
            }
            else
            {
                model = await _context.Contacts.Include(c => c.Categories).Where(c => c.AppUserId == userId).ToListAsync();
            }

            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();
            ViewData["CategoriesList"] = new SelectList(categories, "Id", "Name", categoryId);

            return View(model);
        }

        public async Task<IActionResult> SearchContacts(string? searchString)
        {
            string userId = _userManager.GetUserId(User)!;
            List<Contact> contacts = await _context.Contacts.Include(c => c.Categories).Where(c => c.AppUserId == userId).ToListAsync();

            List<Contact> model = new List<Contact>();

            if (string.IsNullOrEmpty(searchString))
            {
                model = contacts;
            }
            else
            {
                model = contacts.Where(c => c.FullName!.ToLower().Contains(searchString.ToLower()))
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.LastName).ToList();
            }

            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();
            ViewData["CategoriesList"] = new MultiSelectList(categories, "Id", "Name");
            ViewData["SearchString"] = searchString;

            return View(nameof(Index), model);
        }


        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public async Task<IActionResult> Create()
        {
            string userId = _userManager.GetUserId(User)!;

            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();

            ViewData["CategoriesList"] = new MultiSelectList(categories, "Id", "Name");
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,CreatedDate,DateOfBirth,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, List<int> selected)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {

                contact.AppUserId = _userManager.GetUserId(User);

                contact.CreatedDate = DateTime.Now;

                if (contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);

                return RedirectToAction(nameof(Index));
            }

            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User)!;

            Contact? contact = await _context.Contacts
                                             .Include(c => c.Categories)
                                             .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);

            if (contact == null)
            {
                return NotFound();
            }

            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();

            List<int> categoryIds = contact.Categories.Select(c => c.Id).ToList();

            ViewData["CategoriesList"] = new MultiSelectList(categories, "Id", "Name", categoryIds);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());


            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
                                              [Bind("Id,AppUserId,FirstName,LastName,CreatedDate,DateOfBirth,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile,ImageData,ImageType")]
                                      Contact contact,
                                              List<int> selected)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    await _addressBookService.RemoveCategoriesFromContactAsync(contact.Id);
                    await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
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
            return View(contact);
        }


        [HttpGet]
        public async Task<IActionResult> EmailContact(int? id, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;
            if (id == null)
            {
                return NotFound();
            }

            string? appUserId = _userManager.GetUserId(User);
            Contact? contact = await _context.Contacts
                                             .Where(c => c.AppUserId == appUserId)
                                             .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Instantiate & Populate the EmailData
            EmailData emailData = new EmailData()
            {
                EmailAddress = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            EmailContactViewModel viewModel = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData
            };


            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? email = viewModel.EmailData?.EmailAddress;
                    string? subject = viewModel.EmailData?.EmailSubject;
                    string? htmlMessage = viewModel.EmailData?.EmailBody;

                    await _emailService.SendEmailAsync(email!, subject!, htmlMessage!);

                    // Send Sweet Alert for success
                    string? swalMessage = "Success: Email Sent!";

                    return RedirectToAction(nameof(Index), new { swalMessage = swalMessage });
                }
                catch (Exception)
                {
                    string? swalMessage = "Error: Email Failed to Send!";

                    return RedirectToAction(nameof(EmailContact), new { swalMessage = swalMessage });
                    throw;
                }

            }
            return View(viewModel);
        }


        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
