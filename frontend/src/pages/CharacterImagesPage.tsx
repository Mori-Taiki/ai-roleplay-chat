import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useImageApi, ImageItem } from '../hooks/useImageApi';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import styles from './CharacterImagesPage.module.css';

interface PromptModalProps {
  isOpen: boolean;
  onClose: () => void;
  image: ImageItem | null;
}

const PromptModal: React.FC<PromptModalProps> = ({ isOpen, onClose, image }) => {
  const handleCopy = () => {
    if (image?.imagePrompt) {
      navigator.clipboard.writeText(image.imagePrompt);
    }
  };

  if (!isOpen || !image) return null;

  return (
    <div className={styles.modal} onClick={onClose}>
      <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>画像生成プロンプト</h3>
          <button className={styles.closeButton} onClick={onClose}>×</button>
        </div>
        
        <div className={styles.promptText}>
          {image.imagePrompt || 'プロンプト情報が利用できません'}
        </div>

        {image.modelId && (
          <div className={styles.modelInfo}>
            <span className={styles.modelBadge}>Model: {image.modelId}</span>
            {image.serviceName && <span className={styles.modelBadge}>Service: {image.serviceName}</span>}
          </div>
        )}

        <div className={styles.modalActions}>
          {image.imagePrompt && (
            <button className={styles.copyButton} onClick={handleCopy}>
              コピー
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

const CharacterImagesPage: React.FC = () => {
  const { characterId } = useParams<{ characterId: string }>();
  const [selectedSessionId, setSelectedSessionId] = useState<string>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedImage, setSelectedImage] = useState<ImageItem | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const {
    isLoading,
    isDeleting,
    images,
    total,
    pageSize,
    sessions,
    getImages,
    deleteImage,
    getSessionsByCharacter,
    error,
  } = useImageApi();

  const { character, isLoading: isLoadingCharacter, fetchCharacter } = useCharacterProfile();

  const numericCharacterId = characterId ? parseInt(characterId) : 0;

  // Load character profile
  useEffect(() => {
    if (numericCharacterId > 0) {
      fetchCharacter(numericCharacterId);
    }
  }, [numericCharacterId, fetchCharacter]);

  // Load sessions and initial images
  useEffect(() => {
    if (numericCharacterId > 0) {
      getSessionsByCharacter(numericCharacterId);
      loadImages();
    }
  }, [numericCharacterId]);

  // Reload images when filters change
  useEffect(() => {
    if (numericCharacterId > 0) {
      loadImages();
    }
  }, [selectedSessionId, currentPage]);

  const loadImages = useCallback(() => {
    if (numericCharacterId > 0) {
      getImages(
        numericCharacterId,
        selectedSessionId || undefined,
        currentPage,
        40
      );
    }
  }, [numericCharacterId, selectedSessionId, currentPage, getImages]);

  const handleDeleteImage = async (messageId: number) => {
    if (window.confirm('この画像を削除しますか？')) {
      const success = await deleteImage(messageId);
      if (success) {
        // Reload current page if it becomes empty
        if (images.length === 1 && currentPage > 1) {
          setCurrentPage(currentPage - 1);
        }
      }
    }
  };

  const handleShowPrompt = (image: ImageItem) => {
    setSelectedImage(image);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setSelectedImage(null);
  };

  const totalPages = Math.ceil(total / pageSize);

  if (isLoadingCharacter) {
    return <div className={styles.loading}>キャラクター情報を読み込み中...</div>;
  }

  if (!character) {
    return (
      <div className={styles.error}>
        キャラクターが見つかりませんでした。
      </div>
    );
  }

  return (
    <div className={styles.characterImagesPage}>
      <div className={styles.header}>
        <h1 className={styles.title}>{character.name} - 画像ギャラリー</h1>
        <Link to={`/characters/${characterId}/sessions`} className={styles.backLink}>
          セッション一覧に戻る
        </Link>
      </div>

      {error && (
        <div className={styles.error}>
          {error}
        </div>
      )}

      <div className={styles.filters}>
        <div className={styles.filterGroup}>
          <label className={styles.filterLabel}>セッションで絞り込み:</label>
          <select
            className={styles.sessionSelect}
            value={selectedSessionId}
            onChange={(e) => {
              setSelectedSessionId(e.target.value);
              setCurrentPage(1);
            }}
          >
            <option value="">すべてのセッション</option>
            {sessions.map((session) => (
              <option key={session.id} value={session.id}>
                {session.title}
              </option>
            ))}
          </select>
        </div>
      </div>

      {isLoading ? (
        <div className={styles.loading}>画像を読み込み中...</div>
      ) : images.length === 0 ? (
        <div className={styles.empty}>
          {selectedSessionId ? 'このセッションには画像がありません。' : 'まだ画像が生成されていません。'}
        </div>
      ) : (
        <>
          <div className={styles.imageGrid}>
            {images.map((image) => (
              <div key={image.messageId} className={styles.imageCard}>
                <div className={styles.imageWrapper}>
                  <img
                    src={image.imageUrl}
                    alt="Generated"
                    className={styles.image}
                    loading="lazy"
                  />
                  <div className={styles.imageActions}>
                    <button
                      className={styles.actionButton}
                      onClick={() => handleShowPrompt(image)}
                      title="プロンプトを表示"
                    >
                      詳細
                    </button>
                    <button
                      className={`${styles.actionButton} ${styles.deleteButton}`}
                      onClick={() => handleDeleteImage(image.messageId)}
                      disabled={isDeleting}
                      title="画像を削除"
                    >
                      削除
                    </button>
                  </div>
                </div>
                <div className={styles.imageInfo}>
                  <div className={styles.imageDate}>
                    {new Date(image.createdAt).toLocaleString('ja-JP')}
                  </div>
                  {image.sessionTitle && (
                    <div className={styles.sessionInfo}>
                      {image.sessionTitle}
                    </div>
                  )}
                  <div className={styles.modelInfo}>
                    {image.serviceName && (
                      <span className={styles.modelBadge}>
                        {image.serviceName}
                      </span>
                    )}
                    {image.modelId && (
                      <span className={styles.modelBadge}>
                        {image.modelId}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className={styles.pagination}>
              <button
                className={styles.pageButton}
                onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                disabled={currentPage === 1}
              >
                前へ
              </button>

              <span className={styles.pageInfo}>
                {currentPage} / {totalPages} ページ (合計 {total} 枚)
              </span>

              <button
                className={styles.pageButton}
                onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                disabled={currentPage === totalPages}
              >
                次へ
              </button>
            </div>
          )}
        </>
      )}

      <PromptModal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        image={selectedImage}
      />
    </div>
  );
};

export default CharacterImagesPage;