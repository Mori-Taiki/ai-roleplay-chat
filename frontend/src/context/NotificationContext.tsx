import React, { createContext, useState, useCallback, ReactNode } from 'react';
import { v4 as uuidv4 } from 'uuid';
import Notification, { NotificationProps } from '../components/Notification';
import styles from '../components/Notification.module.css'; // For the container

type NotificationData = Omit<NotificationProps, 'id' | 'onClose'>;

interface NotificationContextType {
  addNotification: (notification: NotificationData) => string; // Returns ID
  removeNotification: (id: string) => void;
}

export const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<Omit<NotificationProps, 'onClose'>[]>([]);

  const removeNotification = useCallback((id: string) => {
    setNotifications((current) => current.filter((n) => n.id !== id));
  }, []);

  const addNotification = useCallback((notification: NotificationData): string => {
    const id = uuidv4();
    setNotifications((current) => [...current, { id, ...notification }]);
    return id;
  }, []);

  return (
    <NotificationContext.Provider value={{ addNotification, removeNotification }}>
      {children}
      <div className={styles.notificationContainer}>
        {notifications.map((n) => (
          <Notification key={n.id} {...n} onClose={removeNotification} />
        ))}
      </div>
    </NotificationContext.Provider>
  );
};
