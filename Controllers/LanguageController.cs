using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WasteCollectionSystem.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            // Validate culture
            var supportedCultures = new[] { "en", "fr", "rw" };
            if (!supportedCultures.Contains(culture))
            {
                culture = "en"; // Default to English if invalid
            }

            // Set culture cookie
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/"
                }
            );

            // Redirect to return URL or home page
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToPage("/Index");
        }
    }
}
