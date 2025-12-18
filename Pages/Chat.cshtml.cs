using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WasteCollectionSystem.Pages
{
    public class ChatModel : PageModel
    {
        public IActionResult OnGet()
        {
            var feature = HttpContext.Features.Get<IRequestCultureFeature>();
            // Use the cookie provider to check if explicit choice was made
            var cookie = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];

            if (string.IsNullOrEmpty(cookie))
            {
                return RedirectToPage("/SelectLanguage");
            }

            return Page();
        }
    }
}
