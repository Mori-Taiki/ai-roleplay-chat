// src/pages/CharacterListPage.tsx
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useIsAuthenticated } from "@azure/msal-react";
import { useCharacterList } from '../hooks/useCharacterList';
import { useSessionApi } from '../hooks/useSessionApi';
import Button from '../components/Button';
import styles from './CharacterListPage.module.css';
import { CharacterProfileWithSessionInfoResponse } from '../models/CharacterProfileResponse';

const CharacterListPage: React.FC = () => {
  const isAuthenticated = useIsAuthenticated();
  const { characters, isLoading, error, fetchCharacters } = useCharacterList();
  const { deleteSession } = useSessionApi();
  const [deletingSessionId, setDeletingSessionId] = useState<string | null>(null);

  const handleDeleteSession = async (sessionId: string | undefined, characterName: string) => {
    if (!sessionId) return;
    if (window.confirm(`「${characterName}」との会話履歴を完全に削除しますか？この操作は元に戻せません。`)) {
      setDeletingSessionId(sessionId);
      try {
        await deleteSession(sessionId);
        alert(`「${characterName}」との会話履歴を削除しました。`);
        fetchCharacters();
      } catch (err: any) {
        console.error("Session deletion failed:", err);
        const errorMessage = err instanceof Error ? err.message : String(err);
        alert(`エラーが発生しました: ${errorMessage}`);
      } finally {
        setDeletingSessionId(null);
      }
    }
  };

  return (
    <div className={styles.pageContainer}>
      <h2>キャラクター一覧</h2>
      {/* ... 新規作成ボタン ... */}
      <div className={styles.createButtonContainer}>
        <Button as={Link} to="/characters/new" variant="primary">
          新規キャラクター作成
        </Button>
      </div>

      {!isAuthenticated ? (
        <p>キャラクターリストを表示するには、画面上部の「ログイン / 新規登録」ボタンよりログインしてください。</p>
      ) : (
        <>
          {isLoading && <p>キャラクターリストを読み込み中...</p>}
          {error && <p className={styles.errorMessage}>エラー: {error}</p>}
          {!isLoading && !error && (
            <ul className={styles.characterList}>
              {characters.length === 0 ? (
                <p>登録されているキャラクターがいません。</p>
              ) : (
                characters.map((char: CharacterProfileWithSessionInfoResponse) => (
                  <li key={char.id} className={styles.characterItem}>
                    <div className={styles.characterInfo}>
                      {/* アバター */}
                      {char.avatarImageUrl ? (
                        <img src={char.avatarImageUrl} alt={char.name} className={styles.avatar} />
                      ) : (
                        <img src={"https://airoleplaychatblobstr.blob.core.windows.net/profile-images/placeholder.png"} alt={char.name} className={styles.avatar} />
                      )}
                      <div className={styles.nameAndMessage}>
                        <strong className={styles.characterName}>{char.name}</strong>
                        {char.lastMessageSnippet ? (
                          <div
                            className={styles.lastMessageWrapper}
                            title={char.lastMessageSnippet}
                          >
                            {char.lastMessageSnippet}
                          </div>
                        ) : (
                          <span className={styles.noMessagesYet}>まだ会話がありません</span>
                        )}
                      </div>
                    </div>

                    {/* アクションボタン */}
                    <div className={styles.characterActions}>
                       <Button as={Link} to={`/chat/${char.id}`} variant="secondary" size="sm">会話する</Button>
                       <Button as={Link} to={`/characters/${char.id}/sessions`} variant="secondary" size="sm" style={{ marginLeft: '0.5rem' }}>セッション管理</Button>
                       <Button as={Link} to={`/characters/${char.id}/images`} variant="secondary" size="sm" style={{ marginLeft: '0.5rem' }}>画像ギャラリー</Button>
                       <Button as={Link} to={`/characters/edit/${char.id}`} variant="secondary" size="sm" style={{ marginLeft: '0.5rem' }}>編集</Button>
                       {/* 削除ボタン (disabled 条件は維持) */}
                       <Button
                          variant="danger"
                          size="sm"
                          style={{ marginLeft: '0.5rem' }}
                          onClick={() => handleDeleteSession(char.sessionId, char.name)}
                          disabled={!char.sessionId || deletingSessionId === char.sessionId}
                        >
                          {deletingSessionId === char.sessionId ? '履歴なし' : '履歴削除'}
                        </Button>
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