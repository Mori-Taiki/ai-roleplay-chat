import React, { useEffect, useState, useCallback } from 'react';
import styles from './Notification.module.css';

export interface NotificationProps {
  id: string;
  message: string;
  type: 'success' | 'error' | 'loading';
  onClose: (id: string) => void;
  duration?: number;
}

const Notification: React.FC<NotificationProps> = ({ id, message, type, onClose, duration = 5000 }) => {
  const [isClosing, setIsClosing] = useState(false);

  const handleClose = useCallback(() => {
    setIsClosing(true);
    setTimeout(() => {
      onClose(id);
    }, 300); // Animation duration
  }, [id, onClose]);

  useEffect(() => {
    if (type !== 'loading') {
      const timer = setTimeout(handleClose, duration);

      return () => {
        clearTimeout(timer);
      };
    }
  }, [type, duration, handleClose]);

  return (
    <div className={`${styles.notification} ${styles[type]} ${isClosing ? styles.closing : ''}`}>
      <div className={styles.message}>
        {type === 'loading' && <div className={styles.spinner}></div>}
        <span>{message}</span>
      </div>
      {type !== 'loading' && (
        <button onClick={handleClose} className={styles.closeButton}>
          &times;
        </button>
      )}
    </div>
  );
};

export default Notification;
