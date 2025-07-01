// using Google.Cloud.AIPlatform.V1;
// using Google.Protobuf.WellKnownTypes;
// using Microsoft.Extensions.Configuration;
// using Grpc.Core;
// using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

// namespace AiRoleplayChat.Backend.Services;

// public class ImagenService : IImagenService
// {
//     private readonly PredictionServiceClient _predictionServiceClient;
//     private readonly IConfiguration _config;
//     private readonly string _projectId;
//     private readonly string _location;
//     private readonly string _modelId;
//     private readonly EndpointName _endpointName;

//     public ImagenService(PredictionServiceClient predictionServiceClient, IConfiguration configuration)
//     {
//         _predictionServiceClient = predictionServiceClient ?? throw new ArgumentNullException(nameof(predictionServiceClient));
//         _config = configuration ?? throw new ArgumentNullException(nameof(configuration));

//         _projectId = _config["VertexAi:ProjectId"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:ProjectId");
//         _location = _config["VertexAi:Location"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:Location");
//         _modelId = _config["VertexAi:ImageGenerationModel"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:ImageGenerationModel");

//         _endpointName = EndpointName.FromProjectLocationPublisherModel(_projectId, _location, "google", _modelId);
//         Console.WriteLine($"[ImagenService] Initialized. ProjectId: {_projectId}, Location: {_location}, ModelId: {_modelId}");
//     }

//     /// <summary>
//     /// 指定された英語プロンプトに基づいて画像を生成し、その画像のData URIを返します。
//     /// </summary>
//     public async Task<string?> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
//     {
//         if (string.IsNullOrWhiteSpace(prompt))
//         {
//             throw new ArgumentException("Prompt cannot be empty or whitespace.", nameof(prompt));
//         }

//         var instances = new List<ProtoValue>
//         {
//             ProtoValue.ForStruct(new Struct
//             {
//                 Fields =
//                 {
//                     { "prompt", ProtoValue.ForString(prompt) },
//                     { "sampleCount", ProtoValue.ForNumber(1) }
//                 }
//             })
//         };

//         Console.WriteLine($"Calling Vertex AI Imagen API (Project: {_projectId}, Model: {_modelId}) with prompt: \"{prompt}\"...");
//         PredictResponse imagenResponse;
//         try
//         {
//             imagenResponse = await _predictionServiceClient.PredictAsync(_endpointName, instances, null, cancellationToken);
//             Console.WriteLine("Imagen API call successful!");
//         }
//         catch (RpcException ex)
//         {
//              Console.Error.WriteLine($"Imagen API gRPC Error calling PredictAsync: {ex.Status.Detail} (Code: {ex.StatusCode})");
//              // エラー時は null を返す
//              return null;
//         }
//         catch (Exception ex)
//         {
//             Console.Error.WriteLine($"Unexpected error during Imagen API call: {ex.Message}");
//              // エラー時は null を返す
//             return null;
//         }


//         if (imagenResponse.Predictions.Count > 0)
//         {
//             var firstPrediction = imagenResponse.Predictions[0];

//             if (firstPrediction.KindCase == ProtoValue.KindOneofCase.StructValue &&
//                 firstPrediction.StructValue.Fields.TryGetValue("bytesBase64Encoded", out var base64Value) &&
//                 base64Value.KindCase == ProtoValue.KindOneofCase.StringValue &&
//                 firstPrediction.StructValue.Fields.TryGetValue("mimeType", out var mimeTypeValue) &&
//                 mimeTypeValue.KindCase == ProtoValue.KindOneofCase.StringValue)
//             {
//                 string base64Data = base64Value.StringValue;
//                 string mimeType = mimeTypeValue.StringValue;

//                 Console.WriteLine($"Image data extracted: MimeType={mimeType}, Base64Data Length={base64Data.Length}");

//                 // ★ Data URI形式の文字列を作成して返す
//                 return $"data:{mimeType};base64,{base64Data}";
//             }
//         }

//         // 画像が取得できなかった場合は null を返す
//         Console.WriteLine("Imagen API response contained no valid predictions.");
//         return null;
//     }
// }