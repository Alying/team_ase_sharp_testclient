using ASE_UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public IActionResult LogOut()
        {
            return View("Index", new IndexViewModel());
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

            return View("Index", new IndexViewModel { Token = result.Data.access_token });
        }

        public async Task<IActionResult> GetRecommendation(string countryCode, string state) 
        {
             await HttpContext.Session.LoadAsync();
            var token = HttpContext.Session.TryGetValue("code", out var byteValue);

            var model = new IndexViewModel { Token = Encoding.ASCII.GetString(byteValue) };

            if (string.IsNullOrEmpty(countryCode) && string.IsNullOrEmpty(state))
            {

                var defaultResult = await GetDefaultRecommendationAsync();

                model.Result = defaultResult;
                return View("Index", model);
            }

            if (!string.IsNullOrEmpty(countryCode) && !string.IsNullOrEmpty(state)) 
            {
                var result = await GetSpecificRecommendationAsync(countryCode, state);
                model.Result = result;
                return View("Index", model);
            }

            model.Result = "CountryCode and State must be both provided or both empty.";
            return View("Index", model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    }
}
