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

        [HttpGet("test")]
        public IActionResult Get() // Change return type to IActionResult
        {
            return Ok("Test ok");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation($"Username: {request.Username}, Password: {request.Password}");

            try
            {
                var cookiesJson = await PerformLogin(request.Username, request.Password);
                if (string.IsNullOrEmpty(cookiesJson))
                {
                    return StatusCode(500, new { error = "Wrong username or password" });
                }
                return Ok(cookiesJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("login2")]
        public async Task<IActionResult> Login2([FromQuery] string username, [FromQuery] string password)
        {
            _logger.LogInformation($"Username: {username}, Password: {password}");

            try
            {
                var cookiesJson = await PerformLogin(username, password);
                if (string.IsNullOrEmpty(cookiesJson))
                {
                    return StatusCode(500, new { error = "Wrong username or password" });
                }
                return Ok(cookiesJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> PerformLogin(string username, string password)
        {
            try
            {
                var executablePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
                _logger.LogInformation($"Executable path: {executablePath}");
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executablePath
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

                var navigationTask = page.WaitForNavigationAsync();
                if (await Task.WhenAny(navigationTask, Task.Delay(10000)) == navigationTask)
                {
                    var cookies = await page.GetCookiesAsync();
                    var cookiesJson = JsonSerializer.Serialize(cookies);
                    await browser.CloseAsync();
                    return cookiesJson;
                }
                else
                {
                    await browser.CloseAsync();
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while performing login.");
                throw;
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
