// src/pages/CharacterListPage.tsx
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useIsAuthenticated } from "@azure/msal-react";
import { useCharacterList } from '../hooks/useCharacterList';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import Button from '../components/Button';
import AvatarSelectionModal from '../components/AvatarSelectionModal';
import styles from './CharacterListPage.module.css';
import { CharacterProfileWithSessionInfoResponse } from '../models/CharacterProfileResponse';

const CharacterListPage: React.FC = () => {
  const isAuthenticated = useIsAuthenticated();
  const { characters, isLoading, error, fetchCharacters } = useCharacterList();
  const { updateCharacter, fetchCharacter } = useCharacterProfile();
  const [selectedCharacterId, setSelectedCharacterId] = useState<number | null>(null);
  const [isUpdating, setIsUpdating] = useState<number | null>(null);

  const handleSetAvatar = (characterId: string) => {
    setSelectedCharacterId(parseInt(characterId));
  };

  const handleCloseModal = () => {
    setSelectedCharacterId(null);
  };

  const handleSelectAvatar = async (imageUrl: string) => {
    if (!selectedCharacterId) return;

    setIsUpdating(selectedCharacterId);
    
    // Fetch the full character details first
    const fullCharacter = await fetchCharacter(selectedCharacterId);
    if (!fullCharacter) {
      setIsUpdating(null);
      return;
    }

    // Update the character with the new avatar URL
    const success = await updateCharacter(selectedCharacterId, {
      name: fullCharacter.name,
      personality: fullCharacter.personality || '',
      tone: fullCharacter.tone || '',
      backstory: fullCharacter.backstory || '',
      systemPrompt: fullCharacter.systemPrompt,
      exampleDialogue: fullCharacter.exampleDialogue || '',
      avatarImageUrl: imageUrl,
      isActive: fullCharacter.isActive,
      isSystemPromptCustomized: fullCharacter.isSystemPromptCustomized,
    });

    if (success) {
      // Refresh the character list to show the new avatar
      await fetchCharacters();
    }
    
    setIsUpdating(null);
    setSelectedCharacterId(null);
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
                      <div className={styles.avatarContainer}>
                        {char.avatarImageUrl ? (
                          <img src={char.avatarImageUrl} alt={char.name} className={styles.avatar} />
                        ) : (
                          <img src={"https://airoleplaychatblobstr.blob.core.windows.net/profile-images/placeholder.png"} alt={char.name} className={styles.avatar} />
                        )}
                        <button
                          className={styles.setAvatarButton}
                          onClick={() => handleSetAvatar(char.id)}
                          disabled={isUpdating === parseInt(char.id)}
                          title="生成画像からアバターを設定"
                        >
                          {isUpdating === parseInt(char.id) ? '更新中...' : 'アバター設定'}
                        </button>
                      </div>
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

      {/* Avatar Selection Modal */}
      {selectedCharacterId && (
        <AvatarSelectionModal
          isOpen={selectedCharacterId !== null}
          onClose={handleCloseModal}
          characterId={selectedCharacterId}
          onSelectAvatar={handleSelectAvatar}
        />
      )}
    </div>
  );
};

export default CharacterListPage;