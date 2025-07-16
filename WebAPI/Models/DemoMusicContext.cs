using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models;

public partial class DemoMusicContext : DbContext
{
    public DemoMusicContext()
    {
    }

    public DemoMusicContext(DbContextOptions<DemoMusicContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Album> Albums { get; set; }

    public virtual DbSet<Artist> Artists { get; set; }

    public virtual DbSet<HistorySong> HistorySongs { get; set; }

    public virtual DbSet<HoaDonAdmin> HoaDonAdmins { get; set; }

    public virtual DbSet<HoaDonArtist> HoaDonArtists { get; set; }

    public virtual DbSet<Playlist> Playlists { get; set; }

    public virtual DbSet<PlaylistUser> PlaylistUsers { get; set; }

    public virtual DbSet<Song> Songs { get; set; }

    public virtual DbSet<SongHistory> SongHistories { get; set; }

    public virtual DbSet<Type> Types { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=DemoMusic;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(e => e.AlbumId).HasName("PK__Album__97B4BE1708DFB5F1");

            entity.ToTable("Album");

            entity.Property(e => e.AlbumId).HasColumnName("AlbumID");
            entity.Property(e => e.AlbumName).HasMaxLength(50);
            entity.Property(e => e.ArtistId).HasColumnName("ArtistID");

            entity.HasOne(d => d.Artist).WithMany(p => p.Albums)
                .HasForeignKey(d => d.ArtistId)
                .HasConstraintName("FK__Album__ArtistID__5535A963");
        });

        modelBuilder.Entity<Artist>(entity =>
        {
            entity.HasKey(e => e.ArtistId).HasName("PK__Artist__25706B70611012D6");

            entity.ToTable("Artist");

            entity.Property(e => e.ArtistId).HasColumnName("ArtistID");
            entity.Property(e => e.ArtistName).HasMaxLength(50);
        });

