using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;

namespace WasteCollectionSystem.Pages
{
    public class SelectLanguageModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return RedirectToPage("/Index");
        }
    }
}
