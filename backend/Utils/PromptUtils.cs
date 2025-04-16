namespace AiRoleplayChat.Backend.Utils;

public class PromptUtils
{
    // --- ★ システムプロンプト関連のヘルパー ---
    public static class SystemPromptHelper
    {
        // デフォルトプロンプトを生成するメソッド
        public static string GenerateDefaultPrompt(string name, string? personality, string? tone, string? backstory)
        {
            // Controller にあったロジックをここに移動
            return $"あなたはキャラクター「{name}」です。\n" +
                   $"性格: {personality ?? "未設定"}\n" +
                   $"口調: {tone ?? "未設定"}\n" +
                   $"背景: {backstory ?? "未設定"}\n" +
                   "ユーザーと自然で魅力的な対話を行ってください。";
        }

        // 画像生成の指示を含む定数文字列
        public const string ImageGenerationInstruction = "\n---\n" + // 区切り線
            "【重要】**あなた自身の行動や感情、あなたが見ている情景、あるいはユーザーに見せたいあなた自身の姿（例：自撮り風）**を描写する画像を生成することが、会話をよりリアルで魅力的にすると判断した場合**のみ**、あなたの応答の最後に以下の形式で指示を追加してください。それ以外の場合は、この指示タグを追加することを禁止します。\n" +
            "形式: `[generate_image: (生成したい画像の情景、物、またはあなた自身の姿を、英語で詳細に記述したプロンプト)]`\n" + 
            "**プロンプト例:** `[generate_image: selfie of me smiling and waving, park background, sunny day, anime style]`"; 

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