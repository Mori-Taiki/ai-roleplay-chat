// src/utils/errorHandler.ts
// (ProblemDetails の型定義は別途 src/models/ProblemDetails.ts などに定義することを推奨)
interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: { [key: string]: string[] }; // For ValidationProblemDetails
  // Other custom properties like 'message' if used
  message?: string;
}

/**
 * Response オブジェクトからユーザーフレンドリーなエラーメッセージを生成します。
 * ASP.NET Core の ProblemDetails / ValidationProblemDetails 形式を想定しています。
 * @param response - fetch API の Response オブジェクト
 * @returns ユーザーに表示するためのエラーメッセージ文字列
 */
export const getApiErrorMessage = async (
  response: Response
): Promise<string> => {
  // ステータスコードに基づいたデフォルトメッセージ
  let errorMessage = `エラーが発生しました (ステータス: ${response.status})。`;

  // ステータスコードに応じたメッセージを設定
  if (response.status === 404) {
    errorMessage = "要求されたデータが見つかりませんでした。";
  } else if (response.status === 400) {
    errorMessage = "入力内容、またはリクエスト形式が正しくありません。";
  } else if (response.status >= 500) {
    errorMessage =
      "サーバー内部でエラーが発生しました。時間を置いてから再度お試しください。";
  } else if (response.status === 401 || response.status === 403) {
    errorMessage = "アクセス権限がありません。";
  }
  // ... 他のステータスコードに対するメッセージも必要なら追加 ...

  try {
    // JSON形式のエラーレスポンスか確認
    const contentType = response.headers.get("content-type");
    if (
      contentType?.includes("application/json") ||
      contentType?.includes("application/problem+json")
    ) {
      const errorData: ProblemDetails | any = await response.json();

      if (errorData) {
        // ProblemDetails の title を優先
        if (typeof errorData.title === "string" && errorData.title) {
          errorMessage = errorData.title;
          // ValidationProblemDetails の errors 詳細を追加 (オプション)
          if (
            response.status === 400 &&
            errorData.errors &&
            typeof errorData.errors === "object"
          ) {
            const errorDetails = Object.entries(errorData.errors)
              .map(
                ([field, messages]) =>
                  `${field}: ${
                    Array.isArray(messages) ? messages.join(", ") : messages
                  }`
              )
              .join(" ");
            if (errorDetails) {
              errorMessage += ` (${errorDetails})`;
            }
          }
        }
        // title がなく message があればそれを使用
        else if (typeof errorData.message === "string" && errorData.message) {
          errorMessage = errorData.message;
        }
      }
    } else {
      // JSON 以外の場合、コンソールに警告を出すなどしても良い
      console.warn(
        `Received non-JSON error response (Content-Type: ${contentType}) for status ${response.status}.`
      );
    }
  } catch (e) {
    console.error(
      `Failed to parse error response JSON for status ${response.status}:`,
      e
    );
    // JSON パース失敗時はステータスコードに基づいたメッセージが使われる
  }

  return errorMessage;
};

/**
 * catch ブロックで捕捉したエラーオブジェクトから、ネットワークエラー等を判定し、
 * ユーザーフレンドリーなメッセージを返します。
 * @param error - catch ブロックで捕捉したエラーオブジェクト (unknown 型)
 * @param operationName - 失敗した操作名（例: 'キャラクターデータの読み込み'）
 * @returns ユーザーに表示するためのエラーメッセージ文字列
 */
export const getGenericErrorMessage = (
  error: unknown,
  operationName: string = "処理"
): string => {
  let defaultMessage = `${operationName}中に予期せぬエラーが発生しました。`;

  if (error instanceof Error) {
    // ネットワークエラーの簡易判定
    if (error.message.toLowerCase().includes("failed to fetch")) {
      return "サーバーに接続できませんでした。ネットワーク接続を確認し、時間を置いて再度お試しください。";
    }
    // Error オブジェクトの message をそのまま使う (getApiErrorMessage で生成されたメッセージなど)
    // ただし、あまりに技術的すぎるメッセージは避けるべき場合もある
    // return error.message;
    // 代わりに、より汎用的なメッセージと組み合わせることも可能
    return `${operationName}に失敗しました: ${error.message}`;
  }
  if (typeof error === "string") {
    return `${operationName}に失敗しました: ${error}`;
  }

  // 特定できない場合はデフォルトメッセージ
  return defaultMessage;
};
