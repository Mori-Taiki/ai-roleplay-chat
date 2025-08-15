namespace AiRoleplayChat.Backend.Utils;

public class PromptUtils
{
    // --- ★ システムプロンプト関連のヘルパー ---
    public static class SystemPromptHelper
    {
        // デフォルトプロンプトを生成するメソッド
        // 項目追加時はsystemPrompt生成ロジックも更新する
        public static string GenerateDefaultPrompt(string name, string? personality, string? tone, string? backstory, string? appearance = null, string? userAppellation = null)
        {
            // Controller にあったロジックをここに移動
            return $"あなたはキャラクター「{name}」です。\n" +
                   $"性格: {personality ?? "未設定"}\n" +
                   $"口調: {tone ?? "未設定"}\n" +
                   $"背景: {backstory ?? "未設定"}\n" +
                   $"容姿: {appearance ?? "未設定"}\n" +
                   $"ユーザーの呼び方: {userAppellation ?? "未設定"}\n" +
                   "ユーザーと自然で魅力的な対話を行ってください。";
        }

        // 画像生成の指示を含む定数文字列
        public const string ImageGenerationInstruction = "\n---\n" + 
           "【重要】**あなた自身の行動や感情、あなたが見ている情景**を描写する画像を生成することが、会話をよりリアルで魅力的にすると判断した場合**のみ**、応答の最後に `[generate_image]` というタグを追加してください。";

        // ベースとなるプロンプトに画像生成指示を追加するメソッド
        public static string AppendImageInstruction(string basePrompt)
        {
            // 既に指示が含まれているか簡易的にチェック (任意)
            // if (basePrompt != null && basePrompt.Contains("[generate_image:"))
            // {
            //     return basePrompt; // 既にあれば追加しない
            // }
            // ベースプロンプトが空や Null でないことを確認
            if (string.IsNullOrWhiteSpace(basePrompt))
            {
                return ImageGenerationInstruction; // ベースが空なら指示だけ返す（またはエラー）
            }
            return basePrompt + ImageGenerationInstruction;
        }
    }
}