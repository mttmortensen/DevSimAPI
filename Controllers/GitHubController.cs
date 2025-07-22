using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DevSimAPI.Controllers
{
    [ApiController]
    // Base route: api/github
    [Route("api/[controller]")] 
    public class GitHubController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // ✅ Use IHttpClientFactory for creating HttpClient instances (best practice)
        public GitHubController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ✅ This endpoint starts the OAuth login process
        // When you call GET /api/github/login:
        // - ASP.NET issues a "challenge" to GitHub using OAuth
        // - Redirects user to GitHub login page
        // - After approval, GitHub redirects back to /signin-github (middleware handles this)
        [Route("login")]
        public IActionResult Login()
        {
            // AuthenticationProperties lets us define where to redirect after successful login
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "GitHub");
        }

        // ✅ This endpoint fetches the user's GitHub repositories
        // Requires user to be logged in (via [Authorize])
        [HttpGet("repos")]
        [Authorize]
        public async Task<IActionResult> GetRepos()
        {
            // ✅ Retrieve the GitHub access token that was saved in the authentication cookie
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            // ✅ If there's no token, the user is not authenticated
            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized();

            // ✅ Create an HttpClient using the factory (prevents socket issues)
            var client = _httpClientFactory.CreateClient();

            // ✅ Add Authorization header with the GitHub access token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // ✅ GitHub requires a User-Agent header in every API request
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DevSimApp", "1.0"));

            // ✅ Call GitHub API to get the list of user repos
            var response = await client.GetAsync("https://api.github.com/user/repos");

            // ✅ Read the response body as text
            var content = await response.Content.ReadAsStringAsync();

            // ✅ Parse the JSON and return it in the response
            return Ok(JsonDocument.Parse(content));
        }
    }
}
