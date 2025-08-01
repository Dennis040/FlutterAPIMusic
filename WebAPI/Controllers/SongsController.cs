﻿using System;
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
    public class SongsController : ControllerBase
    {
        private readonly DemoMusicContext _context;

        public SongsController(DemoMusicContext context)
        {
            _context = context;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopSongs([FromQuery] int limit = 10)
        {
            var topSongs = await _context.Songs
                .OrderByDescending(s => s.Views)
                .Take(limit)
                .Select(s => new {
                    s.SongId,
                    s.SongName,
                    s.SongImage,
                    s.LinkSong,
                    s.LinkLrc,
                    s.Views,
                    artistName = s.Artist.ArtistName // <-- Lấy tên nghệ sĩ từ bảng Artist
                })
                .ToListAsync();

            return Ok(topSongs);
        }

        //// GET: api/Songs
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Song>>> GetSongs()
        //{
        //    return await _context.Songs.ToListAsync();
        //}

        // GET: api/Songs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Song>> GetSong(int id)
        {
            var song = await _context.Songs.FindAsync(id);

            if (song == null)
            {
                return NotFound();
            }

            return song;
        }

        // PUT: api/Songs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSong(int id, Song song)
        {
            if (id != song.SongId)
            {
                return BadRequest();
            }

            _context.Entry(song).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SongExists(id))
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

        // POST: api/Songs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Song>> PostSong(Song song)
        {
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSong", new { id = song.SongId }, song);
        }

        // DELETE: api/Songs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SongExists(int id)
        {
            return _context.Songs.Any(e => e.SongId == id);
        }
        // GET: api/Songs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongs()
        {
            var songs = await _context.Songs
                .Include(s => s.Artist) // join bảng Artist
                .Select(s => new SongDto
                {
                    SongId = s.SongId,
                    SongName = s.SongName,
                    SongImage = s.SongImage,
                    LinkSong = s.LinkSong,
                    LinkLrc = s.LinkLrc,
                    Views = s.Views,
                    ArtistName = s.Artist != null ? s.Artist.ArtistName : "Unknown"
                })
                .ToListAsync();

            return Ok(songs);
        }
        [HttpGet("recommendations/{userId}")]
        public async Task<IActionResult> GetRecommendations(int userId)
        {
            var topTypeId = await _context.SongHistories
                .Where(h => h.UserId == userId)
                .Join(_context.Songs, h => h.SongId, s => s.SongId, (h, s) => s.TypeId)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var listenedSongIds = await _context.SongHistories
                .Where(h => h.UserId == userId)
                .Select(h => h.SongId)
                .ToListAsync();

            var recommendations = await _context.Songs
                .Where(s => s.TypeId == topTypeId && !listenedSongIds.Contains(s.SongId))
                .Take(10)
                .ToListAsync();

            return Ok(recommendations);
        }
    }
}
