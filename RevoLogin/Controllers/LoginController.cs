using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Text.Json;
using System.Threading.Tasks;


namespace RevoLogin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation($"Username: {request.Username}, Password: {request.Password}");

            var cookiesJson = await PerformLogin(request.Username, request.Password);

            return Ok(cookiesJson);
        }

        private async Task<string> PerformLogin(string username, string password)
        {
            var browserFetcher = new BrowserFetcher();
            var revisionInfo = await browserFetcher.DownloadAsync(); // Removed DefaultRevision
            var executablePath = revisionInfo.GetExecutablePath(); // Use the method to get the executable path
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath // Use the downloaded browser's executable path
            });

            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://evobuild.ai/n8n/signin", WaitUntilNavigation.Networkidle0);

            var input = await page.QuerySelectorAllAsync("input");
            if (input.Length > 0)
            {
                await input[0].TypeAsync(username);
                await input[1].TypeAsync(password);
            }

            var buttons = await page.QuerySelectorAllAsync("button");
            if (buttons.Length > 0)
            {
                await buttons[1].ClickAsync();
            }

            await page.WaitForNavigationAsync();
            var cookies = await page.GetCookiesAsync();

            // Convert cookies to JSON string
            var cookiesJson = JsonSerializer.Serialize(cookies);

            await browser.CloseAsync();

            return cookiesJson;
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
