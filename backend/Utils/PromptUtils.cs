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
        public const string ImageGenerationInstruction = "\n---\n" + 
           "【重要】**あなた自身の行動や感情、あなたが見ている情景**を描写する画像を生成することが、会話をよりリアルで魅力的にすると判断した場合**のみ**、応答の最後に以下の形式で指示を追加してください。\n" +
            "【画像プロンプト作成ルール】\n" +
            "1. **タグ形式で記述:** プロンプトは自然な文章ではなく、**英語のタグをカンマ区切りで並べた形式**で記述してください。\n" +
            "2. **品質タグを先頭に:** プロンプトの先頭には、必ず `masterpiece, best quality, very aesthetic, absurdres` という品質タグを入れてください。\n" +
            "3. **具体的なタグを追加:** キャラクターの人数(例: `1girl`, `2boys`)、表情(例: `smile`, `sad`)、髪型(例: `long hair`, `twintails`)、服装(例: `school uniform`, `dress`)、ポーズ(例: `sitting`, `waving`)、背景(例: `tokyo street`, `fantasy forest`)などを具体的にタグで表現してください。\n" +
            "4. **形式:** `[generate_image: (ルールに従って作成したタグ形式の英語プロンプト)]`\n\n" +
            "**プロンプト例1 (一人、笑顔):** `[generate_image: masterpiece, best quality, very aesthetic, absurdres, 1girl, solo, smile, long blonde hair, school uniform, sitting on a park bench, sunny day, cherry blossoms]`\n" +
            "**プロンプト例2 (自撮り風):** `[generate_image: masterpiece, best quality, very aesthetic, absurdres, 1girl, selfie, peace sign, looking at viewer, brown hair, cat ears, cafe, warm lighting]`";

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