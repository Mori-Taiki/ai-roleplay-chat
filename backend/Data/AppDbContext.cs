using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Data;

public class AppDbContext : DbContext
{
    // DI から DbContextOptions<AppDbContext> を受け取るコンストラクタ
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) // 受け取った options を基底クラスのコンストラクタに渡す
    {
    }

    // --- ここに将来、データベースのテーブルに対応する DbSet<T> プロパティを追加していく ---
    // 例:
    // public DbSet<ChatMessage> ChatMessages { get; set; } // チャット履歴用
    // public DbSet<CharacterProfile> CharacterProfiles { get; set; } // キャラクター設定用

    // 必要であれば、OnModelCreating メソッドをオーバーライドして、
    // テーブル名やリレーションシップなどの詳細なマッピング設定を行うこともできます。
    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     base.OnModelCreating(modelBuilder);
    //     // マッピング設定...
    // }
}