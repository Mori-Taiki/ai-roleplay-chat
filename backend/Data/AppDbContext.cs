// src/Data/AppDbContext.cs (更新)
using AiRoleplayChat.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Data;

public class AppDbContext : DbContext
{
    public DbSet<CharacterProfile> CharacterProfiles { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;

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

            // リレーションシップ (ChatSession)
            entity.HasOne(d => d.Session) // ChatMessage は 1つの ChatSession を持つ
                  .WithMany(p => p.Messages) // ChatSession は多くの ChatMessage を持つ
                  .HasForeignKey(d => d.SessionId)
                  .OnDelete(DeleteBehavior.Cascade); // セッション削除時にメッセージも削除

            // リレーションシップ (CharacterProfile) - ChatSession経由で辿れるが、直接FKを持つ実装
            entity.HasOne(d => d.CharacterProfile)
                  .WithMany()
                  .HasForeignKey(d => d.CharacterProfileId)
                  .OnDelete(DeleteBehavior.NoAction); // ChatSession側でCascade設定済みなのでNoActionに

            entity.Property(e => e.Sender).HasMaxLength(10);
        });
    }
}