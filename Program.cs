using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DevSimAPI 
{
    public class Program() 
    {
        public static void Main(string[] args) 
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Configure Authentication Services
            // We're telling ASP.NET Core to use:
            // - Cookies to store user login state
            // - OAuth with GitHub for actual login flow
            builder.Services.AddAuthentication(options =>
            {
                // The default way to check if someone is logged in (using cookies)
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // The default way to sign in after successful login (also cookies)
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // If user is NOT logged in, this is the default "challenge" provider (GitHub OAuth)
                options.DefaultChallengeScheme = "GitHub";
            })
            // ✅ Add cookie authentication (used to remember the logged-in user)
            .AddCookie(options =>
            {
                // SameSite must be Lax for OAuth redirects to work on localhost
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            // ✅ Add GitHub OAuth configuration
            .AddOAuth("GitHub", options =>
            {
                // GitHub App credentials (from appsettings.json)
                options.ClientId = builder.Configuration["GitHub:ClientId"];
                options.ClientSecret = builder.Configuration["GitHub:ClientSecret"];

                // The endpoint where GitHub will redirect after user approves
                // (This is automatically handled by ASP.NET)
                options.CallbackPath = new PathString("/signin-github");

                // GitHub's OAuth URLs
                // Step 1: redirect user here
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";

                // Step 2: exchange code for token
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";

                // Step 3: get user info with token
                options.UserInformationEndpoint = "https://api.github.com/user";

                // ✅ Request extra permissions: "repo" means full repo access
                options.Scope.Add("repo");

                // ✅ Save tokens in the authentication cookie so we can use them later
                options.SaveTokens = true;

                // ✅ Map GitHub JSON fields to claims (login name, ID, avatar)
                options.ClaimActions.MapJsonKey("urn:github:login", "login");
                options.ClaimActions.MapJsonKey("urn:github:id", "id");
                options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                // ✅ Event to fetch user info after login
                options.Events = new OAuthEvents
                {
                    // After we get the token, call GitHub API to get user info
                    OnCreatingTicket = async context =>
                    {
                        // Build request to GitHub user API
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        // Call GitHub and get user data
                        var response = await context.Backchannel.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        // Parse JSON response and map claims
                        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        context.RunClaimActions(user.RootElement);
                    }
                };
            });

            // ✅ Add Authorization system (roles/policies if needed later)
            builder.Services.AddAuthorization();

            // ✅ Add Controllers for API endpoints
            builder.Services.AddControllers();

            // ✅ Add HttpClient factory (for calling GitHub API from our endpoints)
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // ✅ Ensure cookies work properly with OAuth redirects
            // Lax SameSite = allow cookies during cross-site redirect (GitHub → localhost)
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax
            });

            // ✅ Enable Authentication & Authorization in the request pipeline
            app.UseAuthentication();
            app.UseAuthorization();

            // ✅ Map Controller routes
            app.MapControllers();

            // ✅ Run the app
            app.Run();

        }
    }
}

