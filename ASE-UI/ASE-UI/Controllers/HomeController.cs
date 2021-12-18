using System.Text;
using System.Threading.Tasks;
using ASE_UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace ASE_UI.Controllers
{
    public class HomeController : Controller
    {
        private const string _client_id = "1091126786905-9qetr8clasin3ph65blhn9jut088c1kf.apps.googleusercontent.com";
        private const string _client_secret = "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";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CommentIndex()
        {
            await HttpContext.Session.LoadAsync();

            if (HttpContext.Session.TryGetValue("code", out var byteValue))
            {
                return View("Comment", new AseViewModel { Token = Encoding.ASCII.GetString(byteValue) });
            }

            return View("Comment");
        }

        public async Task<IActionResult> IndexTab()
        {
            await HttpContext.Session.LoadAsync();

            if (HttpContext.Session.TryGetValue("code", out var byteValue))
            {
                return View("Index", new AseViewModel { Token = Encoding.ASCII.GetString(byteValue) });
            }

            return View("Index");
        }

        public async Task<IActionResult> Clear()
        {
            await HttpContext.Session.LoadAsync();

            if (HttpContext.Session.TryGetValue("code", out var byteValue))
            {
                return View("Index", new AseViewModel { Token = Encoding.ASCII.GetString(byteValue) });
            }

            return View();
        }

        public IActionResult LogOut()
        {
            return View("Index", new AseViewModel());
        }

        public IActionResult Auth()
        {
            var url = $"https://accounts.google.com/o/oauth2/auth?client_id={_client_id}&nonce=123&scope=https://www.googleapis.com/auth/userinfo.email openid&response_type=code&redirect_uri=https://lvh.me:5002/Home/ExchangeToken";
            return Redirect(url);
        }

        public async Task<IActionResult> ExchangeToken()
        {
            var queries = Request.Query;
            if (!queries.ContainsKey("code"))
            {
                return View();
            }

            var code = queries["code"].ToString();
            var client = new RestClient("https://accounts.google.com");
            var request = new RestRequest("o/oauth2/token", Method.POST);

            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", "https://lvh.me:5002/Home/ExchangeToken");
            request.AddParameter("client_id", _client_id);
            request.AddParameter("client_secret", _client_secret);
            request.AddParameter("grant_type", "authorization_code");

            var result = await client.ExecuteAsync<TokenResponse>(request);

            HttpContext.Session.Set("code", Encoding.ASCII.GetBytes(result.Data.access_token));
            await HttpContext.Session.CommitAsync();

            return View("Index", new AseViewModel { Token = result.Data.access_token });
        }

        public async Task<IActionResult> GetRecommendation(string countryCode, string state)
        {
            await HttpContext.Session.LoadAsync();
            var token = HttpContext.Session.TryGetValue("code", out var byteValue);

            var model = new AseViewModel { Token = Encoding.ASCII.GetString(byteValue) };

            if (string.IsNullOrEmpty(countryCode) && string.IsNullOrEmpty(state))
            {

                var defaultResult = await GetDefaultRecommendationAsync();

                model.RecommendationResult = defaultResult;
                return View("Index", model);
            }

            if (!string.IsNullOrEmpty(countryCode) && !string.IsNullOrEmpty(state))
            {
                var result = await GetSpecificRecommendationAsync(countryCode, state);
                model.RecommendationResult = result;
                return View("Index", model);
            }

            model.RecommendationResult = "CountryCode and State must be both provided or both empty.";
            return View("Index", model);
        }

        private async Task<string> GetDefaultRecommendationAsync()
        {
            var client = new RestClient("https://localhost:5001");
            var request = new RestRequest("api/recommendations", Method.GET);

            var result = await client.ExecuteAsync(request);
            return result.Content;
        }

        private async Task<string> GetSpecificRecommendationAsync(string countryCode, string state)
        {
            var client = new RestClient("https://localhost:5001");
            var request = new RestRequest($"api/recommendations/country/{countryCode}/state/{state}", Method.GET);

            var result = await client.ExecuteAsync(request);
            return result.Content;
        }

        public async Task<IActionResult> GetComment(string countryCode, string state)
        {
            await HttpContext.Session.LoadAsync();
            var token = HttpContext.Session.TryGetValue("code", out var byteValue);

            var model = new AseViewModel { Token = Encoding.ASCII.GetString(byteValue) };

            if (!string.IsNullOrEmpty(countryCode) && !string.IsNullOrEmpty(state))
            {
                var result = await GetSpecificCommentAsync(countryCode, state);
                model.CommentResult = result;
                return View("Comment", model);
            }

            model.CommentResult = "CountryCode and State must be both provided.";
            return View("Comment", model);
        }

        private async Task<string> GetSpecificCommentAsync(string countryCode, string state)
        {
            var client = new RestClient("https://localhost:5001");
            var request = new RestRequest($"api/comment/country/{countryCode}/state/{state}", Method.GET);

            var result = await client.ExecuteAsync(request);
            return result.Content;
        }

        public async Task<IActionResult> PostComment(string countryCode, string state, string comment)
        {
            await HttpContext.Session.LoadAsync();
            HttpContext.Session.TryGetValue("code", out var byteValue);
            var token = Encoding.ASCII.GetString(byteValue);

            var model = new AseViewModel { Token = token };

            if (!string.IsNullOrEmpty(countryCode) && !string.IsNullOrEmpty(state))
            {
                var result = await PostSpecificCommentAsync(countryCode, state, comment, token);
                model.CommentResult = result;
                return View("Comment", model);
            }

            model.CommentResult = "CountryCode and State must be both provided.";
            return View("Comment", model);
        }

        private async Task<string> PostSpecificCommentAsync(string countryCode, string state, string comment, string token)
        {
            var client = new RestClient("https://localhost:5001");
            var request = new RestRequest($"api/comment/country/{countryCode}/state/{state}", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddJsonBody(new
            {
                CommentStr = comment,
                UserIdStr = "NewUser1"
            });
            request.AddHeader("Authorization", $"Bearer {token}");

            var result = await client.ExecuteAsync(request);
            return result.Content;
        }
    }
}
