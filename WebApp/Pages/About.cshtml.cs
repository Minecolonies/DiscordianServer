﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages
{
    [Authorize]
    public class AboutModel : PageModel
    {
        public void OnGet()
        {
        }

        public void OnPost()
        { 
            HttpContext.SignOutAsync();
        }
    }
}