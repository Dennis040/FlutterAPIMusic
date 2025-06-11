namespace WebAPI.DTO
{
    public class SongDto
    {
        public int SongId { get; set; }
        public string? SongName { get; set; }
        public string? SongImage { get; set; }
        public string? LinkSong { get; set; }
        public string? LinkLrc { get; set; }
        public int? Views { get; set; }
        public string? ArtistName { get; set; } // Đây là tên nghệ sĩ
    }
}
