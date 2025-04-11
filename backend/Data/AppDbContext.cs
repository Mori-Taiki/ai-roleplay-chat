using AiRoleplayChat.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Data;

public class AppDbContext : DbContext
{
    // DI から DbContextOptions<AppDbContext> を受け取るコンストラクタ
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) // 受け取った options を基底クラスのコンストラクタに渡す
    {
    }

    public DbSet<CharacterProfile> CharacterProfiles { get; set; }
}