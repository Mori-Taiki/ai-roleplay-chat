using System.Text.Json.Serialization;

/// <summary>
/// ★★★ 追加 ★★★
/// 安全性設定のカテゴリと閾値を定義するクラス
/// </summary>
public class GeminiSafetySetting
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("threshold")]
    public required string Threshold { get; set; }
}