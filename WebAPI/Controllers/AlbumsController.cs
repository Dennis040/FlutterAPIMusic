using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly DemoMusicContext _context;

        public AlbumsController(DemoMusicContext context)
        {
            _context = context;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopAlbums([FromQuery] int limit = 3)
        {
            var albums = await _context.Albums
                .Select(a => new {
                    a.AlbumId,
                    a.AlbumName,
                    a.AlbumImage,
                    artistName = a.Artist.ArtistName // navigation property
                })
                .Take(limit)
                .ToListAsync();

            return Ok(albums);
        }

        [HttpGet("{albumId}/songs")]
        public async Task<IActionResult> GetSongsByAlbum(int albumId)
        {
            var songs = await _context.Songs
                .Where(s => s.AlbumId == albumId)
                .Select(s => new {
                    s.SongId,
                    s.SongName,
                    s.SongImage,
                    s.LinkSong,
                    s.LinkLrc,
                    s.Views,
                    artistName = s.Artist.ArtistName // hoặc lấy từ navigation property nếu có
                })
                .ToListAsync();

            return Ok(songs);
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbums()
        {
            return await _context.Albums.ToListAsync();
        }

        // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Album>> GetAlbum(int id)
        {
            var album = await _context.Albums.FindAsync(id);

            if (album == null)
            {
                return NotFound();
            }

            return album;
        }

        // PUT: api/Albums/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlbum(int id, Album album)
        {
            if (id != album.AlbumId)
            {
                return BadRequest();
            }

            _context.Entry(album).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlbumExists(id))
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

        // POST: api/Albums
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Album>> PostAlbum(Album album)
        {
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAlbum", new { id = album.AlbumId }, album);
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AlbumExists(int id)
        {
            return _context.Albums.Any(e => e.AlbumId == id);
        }
    }
}
