using Microsoft.AspNetCore.Mvc;

namespace AI_Driven_Water_Supply.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("set-cookie")]
        public IActionResult SetCookie([FromBody] TokenModel model)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,   // 🔒 JavaScript isay read nahi kar sakta
                Secure = true,     // 🔒 Sirf HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("supabase_token", model.AccessToken, cookieOptions);
            Response.Cookies.Append("supabase_refresh", model.RefreshToken, cookieOptions);

            return Ok();
        }

        [HttpPost("remove-cookie")]
        public IActionResult RemoveCookie()
        {
            Response.Cookies.Delete("supabase_token");
            Response.Cookies.Delete("supabase_refresh");
            return Ok();
        }
    }

    public class TokenModel
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}