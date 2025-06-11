using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Song
{
    public int SongId { get; set; }

    public string? SongName { get; set; }

    public string? SongImage { get; set; }

    public int? AlbumId { get; set; }

    public int? ArtistId { get; set; }

    public string? LinkSong { get; set; }

    public int? TypeId { get; set; }

    public int? Views { get; set; }

    public string? LinkLrc { get; set; }

    public virtual Album? Album { get; set; }

    public virtual Artist? Artist { get; set; }

    public virtual ICollection<HistorySong> HistorySongs { get; set; } = new List<HistorySong>();

    public virtual ICollection<SongHistory> SongHistories { get; set; } = new List<SongHistory>();

    public virtual Type? Type { get; set; }

    public virtual ICollection<PlaylistUser> IdPlaylistUsers { get; set; } = new List<PlaylistUser>();

    public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
