using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> GetOrCreateAppUserIdAsync(string b2cObjectId, string displayName, string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(b2cObjectId))
        {
            // B2C Object ID がなければ処理できない
            _logger.LogError("B2C Object ID is null or empty. Cannot get or create user.");
            // 例外をスローするか、エラーを示す値 (例: 0 や -1) を返すか、設計に応じて決定
            throw new ArgumentException("B2C Object ID cannot be null or empty.", nameof(b2cObjectId));
        }

        // 1. B2C Object ID で Users テーブルを検索
        var existingUser = await _context.Users
                                         .FirstOrDefaultAsync(u => u.B2cObjectId == b2cObjectId, cancellationToken);

        if (existingUser != null)
        {
            // 2a. ユーザーが見つかった場合: そのユーザーの内部 ID (int) を返す
            _logger.LogDebug("Found existing user with B2C Object ID {B2cObjectId}. App User ID: {AppUserId}", b2cObjectId, existingUser.Id);
            // 必要であれば DisplayName や Email を更新するロジックを追加しても良い
            // existingUser.DisplayName = displayName;
            // existingUser.Email = email ?? existingUser.Email;
            // existingUser.UpdatedAt = DateTime.UtcNow;
            // await _context.SaveChangesAsync(cancellationToken);
            return existingUser.Id;
        }
        else
        {
            // 2b. ユーザーが見つからなかった場合: 新規作成
            _logger.LogInformation("User with B2C Object ID {B2cObjectId} not found. Creating new user.", b2cObjectId);

            var newUser = new User
            {
                B2cObjectId = b2cObjectId,
                DisplayName = string.IsNullOrEmpty(displayName) ? "新規ユーザー" : displayName, // displayName が空の場合のフォールバック
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully created new user. App User ID: {AppUserId}, B2C Object ID: {B2cObjectId}", newUser.Id, b2cObjectId);
                // 作成された新しいユーザーの内部 ID (int) を返す
                return newUser.Id;
            }
            catch (DbUpdateException ex) // 保存時の例外処理 (例: 一意制約違反など)
            {
                _logger.LogError(ex, "Failed to create new user with B2C Object ID {B2cObjectId}.", b2cObjectId);
                // より詳細なエラーハンドリングや再試行ロジックが必要な場合もある
                throw new Exception("Failed to create user record in the database.", ex);
            }
        }
    }
}