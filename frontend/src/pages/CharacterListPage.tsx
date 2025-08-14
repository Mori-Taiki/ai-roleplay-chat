// src/pages/CharacterListPage.tsx
import React from 'react';
import { Link } from 'react-router-dom';
import { useIsAuthenticated } from "@azure/msal-react";
import { useCharacterList } from '../hooks/useCharacterList';
import Button from '../components/Button';
import styles from './CharacterListPage.module.css';
import { CharacterProfileWithSessionInfoResponse } from '../models/CharacterProfileResponse';

const CharacterListPage: React.FC = () => {
  const isAuthenticated = useIsAuthenticated();
  const { characters, isLoading, error } = useCharacterList();

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
                      <div className={styles.nameActionsMessage}>
                        <strong className={styles.characterName}>{char.name}</strong>
                        
                        {/* アクションボタン */}
                        <div className={styles.characterActions}>
                           <Button as={Link} to={`/chat/${char.id}`} variant="secondary" size="sm">会話する</Button>
                           <Button as={Link} to={`/characters/${char.id}/sessions`} variant="secondary" size="sm">セッション管理</Button>
                           <Button as={Link} to={`/characters/${char.id}/images`} variant="secondary" size="sm">画像ギャラリー</Button>
                           <Button as={Link} to={`/characters/edit/${char.id}`} variant="secondary" size="sm">編集</Button>
                        </div>

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