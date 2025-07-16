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

            // Optionally hide the password when returning the user object  
            user.Password = null;
            // Tạo access token (ví dụ JWT)
            var token = GenerateJwtToken(user); // bạn cần tự tạo hàm này

            // Trả user + token
            return Ok(new
            {
                accessToken = token,
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
            });
            return Ok(user);
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
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] WebAPI.DTO.RegisterRequest request)
        {
            // Kiểm tra username hoặc email đã tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Username hoặc Email đã tồn tại." });
            }

            // Tạo user mới
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password, // Có thể mã hoá sau
                Phone = request.Phone,
                Role = request.Role ?? "member"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Xoá password trước khi trả về
            newUser.Password = null;

            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, newUser);
        }

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
