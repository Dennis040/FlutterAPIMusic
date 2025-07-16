using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly DemoMusicContext _context;

        public SearchController(DemoMusicContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            keyword = keyword?.Trim().ToLower() ?? "";

            var songs = await _context.Songs
                .Where(s => s.SongName.ToLower().Contains(keyword) || s.Artist.ArtistName.ToLower().Contains(keyword))
                .Select(s => new {
                    s.SongId,
                    s.SongName,
                    s.SongImage,
                    s.LinkSong,
                    s.LinkLrc,
                    s.Views,
                    artistName = s.Artist.ArtistName
                })
                .ToListAsync();

            var albums = await _context.Albums
                .Where(a => a.AlbumName.ToLower().Contains(keyword))
                .Select(a => new {
                    a.AlbumId,
                    a.AlbumName,
                    a.AlbumImage
                })
                .ToListAsync();

            var artists = await _context.Artists
                .Where(a => a.ArtistName.ToLower().Contains(keyword))
                .ToListAsync();

            var playlists = await _context.Playlists
                .Where(p => p.PlaylistName.ToLower().Contains(keyword))
                .ToListAsync();

            return Ok(new
            {
                songs,
                albums,
                artists,
                playlists
            });
        }
    }

}
