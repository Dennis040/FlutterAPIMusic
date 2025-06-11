using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTO;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DemoMusicContext _context;

        public UsersController(DemoMusicContext context)
        {
            _context = context;
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

            return Ok(user);
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

    }
}
