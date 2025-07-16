using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
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

                // Lấy context chi tiết về dữ liệu âm nhạc
                var musicContext = await GetDetailedMusicContextAsync(request.Message);

                var apiKey = _config["OpenRouter:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return StatusCode(500, new { error = "OpenRouter API key chưa được cấu hình" });

                // Tạo system prompt thông minh hơn
                var systemPrompt = BuildAdvancedSystemPrompt(musicContext);

                var payload = new
                {
                    model = "mistralai/mistral-7b-instruct",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = request.Message }
                    },
                    temperature = 0.7,
                    max_tokens = 800
                };

                // Setup headers cho OpenRouter
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://yourdomain.com");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "MusicApp Chatbot");

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

        private async Task<string> GetDetailedMusicContextAsync(string userMessage)
        {
            var context = new List<string>();
            var lowerMessage = userMessage.ToLower();

            // 1. Tìm kiếm bài hát theo tên hoặc nghệ sĩ
            await SearchSongsAsync(lowerMessage, context);

            // 2. Tìm kiếm nghệ sĩ
            await SearchArtistsAsync(lowerMessage, context);

            // 3. Tìm kiếm album
            await SearchAlbumsAsync(lowerMessage, context);

            // 4. Tìm kiếm theo thể loại
            await SearchByGenreAsync(lowerMessage, context);

            // 5. Thống kê và số liệu
            await GetStatisticsAsync(lowerMessage, context);

            // 6. Tìm kiếm theo từ khóa chung
            await SearchByKeywordsAsync(lowerMessage, context);

            return string.Join("\n", context);
        }

        private async Task SearchSongsAsync(string message, List<string> context)
        {
            if (ContainsAnyKeyword(message, new[] { "bài hát", "song", "nhạc", "hát", "bài", "track" }))
            {
                // Tìm kiếm chính xác và fuzzy search
                var songs = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .Include(s => s.Type)
                    .Where(s => EF.Functions.Like(s.SongName.ToLower(), $"%{ExtractSongName(message)}%") ||
                               EF.Functions.Like(s.Artist.ArtistName.ToLower(), $"%{ExtractArtistName(message)}%") ||
                               s.SongName.ToLower().Contains(message) ||
                               s.Artist.ArtistName.ToLower().Contains(message))
                    .Take(10)
                    .Select(s => new
                    {
                        s.SongName,
                        ArtistName = s.Artist.ArtistName,
                        AlbumName = s.Album.AlbumName,
                        TypeName = s.Type.NameType,
                    })
                    .ToListAsync();

                if (songs.Any())
                {
                    var songList = songs.Select(s => $"'{s.SongName}' - {s.ArtistName} (Album: {s.AlbumName}, Thể loại: {s.TypeName})").ToList();
                    context.Add($"🎵 Bài hát tìm thấy ({songs.Count} kết quả):\n" + string.Join("\n", songList));
                }
            }
        }

        private async Task SearchArtistsAsync(string message, List<string> context)
        {
            if (ContainsAnyKeyword(message, new[] { "nghệ sĩ", "artist", "ca sĩ", "singer", "người hát" }))
            {
                var artists = await _context.Artists
                    .Include(a => a.Songs)
                    .ThenInclude(s => s.Type)
                    .Where(a => EF.Functions.Like(a.ArtistName.ToLower(), $"%{message}%") ||
                               a.ArtistName.ToLower().Contains(message))
                    .Take(8)
                    .Select(a => new
                    {
                        a.ArtistName,
                        SongCount = a.Songs.Count(),
                        PopularGenres = a.Songs.Select(s => s.Type.NameType).Distinct().Take(3).ToList()
                    })
                    .ToListAsync();

                if (artists.Any())
                {
                    var artistList = artists.Select(a =>
                        $"🎤 {a.ArtistName} - {a.SongCount} bài hát. Thể loại: {string.Join(", ", a.PopularGenres)}"
                    ).ToList();
                    context.Add($"Nghệ sĩ tìm thấy ({artists.Count} kết quả):\n" + string.Join("\n", artistList));
                }
            }
        }

        private async Task SearchAlbumsAsync(string message, List<string> context)
        {
            if (ContainsAnyKeyword(message, new[] { "album", "đĩa nhạc", "tuyển tập" }))
            {
                var albums = await _context.Albums
                    .Include(a => a.Songs)
                    .ThenInclude(s => s.Artist)
                    .Where(a => EF.Functions.Like(a.AlbumName.ToLower(), $"%{message}%") ||
                               a.AlbumName.ToLower().Contains(message))
                    .Take(8)
                    .Select(a => new
                    {
                        a.AlbumName,
                        ArtistName = a.Songs.FirstOrDefault().Artist.ArtistName,
                        SongCount = a.Songs.Count()
                    })
                    .ToListAsync();

                if (albums.Any())
                {
                    var albumList = albums.Select(a =>
                        $"💿 {a.AlbumName} - {a.ArtistName}, {a.SongCount} bài hát)"
                    ).ToList();
                    context.Add($"Album tìm thấy ({albums.Count} kết quả):\n" + string.Join("\n", albumList));
                }
            }
        }

        private async Task SearchByGenreAsync(string message, List<string> context)
        {
            if (ContainsAnyKeyword(message, new[] { "thể loại", "genre", "loại nhạc", "kiểu nhạc" }))
            {
                var genres = await _context.Types
                    .Include(t => t.Songs)
                    .ThenInclude(s => s.Artist)
                    .Where(t => EF.Functions.Like(t.NameType.ToLower(), $"%{message}%") ||
                               t.NameType.ToLower().Contains(message))
                    .Take(5)
                    .Select(t => new
                    {
                        t.NameType,
                        SongCount = t.Songs.Count(),
                        PopularArtists = t.Songs.Select(s => s.Artist.ArtistName).Distinct().Take(3).ToList()
                    })
                    .ToListAsync();

                if (genres.Any())
                {
                    var genreList = genres.Select(g =>
                        $"🎼 {g.NameType} - {g.SongCount} bài hát. Nghệ sĩ nổi bật: {string.Join(", ", g.PopularArtists)}"
                    ).ToList();
                    context.Add($"Thể loại tìm thấy ({genres.Count} kết quả):\n" + string.Join("\n", genreList));
                }
            }
        }

        private async Task GetStatisticsAsync(string message, List<string> context)
        {
            if (ContainsAnyKeyword(message, new[] { "thống kê", "tổng", "số lượng", "có bao nhiêu", "stats" }))
            {
                var stats = new
                {
                    TotalSongs = await _context.Songs.CountAsync(),
                    TotalArtists = await _context.Artists.CountAsync(),
                    TotalAlbums = await _context.Albums.CountAsync(),
                    TotalTypes = await _context.Types.CountAsync(),
                    PopularGenres = await _context.Types
                        .Include(t => t.Songs)
                        .OrderByDescending(t => t.Songs.Count())
                        .Take(5)
                        .Select(t => new { t.NameType, Count = t.Songs.Count() })
                        .ToListAsync(),
                    TopArtists = await _context.Artists
                        .Include(a => a.Songs)
                        .OrderByDescending(a => a.Songs.Count())
                        .Take(5)
                        .Select(a => new { a.ArtistName, Count = a.Songs.Count() })
                        .ToListAsync()
                };

                context.Add($"📊 Thống kê thư viện nhạc:\n" +
                           $"• Tổng số bài hát: {stats.TotalSongs}\n" +
                           $"• Tổng số nghệ sĩ: {stats.TotalArtists}\n" +
                           $"• Tổng số album: {stats.TotalAlbums}\n" +
                           $"• Tổng số thể loại: {stats.TotalTypes}\n\n" +
                           $"🔥 Top 5 thể loại phổ biến:\n" +
                           string.Join("\n", stats.PopularGenres.Select(g => $"• {g.NameType}: {g.Count} bài hát")) + "\n\n" +
                           $"⭐ Top 5 nghệ sĩ có nhiều bài hát nhất:\n" +
                           string.Join("\n", stats.TopArtists.Select(a => $"• {a.ArtistName}: {a.Count} bài hát")));
            }
        }

        private async Task SearchByKeywordsAsync(string message, List<string> context)
        {
            // Tìm kiếm chung dựa trên keywords
            var keywords = ExtractKeywords(message);
            if (keywords.Any())
            {
                var generalSearch = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .Include(s => s.Type)
                    .Where(s => keywords.Any(k =>
                        s.SongName.ToLower().Contains(k) ||
                        s.Artist.ArtistName.ToLower().Contains(k) ||
                        s.Album.AlbumName.ToLower().Contains(k) ||
                        s.Type.NameType.ToLower().Contains(k)))
                    .Take(5)
                    .Select(s => new
                    {
                        s.SongName,
                        ArtistName = s.Artist.ArtistName,
                        AlbumName = s.Album.AlbumName,
                        TypeName = s.Type.NameType
                    })
                    .ToListAsync();

                if (generalSearch.Any() && !context.Any())
                {
                    var resultList = generalSearch.Select(s =>
                        $"🔍 {s.SongName} - {s.ArtistName} ({s.TypeName})"
                    ).ToList();
                    context.Add($"Kết quả tìm kiếm liên quan:\n" + string.Join("\n", resultList));
                }
            }
        }

        private bool ContainsAnyKeyword(string text, string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private string ExtractSongName(string message)
        {
            // Trích xuất tên bài hát từ câu hỏi
            var patterns = new[]
            {
                @"bài hát (.+?)(?:\s|$)",
                @"song (.+?)(?:\s|$)",
                @"nhạc (.+?)(?:\s|$)",
                @"hát (.+?)(?:\s|$)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }

            return message;
        }

        private string ExtractArtistName(string message)
        {
            // Trích xuất tên nghệ sĩ từ câu hỏi
            var patterns = new[]
            {
                @"nghệ sĩ (.+?)(?:\s|$)",
                @"ca sĩ (.+?)(?:\s|$)",
                @"artist (.+?)(?:\s|$)",
                @"singer (.+?)(?:\s|$)",
                @"của (.+?)(?:\s|$)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }

            return message;
        }

        private List<string> ExtractKeywords(string message)
        {
            // Loại bỏ stop words và trích xuất keywords quan trọng
            var stopWords = new[] { "tôi", "tìm", "kiếm", "về", "cho", "của", "có", "là", "trong", "và", "hay", "hoặc", "với", "để", "bạn", "ai", "gì", "như", "thế", "nào", "đâu", "sao", "the", "is", "are", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };

            var words = message.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .ToList();

            return words;
        }

        private string BuildAdvancedSystemPrompt(string musicContext)
        {
            var basePrompt = @"Bạn là Music AI Assistant - trợ lý AI chuyên nghiệp cho ứng dụng nghe nhạc.

KHẢ NĂNG CỦA BẠN:
• Tìm kiếm và giới thiệu bài hát, nghệ sĩ, album theo yêu cầu
• Phân tích và thống kê dữ liệu âm nhạc
• Gợi ý nhạc phù hợp dựa trên sở thích
• Giải đáp thắc mắc về âm nhạc và nghệ sĩ
• So sánh và đánh giá các tác phẩm âm nhạc

CÁCH TRาาẢ LỜI:
• Sử dụng emojis phù hợp (🎵, 🎤, 💿, 🎼, ⭐, 🔥)
• Trả lời chi tiết, có cấu trúc rõ ràng
• Ưu tiên thông tin từ dữ liệu thực tế được cung cấp
• Nếu không tìm thấy thông tin, hãy gợi ý tìm kiếm khác
• Luôn thân thiện và nhiệt tình

ĐỊNH DẠNG RESPONSE:
• Sử dụng bullet points và numbering khi cần thiết
• Phân chia thông tin thành các section rõ ràng
• Đưa ra gợi ý tiếp theo nếu phù hợp";

            if (!string.IsNullOrEmpty(musicContext))
            {
                basePrompt += $"\n\n📋 DỮ LIỆU TỪ THƯ VIỆN NHẠC:\n{musicContext}";
                basePrompt += "\n\n⚠️ QUAN TRỌNG: Ưu tiên sử dụng dữ liệu thực tế từ thư viện nhạc ở trên để trả lời. Chỉ bổ sung thông tin chung về âm nhạc khi cần thiết.";
            }
            else
            {
                basePrompt += "\n\n📝 LƯU Ý: Không tìm thấy dữ liệu cụ thể từ thư viện nhạc. Hãy đưa ra gợi ý tìm kiếm hoặc thông tin chung về âm nhạc.";
            }

            return basePrompt;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions()
        {
            try
            {
                var suggestions = new
                {
                    PopularQueries = new[]
                    {
                        "Tìm bài hát pop hay nhất",
                        "Nghệ sĩ nào có nhiều bài hát nhất?",
                        "Thống kê thư viện nhạc",
                        "Gợi ý nhạc ballad",
                        "Album mới nhất"
                    },
                    QuickActions = new[]
                    {
                        new { text = "🎵 Tìm bài hát", query = "Tìm bài hát " },
                        new { text = "🎤 Tìm nghệ sĩ", query = "Tìm nghệ sĩ " },
                        new { text = "📊 Thống kê", query = "Thống kê thư viện nhạc" },
                        new { text = "🎼 Thể loại", query = "Có những thể loại nhạc nào?" }
                    }
                };

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error getting suggestions", message = ex.Message });
            }
        }

        [HttpGet("search/{query}")]
        public async Task<IActionResult> QuickSearch(string query)
        {
            try
            {
                var results = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .Include(s => s.Type)
                    .Where(s => EF.Functions.Like(s.SongName.ToLower(), $"%{query.ToLower()}%") ||
                               EF.Functions.Like(s.Artist.ArtistName.ToLower(), $"%{query.ToLower()}%"))
                    .Take(10)
                    .Select(s => new
                    {
                        s.SongId,
                        s.SongName,
                        ArtistName = s.Artist.ArtistName,
                        AlbumName = s.Album.AlbumName,
                        TypeName = s.Type.NameType,
                    })
                    .ToListAsync();

                return Ok(new { results, count = results.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search error", message = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public int? UserId { get; set; }
    }

    public class ChatResponse
    {
        public string Reply { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public bool HasMusicContext { get; set; }
    }
}