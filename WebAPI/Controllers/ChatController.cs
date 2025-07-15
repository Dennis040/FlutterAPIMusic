using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly DemoMusicContext _context;

        public ChatController(IConfiguration config, HttpClient httpClient, DemoMusicContext context)
        {
            _config = config;
            _httpClient = httpClient;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ChatWithGPT([FromBody] ChatRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request?.Message))
                    return BadRequest(new { error = "Message không được để trống" });

                // Lấy context về dữ liệu âm nhạc
                var musicContext = await GetMusicContextAsync(request.Message);

                //var apiKey = _config["OpenAI:ApiKey"];
                var apiKey = _config["OpenRouter:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    //return StatusCode(500, new { error = "OpenAI API key chưa được cấu hình" });
                    return StatusCode(500, new { error = "OpenRouter API key chưa được cấu hình" });

                // Setup system prompt với context về app nhạc
                var systemPrompt = BuildSystemPrompt(musicContext);

                var payload = new
                {
                    //model = "gpt-3.5-turbo",
                    model = "mistralai/mistral-7b-instruct",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = request.Message }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                // Header cho OpenRouter
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://yourdomain.com"); // Bắt buộc
                _httpClient.DefaultRequestHeaders.Add("X-Title", "MusicApp Chatbot");
                //var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
                var response = await _httpClient.PostAsJsonAsync("https://openrouter.ai/api/v1/chat/completions", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { error = "OpenRouter API error", details = errorContent });
                }

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JsonDocument.Parse(json);
                var content = parsed.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return Ok(new ChatResponse
                {
                    Reply = content,
                    Success = true,
                    HasMusicContext = !string.IsNullOrEmpty(musicContext)
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { error = "Network error", message = ex.Message });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { error = "JSON parsing error", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        private async Task<string> GetMusicContextAsync(string userMessage)
        {
            var context = new List<string>();

            // Tìm kiếm bài hát liên quan
            if (ContainsKeywords(userMessage, new[] { "bài hát", "song", "nhạc", "hát" }))
            {
                var songs = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .Include(s => s.Type)
                    .Where(s => s.SongName.Contains(userMessage) ||
                               s.Artist.ArtistName.Contains(userMessage))
                    .Take(5)
                    .Select(s => new
                    {
                        s.SongName,
                        ArtistName = s.Artist.ArtistName,
                        AlbumName = s.Album.AlbumName,
                        TypeName = s.Type.NameType
                    })
                    .ToListAsync();

                if (songs.Any())
                {
                    context.Add($"Các bài hát liên quan: {string.Join(", ", songs.Select(s => $"{s.SongName} - {s.ArtistName}"))}");
                }
            }

            // Tìm kiếm nghệ sĩ
            if (ContainsKeywords(userMessage, new[] { "nghệ sĩ", "artist", "ca sĩ", "singer" }))
            {
                var artists = await _context.Artists
                    .Include(a => a.Songs)
                    .Where(a => a.ArtistName.Contains(userMessage))
                    .Take(5)
                    .Select(a => new
                    {
                        a.ArtistName,
                        SongCount = a.Songs.Count()
                    })
                    .ToListAsync();

                if (artists.Any())
                {
                    context.Add($"Nghệ sĩ liên quan: {string.Join(", ", artists.Select(a => $"{a.ArtistName} ({a.SongCount} bài hát)"))}");
                }
            }

            // Thống kê tổng quan
            if (ContainsKeywords(userMessage, new[] { "thống kê", "tổng", "số lượng", "có bao nhiêu" }))
            {
                var stats = new
                {
                    TotalSongs = await _context.Songs.CountAsync(),
                    TotalArtists = await _context.Artists.CountAsync(),
                    TotalAlbums = await _context.Albums.CountAsync(),
                    TotalTypes = await _context.Types.CountAsync()
                };

                context.Add($"Thống kê: {stats.TotalSongs} bài hát, {stats.TotalArtists} nghệ sĩ, {stats.TotalAlbums} album, {stats.TotalTypes} thể loại");
            }

            return string.Join("\n", context);
        }

        private bool ContainsKeywords(string text, string[] keywords)
        {
            return keywords.Any(keyword => text.ToLower().Contains(keyword.ToLower()));
        }

        private string BuildSystemPrompt(string musicContext)
        {
            var basePrompt = @"Bạn là trợ lý AI thông minh cho ứng dụng nghe nhạc. Bạn có thể:
- Trả lời các câu hỏi về âm nhạc, nghệ sĩ, album, thể loại
- Đưa ra gợi ý về bài hát hoặc nghệ sĩ
- Giải thích về âm nhạc và các thuật ngữ liên quan
- Giúp người dùng tìm kiếm và khám phá âm nhạc mới

Hãy trả lời một cách thân thiện, hữu ích và chính xác. Nếu không biết thông tin cụ thể, hãy thừa nhận và đưa ra gợi ý chung về âm nhạc.";

            if (!string.IsNullOrEmpty(musicContext))
            {
                basePrompt += $"\n\nDữ liệu từ ứng dụng:\n{musicContext}";
            }

            return basePrompt;
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public int? UserId { get; set; } // Optional: để track user context
    }

    public class ChatResponse
    {
        public string Reply { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public bool HasMusicContext { get; set; }
    }
}