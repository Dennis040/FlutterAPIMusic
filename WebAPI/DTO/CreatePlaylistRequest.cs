namespace WebAPI.DTO
{
    public class CreatePlaylistRequest
    {
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
        public List<int> SongIds { get; set; } = new();
    }
}
