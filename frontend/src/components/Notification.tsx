import React, { useEffect, useState } from 'react';
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

  const handleClose = () => {
    setIsClosing(true);
    setTimeout(() => {
      onClose(id);
    }, 300); // Animation duration
  };

  useEffect(() => {
    if (type !== 'loading') {
      const timer = setTimeout(() => {
        handleClose();
      }, duration);

      return () => {
        clearTimeout(timer);
      };
    }
  }, [id, type, duration, onClose]);

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