        modelBuilder.Entity<HistorySong>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SongId }).HasName("PK__HistoryS__76A6F1C3BD479E9D");

            entity.ToTable("HistorySong");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.SongId).HasColumnName("SongID");
            entity.Property(e => e.OrderIndex).HasDefaultValue(0);

            entity.HasOne(d => d.Song).WithMany(p => p.HistorySongs)
                .HasForeignKey(d => d.SongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HistorySo__SongI__5629CD9C");

            entity.HasOne(d => d.User).WithMany(p => p.HistorySongs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HistorySo__UserI__571DF1D5");
        });

        modelBuilder.Entity<HoaDonAdmin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HoaDon_A__3214EC27159FB214");

            entity.ToTable("HoaDon_Admin");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.EndDate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.HoaDonAdmins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__HoaDon_Ad__UserI__5812160E");
        });

        modelBuilder.Entity<HoaDonArtist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HoaDon_A__3214EC27B3DBADA7");

            entity.ToTable("HoaDon_Artist");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.HoaDonArtists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__HoaDon_Ar__UserI__59063A47");
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.PlaylistId).HasName("PK__Playlist__B30167804D14572C");

            entity.ToTable("Playlist");

            entity.Property(e => e.PlaylistId).HasColumnName("PlaylistID");
            entity.Property(e => e.PlaylistName).HasMaxLength(50);

            entity.HasMany(d => d.Songs).WithMany(p => p.Playlists)
                .UsingEntity<Dictionary<string, object>>(
                    "PlaylistSong",
                    r => r.HasOne<Song>().WithMany()
                        .HasForeignKey("SongId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Playlist___SongI__5AEE82B9"),
                    l => l.HasOne<Playlist>().WithMany()
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Playlist___Playl__59FA5E80"),
                    j =>
                    {
                        j.HasKey("PlaylistId", "SongId").HasName("PK__Playlist__D22F5AEF12BE9B54");
                        j.ToTable("Playlist_Song");
                        j.IndexerProperty<int>("PlaylistId").HasColumnName("PlaylistID");
                        j.IndexerProperty<int>("SongId").HasColumnName("SongID");
                    });
        });

        modelBuilder.Entity<PlaylistUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Playlist__3214EC2798D98C5B");

            entity.ToTable("Playlist_User");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PlaylistUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Playlist___UserI__5BE2A6F2");

            entity.HasMany(d => d.Songs).WithMany(p => p.IdPlaylistUsers)
                .UsingEntity<Dictionary<string, object>>(
                    "PlaylistUserSong",
                    r => r.HasOne<Song>().WithMany()
                        .HasForeignKey("SongId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK__Playlist___SongI__5DCAEF64"),
                    l => l.HasOne<PlaylistUser>().WithMany()
                        .HasForeignKey("IdPlaylistUser")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK__Playlist___ID_Pl__5CD6CB2B"),
                    j =>
                    {
                        j.HasKey("IdPlaylistUser", "SongId").HasName("PK__Playlist__930A950CBDA5479A");
                        j.ToTable("Playlist_User_Song");
                        j.IndexerProperty<int>("IdPlaylistUser").HasColumnName("ID_Playlist_User");
                        j.IndexerProperty<int>("SongId").HasColumnName("SongID");
                    });
        });

        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(e => e.SongId).HasName("PK__Song__12E3D6F7F64374A4");

            entity.ToTable("Song");

            entity.Property(e => e.SongId).HasColumnName("SongID");
            entity.Property(e => e.AlbumId).HasColumnName("AlbumID");
            entity.Property(e => e.ArtistId).HasColumnName("ArtistID");
            entity.Property(e => e.LinkLrc).HasColumnName("LinkLRC");
            entity.Property(e => e.SongName).HasMaxLength(50);
            entity.Property(e => e.TypeId).HasColumnName("TypeID");

            entity.HasOne(d => d.Album).WithMany(p => p.Songs)
                .HasForeignKey(d => d.AlbumId)
                .HasConstraintName("FK__Song__AlbumID__5EBF139D");

            entity.HasOne(d => d.Artist).WithMany(p => p.Songs)
                .HasForeignKey(d => d.ArtistId)
                .HasConstraintName("FK__Song__ArtistID__5FB337D6");

            entity.HasOne(d => d.Type).WithMany(p => p.Songs)
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("FK__Song__TypeID__60A75C0F");
        });

        modelBuilder.Entity<SongHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__SongHist__4D7B4ADD72EEEE2B");

            entity.ToTable("SongHistory");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.PlayTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SongId).HasColumnName("SongID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Song).WithMany(p => p.SongHistories)
                .HasForeignKey(d => d.SongId)
                .HasConstraintName("FK__SongHisto__SongI__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.SongHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SongHisto__UserI__628FA481");
        });

        modelBuilder.Entity<Type>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__Type__516F03956832CBFA");

            entity.ToTable("Type");

            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.NameType).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC1973ED2F");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DeviceID");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(20);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Role).HasMaxLength(10);
            entity.Property(e => e.Status).HasMaxLength(10);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasMany(d => d.Artists).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserArtist",
                    r => r.HasOne<Artist>().WithMany()
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Arti__Artis__6383C8BA"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Arti__UserI__6477ECF3"),
                    j =>
                    {
                        j.HasKey("UserId", "ArtistId").HasName("PK__User_Art__05DFCA1B823222ED");
                        j.ToTable("User_Artist");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("ArtistId").HasColumnName("ArtistID");
                    });

            entity.HasMany(d => d.Songs).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserSongLove",
                    r => r.HasOne<Song>().WithMany()
                        .HasForeignKey("SongId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Song__SongI__656C112C"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__User_Song__UserI__66603565"),
                    j =>
                    {
                        j.HasKey("UserId", "SongId").HasName("PK__User_Son__76A6F1C3742C4360");
                        j.ToTable("User_SongLove");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("SongId").HasColumnName("SongID");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
