namespace WebAPI.DTO
{
    public class UserLibDto
    {
        public List<PlaylistUserDto> Playlists { get; set; } = new();
        public List<ArtistDto> FavoriteArtists { get; set; } = new();
    }
}
