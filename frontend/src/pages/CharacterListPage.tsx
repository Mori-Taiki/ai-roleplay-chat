import React from 'react';
import { Link } from 'react-router-dom';
import { useIsAuthenticated } from "@azure/msal-react"; // ★ インポート
import { useCharacterList } from '../hooks/useCharacterList';
import Button from '../components/Button';
import styles from './CharacterListPage.module.css';
// import { InteractionStatus } from "@azure/msal-browser"; // ログイン処理中の考慮が必要なら
// import { useMsal } from "@azure/msal-react"; // ログイン処理中の考慮が必要なら

const CharacterListPage: React.FC = () => {
  const isAuthenticated = useIsAuthenticated(); // ★ 認証状態を取得
  // const { inProgress } = useMsal(); // ログイン/トークン取得処理中かどうかの状態

  // ★ フックは常に呼び出す (Reactのルール)
  const { characters, isLoading, error } = useCharacterList();

  // // オプション：ログイン処理中の表示（より丁寧にする場合）
  // if (inProgress === InteractionStatus.Startup || inProgress === InteractionStatus.HandleRedirect) {
  //   return <p>認証状態を確認中...</p>;
  // }

  return (
    <div className={styles.pageContainer}>
      <h2>キャラクター一覧</h2>
      <div className={styles.createButtonContainer}>
        <Button
          as={Link}
          to="/characters/new"
          variant="primary"
        >
          新規キャラクター作成
        </Button>
      </div>

      {/* ★ 認証状態に応じて表示を切り替え */}
      {!isAuthenticated ? (
        <p>
          キャラクターを登録・表示するには、ログインしてください。
        </p>
      ) : (
        // ★ 認証済みの場合の表示ロジック
        <>
          {/* isLoading は useCharacterList フックが認証状態を考慮して更新してくれる */}
          {isLoading && <p>キャラクターリストを読み込み中...</p>}

          {/* エラー表示 */}
          {error && <p className={styles.errorMessage}>エラー: {error}</p>}

          {/* キャラクターリスト (ローディング中でなく、エラーがなく、認証済みの場合) */}
          {!isLoading && !error && (
            <ul className={styles.characterList}>
              {characters.length === 0 ? (
                <p>登録されているキャラクターがいません。新規作成ボタンから作成してください。</p>
              ) : (
                characters.map((char) => (
                  <li key={char.id} className={styles.characterItem}>
                    <div className={styles.characterInfo}>
                      {char.avatarImageUrl ? (
                          <img src={char.avatarImageUrl} alt={char.name} className={styles.avatar} />
                      ) : (
                          <div className={styles.avatarPlaceholder}></div>
                      )}
                      <strong>{char.name}</strong>
                    </div>
                    <div className={styles.characterActions}>
                      <Button
                        as={Link}
                        to={`/chat/${char.id}`}
                        variant="secondary"
                        size="sm"
                      >
                        会話する
                      </Button>
                      <Button
                        as={Link}
                        to={`/characters/edit/${char.id}`}
                        variant="secondary"
                        size="sm"
                        style={{ marginLeft: '0.5rem' }}
                      >
                        編集
                      </Button>
                      {/* TODO: 削除ボタン */}
                    </div>
                  </li>
                ))
              )}
            </ul>
          )}
        </>
      )}
    </div>
  );
};

export default CharacterListPage;