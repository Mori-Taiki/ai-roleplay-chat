using Google.Cloud.AIPlatform.V1; // PredictionServiceClient など
using Google.Protobuf.WellKnownTypes; // Struct, Value
using ProtoValue = Google.Protobuf.WellKnownTypes.Value; // エイリアスを使用
using AiRoleplayChat.Backend.Models; // ImageGenerationResponse モデル
using Microsoft.Extensions.Configuration; // IConfiguration
using Grpc.Core; // RpcException をキャッチするため

namespace AiRoleplayChat.Backend.Services; // 名前空間を確認・調整

public class ImagenService : IImagenService // IImagenService インターフェースを実装
{
    // DIコンテナから注入される PredictionServiceClient と IConfiguration
    private readonly PredictionServiceClient _predictionServiceClient;
    private readonly IConfiguration _config;

    // 設定ファイルから読み込んだ値を保持するフィールド
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _modelId;
    private readonly EndpointName _endpointName; // 事前に EndpointName も組み立てておく

    // コンストラクタ: DIコンテナからサービスと設定を受け取る
    public ImagenService(PredictionServiceClient predictionServiceClient, IConfiguration configuration)
    {
        _predictionServiceClient = predictionServiceClient ?? throw new ArgumentNullException(nameof(predictionServiceClient));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // コンストラクタで設定値を読み込み、必須チェックを行う (?? throw)
        _projectId = _config["VertexAi:ProjectId"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:ProjectId");
        _location = _config["VertexAi:Location"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:Location");
        _modelId = _config["VertexAi:ImageGenerationModel"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:ImageGenerationModel");

        // エンドポイント名を組み立ててフィールドに保持 (毎回組み立てる必要がなくなる)
        _endpointName = EndpointName.FromProjectLocationPublisherModel(_projectId, _location, "google", _modelId);

        Console.WriteLine($"[ImagenService] Initialized. ProjectId: {_projectId}, Location: {_location}, ModelId: {_modelId}");
    }

    /// <summary>
    /// 指定された英語プロンプトに基づいて画像を生成します。
    /// </summary>
    public async Task<ImageGenerationResponse> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // プロンプトが空でないかチェック
        if (string.IsNullOrWhiteSpace(prompt))
        {
            // 引数が不正な場合は ArgumentException をスローするのが一般的
            throw new ArgumentException("Prompt cannot be empty or whitespace.", nameof(prompt));
        }

        // Imagen API へのリクエストパラメータを作成
        var instances = new List<ProtoValue>
        {
            ProtoValue.ForStruct(new Struct
            {
                Fields =
                {
                    { "prompt", ProtoValue.ForString(prompt) },
                    { "sampleCount", ProtoValue.ForNumber(1) } // 生成枚数 (必要なら設定可能にする)
                    // その他のパラメータ (negativePrompt, aspectRatio など) も必要なら追加
                }
            })
        };

        Console.WriteLine($"Calling Vertex AI Imagen API (Project: {_projectId}, Model: {_modelId}) with prompt: \"{prompt}\"...");
        PredictResponse imagenResponse;
        try
        {
            // PredictionServiceClient を使って API を呼び出す
            // CancellationToken を渡してキャンセル可能にする
            imagenResponse = await _predictionServiceClient.PredictAsync(_endpointName, instances, null, cancellationToken);
            Console.WriteLine("Imagen API call successful!");
        }
        catch (RpcException ex) // Google Cloud クライアントライブラリがよくスローする gRPC の例外
        {
             Console.Error.WriteLine($"Imagen API gRPC Error calling PredictAsync: {ex.Status.Detail} (Code: {ex.StatusCode})");
             // より具体的な情報を含む例外をスロー
             throw new Exception($"Failed to generate image via Imagen API: {ex.Status.Detail}", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
             Console.WriteLine("Imagen API call canceled by request.");
             throw; // キャンセルはそのまま再スロー
        }
        catch (Exception ex) // その他の予期せぬエラー
        {
            Console.Error.WriteLine($"Unexpected error during Imagen API call: {ex.Message}");
            throw new Exception("An unexpected error occurred while generating the image.", ex);
        }

        // --- レスポンス処理 (Program.cs から移動) ---
        if (imagenResponse.Predictions.Count > 0)
        {
            var firstPrediction = imagenResponse.Predictions[0];

            if (firstPrediction.KindCase == ProtoValue.KindOneofCase.StructValue &&
                firstPrediction.StructValue.Fields.TryGetValue("bytesBase64Encoded", out var base64Value) &&
                base64Value.KindCase == ProtoValue.KindOneofCase.StringValue &&
                firstPrediction.StructValue.Fields.TryGetValue("mimeType", out var mimeTypeValue) &&
                mimeTypeValue.KindCase == ProtoValue.KindOneofCase.StringValue)
            {
                // 正常に画像データを取得できた場合
                string base64Data = base64Value.StringValue;
                string mimeType = mimeTypeValue.StringValue;

                Console.WriteLine($"Image data extracted: MimeType={mimeType}, Base64Data Length={base64Data.Length}");
                // 結果を ImageGenerationResponse レコードで返す
                return new ImageGenerationResponse(mimeType, base64Data);
            }
            else
            {
                // レスポンスの構造が期待と異なる場合
                string errorDetail = $"Actual Prediction[0] structure: {firstPrediction}";
                Console.Error.WriteLine($"Error: Could not find expected fields 'bytesBase64Encoded' or 'mimeType' in Imagen API response. {errorDetail}");
                throw new Exception("Imagen API response did not contain expected image data structure.");
            }
        }
        else
        {
            // 予測結果が空の場合 (安全フィルターによるブロックなど)
            Console.WriteLine("Imagen API response contained no predictions. Check safety filters or prompt issues.");
            Console.WriteLine($"[DEBUG] Full Imagen Response (No Predictions): {imagenResponse}"); // デバッグ用に全レスポンス出力
            // ★今後の課題: imagenResponse.Metadata などからブロック理由を取得する処理を追加する場所
            throw new Exception("Image generation failed. The response contained no predictions (possibly due to safety filters).");
        }
    }
}