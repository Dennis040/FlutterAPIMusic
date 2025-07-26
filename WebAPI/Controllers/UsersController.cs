using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPI.DTO;
using WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using Org.BouncyCastle.Crypto.Generators;


namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DemoMusicContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(DemoMusicContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Users/{userId}/history/{songId}
        [HttpPost("{userId}/history/{songId}")]
        public async Task<IActionResult> AddToHistory(int userId, int songId)
        {
            var existing = await _context.SongHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.SongId == songId);

            if (existing != null)
            {
                existing.PlayTime = DateTime.UtcNow; // cập nhật thời gian nghe
            }
            else
            {
                var newHistory = new SongHistory
                {
                    UserId = userId,
                    SongId = songId,
                    PlayTime = DateTime.UtcNow
                };
                _context.SongHistories.Add(newHistory);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpGet("{userId}/history")]
        public async Task<IActionResult> GetUserHistory(int userId)
        {
            var history = await _context.SongHistories
                .Where(h => h.UserId == userId)
                .Include(h => h.Song)
                .OrderByDescending(h => h.PlayTime) 
                .Select(h => new
                {
                    h.Song.SongId,
                    h.Song.SongName,
                    h.Song.Artist.ArtistName,
                    h.Song.SongImage
                })
                .ToListAsync();

            return Ok(history);
        }

        // DELETE: api/Users/{userId}/history
        [HttpDelete("{userId}/history")]
        public async Task<IActionResult> ClearUserHistory(int userId)
        {
            var histories = await _context.SongHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            if (!histories.Any())
                return NoContent();

            _context.SongHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{userId}/favorite-songs/{songId}")]
        public async Task<IActionResult> AddFavoriteSong(int userId, int songId)
        {
            var user = await _context.Users
                .Include(u => u.Songs)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var song = await _context.Songs.FindAsync(songId);

            if (user == null || song == null)
                return NotFound();

            // Kiểm tra đã tồn tại chưa
            if (!user.Songs.Any(s => s.SongId == songId))
            {
                user.Songs.Add(song);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpDelete("{userId}/favorite-songs/{songId}")]
        public async Task<IActionResult> RemoveFavoriteSong(int userId, int songId)
        {
            var user = await _context.Users
                .Include(u => u.Songs)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            var song = user.Songs.FirstOrDefault(s => s.SongId == songId);

            if (song == null)
                return NotFound("Song not in favorites.");

            user.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("{userId}/favorite-songs")]
        public async Task<IActionResult> GetFavoriteSongs(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Songs)
                .Include(u => u.Artists)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User not found.");

            var songs = user.Songs
                .Select(s => new SongDto
                {
                    SongId = s.SongId,
                    SongName = s.SongName,
                    SongImage = s.SongImage,
                    LinkSong = s.LinkSong,
                    LinkLrc = s.LinkLrc,
                    Views = s.Views,
                    ArtistName = s.Artist != null ? s.Artist.ArtistName : "Unknown"
                }).ToList();

            return Ok(songs);
        }



        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] WebAPI.DTO.LoginRequest request)
        {
            // Fix: Use 'Email' property from LoginRequest instead of 'Username'  
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    (u.Username == request.Username || u.Email == request.Username)
                    && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });
            }
            if (!user.IsEmailVerified)
            {
                return BadRequest(new
                {
                    message = "Tài khoản chưa được xác thực email. Vui lòng kiểm tra email để xác thực.",
                    errorCode = "EMAIL_NOT_VERIFIED",
                    userId = user.UserId,
                    email = user.Email,
                    username = user.Username
                });
            }

            // Optionally hide the password when returning the user object  
            user.Password = null;
            // Tạo access token (ví dụ JWT)
            var token = GenerateJwtToken(user); // bạn cần tự tạo hàm này

            // Trả user + token
            return Ok(new
            {
                message = "Đăng nhập thành công",
                accessToken = token,
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                role = user.Role,
                isEmailVerified = user.IsEmailVerified
            });
        }
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        // Backend API Controller
        [HttpPost("register")]
        public async Task<ActionResult<object>> Register([FromBody] WebAPI.DTO.RegisterRequest request)
        {
            try
            {
                // Kiểm tra username hoặc email đã tồn tại
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Username hoặc Email đã tồn tại." });
                }

                // Tạo verification code
                var verificationCode = GenerateVerificationCode();
                var codeExpiry = DateTime.UtcNow.AddMinutes(15); // Code hết hạn sau 15 phút

                // Tạo user mới với trạng thái chưa verify
                var newUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password, // Mã hóa password
                    Phone = request.Phone,
                    Role = request.Role ?? "member",
                    IsEmailVerified = false,
                    EmailVerificationCode = verificationCode,
                    EmailVerificationExpiry = codeExpiry,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Gửi email verification
                await SendVerificationEmail(newUser.Email, newUser.Username, verificationCode);

                return Ok(new
                {
                    message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.",
                    userId = newUser.UserId,
                    requiresVerification = true
                });
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình đăng ký." });
            }
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId &&
                                             u.EmailVerificationCode == request.VerificationCode);

                if (user == null)
                {
                    return BadRequest(new { message = "Mã xác thực không hợp lệ." });
                }

                if (user.EmailVerificationExpiry < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Mã xác thực đã hết hạn." });
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(new { message = "Email đã được xác thực trước đó." });
                }

                // Cập nhật trạng thái verified
                user.IsEmailVerified = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationExpiry = null;
                user.EmailVerifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Xác thực email thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình xác thực." });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<ActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(request.UserId);

                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy người dùng." });
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(new { message = "Email đã được xác thực." });
                }

                // Tạo mã xác thực mới
                var verificationCode = GenerateVerificationCode();
                var codeExpiry = DateTime.UtcNow.AddMinutes(15);

                user.EmailVerificationCode = verificationCode;
                user.EmailVerificationExpiry = codeExpiry;

                await _context.SaveChangesAsync();
                await SendVerificationEmail(user.Email, user.Username, verificationCode);

                return Ok(new { message = "Đã gửi lại mã xác thực." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi lại mã xác thực." });
            }
        }

        private async Task SendVerificationEmail(string toEmail, string username, string verificationCode)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse("phantuankhang333@gmail.com"));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Xác thực tài khoản - Mã xác thực";

                var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #333;'>Xin chào {username}!</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất quá trình đăng ký, vui lòng sử dụng mã xác thực bên dưới:</p>
                
                <div style='background-color: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0; border-radius: 8px;'>
                    <h1 style='color: #007bff; font-size: 32px; margin: 0; letter-spacing: 4px;'>{verificationCode}</h1>
                </div>
                
                <p><strong>Lưu ý:</strong> Mã này sẽ hết hạn sau 15 phút.</p>
                <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
                
                <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>
                    Email này được gửi tự động, vui lòng không trả lời.
                </p>
            </div>";

                email.Body = new TextPart("html") { Text = htmlBody };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 465, true);
                await smtp.AuthenticateAsync("phantuankhang333@gmail.com", "ohjt honx nhby ydxj");
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không làm gián đoạn flow đăng ký
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Mã 6 số
        }

        //private string HashPassword(string password)
        //{
        //    // Sử dụng BCrypt hoặc phương pháp hash khác
        //    return BCrypt.Net.BCrypt.HashPassword(password);
        //}

        [HttpGet("{userId}/lib")]
        public async Task<ActionResult<UserLibDto>> GetUserProfile(int userId)
        {
            var user = await _context.Users
                .Include(u => u.PlaylistUsers)
                .Include(u => u.Artists)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = new UserLibDto
            {
                Playlists = user.PlaylistUsers.Select(p => new PlaylistUserDto
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToList(),

                FavoriteArtists = user.Artists.Select(a => new ArtistDto
                {
                    ArtistId = a.ArtistId,
                    ArtistName = a.ArtistName,
                    ArtistImage = a.ArtistImage
                }).ToList()
            };

            return Ok(result);
        }


    }

}
