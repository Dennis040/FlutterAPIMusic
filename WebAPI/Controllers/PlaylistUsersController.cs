using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTO;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistUsersController : ControllerBase
    {
        private readonly DemoMusicContext _context;

        public PlaylistUsersController(DemoMusicContext context)
        {
            _context = context;
        }

        // GET: api/PlaylistUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlaylistUser>>> GetPlaylistUsers()
        {
            return await _context.PlaylistUsers.ToListAsync();
        }

        // GET: api/PlaylistUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistUser>> GetPlaylistUser(int id)
        {
            var playlistUser = await _context.PlaylistUsers.FindAsync(id);

            if (playlistUser == null)
            {
                return NotFound();
            }

            return playlistUser;
        }

        // PUT: api/PlaylistUsers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlaylistUser(int id, PlaylistUser playlistUser)
        {
            if (id != playlistUser.Id)
            {
                return BadRequest();
            }

            _context.Entry(playlistUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistUserExists(id))
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
        [HttpPut("PlaylistUsersName/{id}")]
        public async Task<IActionResult> PutPlaylistName(int id, [FromBody] UpdatePlaylistNameDto dto)
        {
            var playlist = await _context.PlaylistUsers.FindAsync(id);
            if (playlist == null)
                return NotFound();

            playlist.Name = dto.PlaylistName;
            await _context.SaveChangesAsync();

            return Ok(); // hoặc NoContent()
        }


        // POST: api/PlaylistUsers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PlaylistUser>> PostPlaylistUser(PlaylistUser playlistUser)
        {
            _context.PlaylistUsers.Add(playlistUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlaylistUser", new { id = playlistUser.Id }, playlistUser);
        }

        [HttpPost("CreateWithSongs")]
        public async Task<ActionResult<PlaylistUser>> CreatePlaylistWithSongs(CreatePlaylistRequest request)
        {
            // 1. Tạo mới playlist
            var playlist = new PlaylistUser
            {
                Name = request.Name,
                UserId = request.UserId
            };
            _context.PlaylistUsers.Add(playlist);
            await _context.SaveChangesAsync();

            // 2. Thêm bài hát vào bảng Playlist_User_Song thông qua navigation
            foreach (var songId in request.SongIds)
            {
                var song = await _context.Songs.FindAsync(songId);
                if (song != null)
                {
                    playlist.Songs.Add(song); // dùng collection navigation
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlaylistUser), new { id = playlist.Id }, new PlaylistUserDto
            {
                Id = playlist.Id,
                Name = playlist.Name,
                UserId = playlist.UserId,
                SongIds = playlist.Songs.Select(s => s.SongId).ToList()
            });
        }
        [HttpPost("{playlistId}/songs/{songId}")]
        public async Task<IActionResult> AddSongToPlaylist(int playlistId, int songId)
        {
            var playlist = await _context.PlaylistUsers
                .Include(p => p.Songs)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null) return NotFound("Playlist không tồn tại");

            var song = await _context.Songs.FindAsync(songId);
            if (song == null) return NotFound("Bài hát không tồn tại");

            if (!playlist.Songs.Any(s => s.SongId == songId))
            {
                playlist.Songs.Add(song);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }


        [HttpGet("playlists/{playlistId}/songs")]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongsInPlaylist(int playlistId)
        {
            var playlist = await _context.PlaylistUsers
                .Include(p => p.Songs)
                    .ThenInclude(s => s.Artist)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
            {
                return NotFound("Playlist not found.");
            }

            var songs = playlist.Songs
                .Select(s => new SongDto
            {
                    SongId = s.SongId,
                    SongName = s.SongName,
                    SongImage = s.SongImage,
                    LinkSong = s.LinkSong,
                    LinkLrc = s.LinkLrc,
                    Views = s.Views,
                    ArtistName = s.Artist?.ArtistName,
            }).ToList();

            return Ok(songs);
        }

        // DELETE: api/PlaylistUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylistUser(int id)
        {
            var playlistUser = await _context.PlaylistUsers.FindAsync(id);
            if (playlistUser == null)
            {
                return NotFound();
            }

            _context.PlaylistUsers.Remove(playlistUser);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlaylistUserExists(int id)
        {
            return _context.PlaylistUsers.Any(e => e.Id == id);
        }
        [HttpDelete("playlists/{playlistId}/songs/{songId}")]
        public async Task<IActionResult> RemoveSongFromPlaylist(int playlistId, int songId)
        {
            var playlist = await _context.PlaylistUsers
                .Include(p => p.Songs)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return NotFound("Playlist không tồn tại");

            var song = playlist.Songs.FirstOrDefault(s => s.SongId == songId);
            if (song == null)
                return NotFound("Bài hát không tồn tại trong playlist");

            playlist.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return Ok("Đã xoá bài hát khỏi playlist");
        }

    }
}
