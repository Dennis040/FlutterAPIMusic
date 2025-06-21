namespace WebAPI.DTO
{
    public class PlaylistUserDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? UserId { get; set; }
        public List<int> SongIds { get; set; } = new();
    }

}
