// src/Data/AppDbContext.cs
using AiRoleplayChat.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Data;

public class AppDbContext : DbContext
{
    public DbSet<CharacterProfile> CharacterProfiles { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserSetting> UserSettings { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- ChatSessions の設定 ---
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id); // 主キー設定 (UUID なので自動インクリメントなし)
            entity.Property(e => e.Id).ValueGeneratedNever(); // DB 側で自動生成しない

            // インデックス例 (必要に応じて調整)
            entity.HasIndex(e => new { e.UserId, e.CharacterProfileId, e.StartTime })
                  .HasDatabaseName("IX_ChatSessions_User_Character_StartTime");

            // リレーションシップ (CharacterProfile)
            entity.HasOne(d => d.CharacterProfile)
                  .WithMany() // CharacterProfile 側にはナビゲーションプロパティ不要の場合
                              // .WithMany(p => p.ChatSessions) // もし CharacterProfile に ICollection<ChatSession> を追加するなら
                  .HasForeignKey(d => d.CharacterProfileId)
                  .OnDelete(DeleteBehavior.Cascade); // キャラ削除時にセッションも削除
        });


        // --- ChatMessages の設定 ---
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            // 複合インデックス (セッション内のメッセージ取得用)
            entity.HasIndex(e => new { e.SessionId, e.Timestamp })
                  .HasDatabaseName("IX_ChatMessages_SessionId_Timestamp");

            // 複合インデックス (最新ユーザーメッセージ検索用)
            entity.HasIndex(e => new { e.SessionId, e.UserId, e.Sender, e.Timestamp })
                  .HasDatabaseName("IX_ChatMessages_SessionId_UserId_Sender_Timestamp");

            // リレーションシップ (ChatSession)
            entity.HasOne(d => d.Session)
                  .WithMany(p => p.Messages)
                  .HasForeignKey(d => d.SessionId)
                  .OnDelete(DeleteBehavior.Cascade); // セッション削除時にメッセージも削除

            // リレーションシップ (CharacterProfile)
            entity.HasOne(d => d.CharacterProfile)
                  .WithMany()
                  .HasForeignKey(d => d.CharacterProfileId)
                  .OnDelete(DeleteBehavior.Cascade); // キャラ削除時にメッセージも削除

            entity.Property(e => e.Sender).HasMaxLength(10);
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ServiceType, e.SettingKey }).IsUnique();
        });
    }
}