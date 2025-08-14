import React, { useState, useEffect, useCallback } from 'react';
import { useImageApi, ImageItem } from '../hooks/useImageApi';
import styles from './AvatarSelectionModal.module.css';

interface AvatarSelectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  characterId: number;
  onSelectAvatar: (imageUrl: string) => void;
}

const AvatarSelectionModal: React.FC<AvatarSelectionModalProps> = ({
  isOpen,
  onClose,
  characterId,
  onSelectAvatar,
}) => {
  const [currentPage, setCurrentPage] = useState(1);
  const {
    isLoading,
    images,
    total,
    pageSize,
    getImages,
    error,
  } = useImageApi();

  const loadImages = useCallback(() => {
    if (characterId > 0) {
      getImages(characterId, undefined, currentPage, 20); // Use smaller page size for modal
    }
  }, [characterId, currentPage, getImages]);

  useEffect(() => {
    if (isOpen) {
      loadImages();
    }
  }, [isOpen, loadImages]);

  const handleSelectImage = (imageUrl: string) => {
    onSelectAvatar(imageUrl);
    onClose();
  };

  const totalPages = Math.ceil(total / pageSize);

  if (!isOpen) return null;

  return (
    <div className={styles.modal} onClick={onClose}>
      <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h3 className={styles.modalTitle}>アバター画像を選択</h3>
          <button className={styles.closeButton} onClick={onClose}>×</button>
        </div>

        <div className={styles.modalBody}>
          {error && (
            <div className={styles.error}>
              {error}
            </div>
          )}

          {isLoading ? (
            <div className={styles.loading}>画像を読み込み中...</div>
          ) : images.length === 0 ? (
            <div className={styles.empty}>
              まだ画像が生成されていません。チャットで画像を生成してから設定してください。
            </div>
          ) : (
            <>
              <div className={styles.imageGrid}>
                {images.map((image: ImageItem) => (
                  <div
                    key={image.messageId}
                    className={styles.imageCard}
                    onClick={() => handleSelectImage(image.imageUrl)}
                  >
                    <img
                      src={image.imageUrl}
                      alt="Generated"
                      className={styles.image}
                      loading="lazy"
                    />
                    <div className={styles.imageOverlay}>
                      <button className={styles.selectButton}>
                        選択
                      </button>
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
                    {currentPage} / {totalPages} ページ
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
        </div>
      </div>
    </div>
  );
};

export default AvatarSelectionModal;