using ASE_UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ASE_UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetRecommendation(string countryCode, string state) 
        {
            if (string.IsNullOrEmpty(countryCode) && string.IsNullOrEmpty(state))
            {

                var defaultResult = await GetDefaultRecommendationAsync();

                return View("Index", new IndexViewModel { Result = defaultResult });
            }

            if (!string.IsNullOrEmpty(countryCode) && !string.IsNullOrEmpty(state)) 
            {
                var result = await GetSpecificRecommendationAsync(countryCode, state);
                return View("Index", new IndexViewModel { Result = result });
            }

            return View("Index", new IndexViewModel { Result = "CountryCode and State must be both provided or both empty." });
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
