// frontend/src/pages/CharacterListPage.tsx (新規作成)
import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom'; // リンク作成用
// バックエンドのDTOに対応する型定義をインポートします。
// 事前に frontend/src/models/CharacterProfileResponse.ts のようなファイルを作成しておきましょう。
import { CharacterProfileResponse } from '../models/CharacterProfileResponse';

const CharacterListPage: React.FC = () => {
  // キャラクターリスト、ローディング状態、エラー状態を管理する state
  const [characters, setCharacters] = useState<CharacterProfileResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCharacters = async () => {
      setIsLoading(true);
      setError(null); 
      try {
        // バックエンド API (GET /api/characterprofiles) を呼び出す
        // Viteを使っている場合、vite.config.ts でプロキシ設定をしていれば '/api/...' だけで呼び出せます。
        // 設定していない場合は、バックエンドのURLを含めてください (例: 'http://localhost:7000/api/characterprofiles')
        const response = await fetch('https://localhost:7000/api/characterprofiles');

        if (!response.ok) {
          throw new Error(`キャラクターリストの取得に失敗しました: ${response.statusText}`);
        }

        // レスポンスボディを JSON としてパース
        const data: CharacterProfileResponse[] = await response.json();
        setCharacters(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : '不明なエラーが発生しました');
        console.error("Error fetching characters:", err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCharacters(); // 定義した関数を実行
  }, []); // 第2引数の空配列 [] は、この useEffect がコンポーネントのマウント時に1回だけ実行されることを意味します

  // --- レンダリング部分 ---
  return (
    <div>
      <h2>キャラクター一覧</h2>

      {/* 新規作成画面へのリンク */}
      <div style={{ marginBottom: '1rem' }}>
        <Link to="/characters/new">
          <button type="button">新規キャラクター作成</button>
        </Link>
      </div>

      {/* ローディング中の表示 */}
      {isLoading && <p>キャラクターリストを読み込み中...</p>}

      {/* エラー発生時の表示 */}
      {error && <p style={{ color: 'red' }}>エラー: {error}</p>}

      {/* キャラクターリストの表示 (ローディング完了、エラーなしの場合) */}
      {!isLoading && !error && (
        <ul>
          {/* characters 配列が空の場合のメッセージ */}
          {characters.length === 0 ? (
            <p>登録されているキャラクターがいません。</p>
          ) : (
            // characters 配列をループして各キャラクターを表示
            characters.map(char => (
              <li key={char.id} style={{ marginBottom: '0.5rem', borderBottom: '1px solid #eee', paddingBottom: '0.5rem' }}>
                <strong>{char.name}</strong> (ID: {char.id})
                {/* 各キャラクターの編集画面へのリンク */}
                <Link to={`/characters/edit/${char.id}`} style={{ marginLeft: '1rem' }}>
                  <button type="button">編集</button>
                </Link>
                {/* TODO: 将来的にここに削除ボタンも追加検討 */}
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
};

export default CharacterListPage;