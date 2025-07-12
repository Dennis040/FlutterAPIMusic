using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZaloPayController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        private readonly string app_id = "2553";
        private readonly string key1 = "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
        private readonly string key2 = "kLtgPl8HHhfvMuDHPwKfgfsY4Ydm9eIz";
        private readonly string createEndpoint = "https://sb-openapi.zalopay.vn/v2/create";
        private readonly string queryEndpoint = "https://sb-openapi.zalopay.vn/v2/query";

        public ZaloPayController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("payment")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest createOrderRequest)
        {


            var transID = new Random().Next(100000, 999999);
            var app_trans_id = DateTime.Now.ToString("yyMMdd") + "_" + transID;
            var app_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var embed_data = "{}";
            var items = JsonSerializer.Serialize(new List<object>());
            var amount = createOrderRequest.amount;

            var order = new Dictionary<string, string>
            {
                {"app_id", app_id},
                {"app_trans_id", app_trans_id},
                {"app_user", "user123"},
                {"app_time", app_time.ToString()},
                {"amount", amount.ToString()},
                {"item", items},
                {"embed_data", embed_data},
                {"description", $"Thanh toán gói Premium #{transID}"},
                {"bank_code", ""},
                {"callback_url", "https://yourdomain.com/callback"}
            };

                string data = string.Join("|", new[] {
                app_id,
                app_trans_id,
                order["app_user"],
                order["amount"],
                order["app_time"],
                order["embed_data"],
                order["item"]
                });

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key1));
            var mac = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();
            order.Add("mac", mac);

            var request = new HttpRequestMessage(HttpMethod.Post, createEndpoint)
            {
                Content = new FormUrlEncodedContent(order)
            };

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return Content(responseContent, "application/json");
        }


        [HttpPost("callback")]
        public IActionResult Callback([FromBody] JsonElement body)
        {
            var data = body.GetProperty("data").GetString();
            var reqMac = body.GetProperty("mac").GetString();

            var mac = BitConverter.ToString(new HMACSHA256(Encoding.UTF8.GetBytes(key2))
                .ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();

            if (reqMac != mac)
            {
                return Ok(new { return_code = -1, return_message = "mac not equal" });
            }

            var dataJson = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            var appTransId = dataJson["app_trans_id"]?.ToString();
            Console.WriteLine($"update order's status = success where app_trans_id = {appTransId}");

            return Ok(new { return_code = 1, return_message = "success" });
        }

        [HttpPost("check-status-order")]
        public async Task<IActionResult> CheckOrderStatus([FromBody] JsonElement json)
        {
            var app_trans_id = json.GetProperty("app_trans_id").GetString();
            var postData = new Dictionary<string, string>
            {
                {"app_id", app_id},
                {"app_trans_id", app_trans_id!}
            };

            var data = app_id + "|" + app_trans_id + "|" + key1;
            var mac = BitConverter.ToString(new HMACSHA256(Encoding.UTF8.GetBytes(key1))
                .ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();

            postData.Add("mac", mac);

            var request = new HttpRequestMessage(HttpMethod.Post, queryEndpoint)
            {
                Content = new FormUrlEncodedContent(postData)
            };

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Content(content, "application/json");
        }
    }
}
