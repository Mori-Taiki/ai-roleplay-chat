// src/pages/CharacterListPage.tsx (修正後)
import React from 'react';
import { Link } from 'react-router-dom';
import { useCharacterList } from '../hooks/useCharacterList'; // 作成したフックをインポート
import Button from '../components/Button';
import styles from './CharacterListPage.module.css';

const CharacterListPage: React.FC = () => {
  // カスタムフックから状態と関数を取得
  const { characters, isLoading, error } = useCharacterList();

  return (
    <div className={styles.pageContainer}>
      {' '}
      {/* スタイル用コンテナ */}
      <h2>キャラクター一覧</h2>
      {/* 新規作成ボタン */}
      <div className={styles.createButtonContainer}>
        <Button as={Link} to="/characters/new" variant="primary">
          新規キャラクター作成
        </Button>
      </div>
      {/* ローディング表示 */}
      {isLoading && <p>キャラクターリストを読み込み中...</p>}
      {/* エラー表示 */}
      {error && <p className={styles.errorMessage}>エラー: {error}</p>}
      {/* キャラクターリスト */}
      {!isLoading && !error && (
        <ul className={styles.characterList}>
          {characters.length === 0 ? (
            <p>登録されているキャラクターがいません。</p>
          ) : (
            characters.map((char) => (
              <li key={char.id} className={styles.characterItem}>
                <div className={styles.characterInfo}>
                  {/* アバター画像 (任意) */}
                  {char.avatarImageUrl && <img src={char.avatarImageUrl} alt={char.name} className={styles.avatar} />}
                  <strong>{char.name}</strong>
                  <span className={styles.characterId}>(ID: {char.id})</span>
                  {/* 有効/無効表示 (任意) */}
                  {/* <span className={char.isActive ? styles.active : styles.inactive}>
                     {char.isActive ? '有効' : '無効'}
                  </span> */}
                </div>
                <div className={styles.characterActions}>
                  {/* 会話するボタン */}
                  <Button
                    as={Link}
                    to={`/chat/${char.id}`}
                    variant="secondary" // スタイルは適宜調整
                    size="sm" // 小さいボタンにするなど (Buttonコンポーネントに size props が必要)
                  >
                    会話する
                  </Button>
                  {/* 編集ボタン */}
                  <Button
                    as={Link}
                    to={`/characters/edit/${char.id}`}
                    variant="secondary"
                    size="sm" // 小さいボタンにするなど
                    style={{ marginLeft: '0.5rem' }}
                  >
                    編集
                  </Button>
                  {/* TODO: 削除ボタン (削除機能実装時に追加) */}
                </div>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
};

export default CharacterListPage;
