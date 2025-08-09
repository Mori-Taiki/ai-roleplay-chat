import React, { useState, useEffect } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useIsAuthenticated } from "@azure/msal-react";
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import { useSessionApi } from '../hooks/useSessionApi';
import { ChatSessionResponse } from '../models/ChatSessionResponse';
import Button from '../components/Button';
import styles from './SessionListPage.module.css';

const SessionListPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const characterId = parseInt(id ?? '0', 10);
  const navigate = useNavigate();
  const isAuthenticated = useIsAuthenticated();
  
  const [sessions, setSessions] = useState<ChatSessionResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [deletingSessionId, setDeletingSessionId] = useState<string | null>(null);

  const { character, isLoading: isLoadingCharacter, fetchCharacter } = useCharacterProfile();
  const { getSessionsForCharacter, createNewSession, deleteSession } = useSessionApi();

  // Load character profile
  useEffect(() => {
    if (characterId > 0) {
      fetchCharacter(characterId);
    }
  }, [characterId, fetchCharacter]);

  // Load sessions for the character
  const loadSessions = async () => {
    if (!characterId || !isAuthenticated) return;
    
    setIsLoading(true);
    setError(null);
    try {
      const sessionList = await getSessionsForCharacter(characterId);
      setSessions(sessionList);
    } catch (err: any) {
      console.error("Failed to load sessions:", err);
      setError(err.message || 'セッション一覧の取得に失敗しました。');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadSessions();
  }, [characterId, isAuthenticated]);

  const handleCreateNewSession = async () => {
    if (!characterId || isCreating) return;

    setIsCreating(true);
    try {
      const newSession = await createNewSession(characterId);
      // Navigate directly to chat with the new session
      navigate(`/chat/${characterId}/${newSession.id}`);
    } catch (err: any) {
      console.error("Failed to create session:", err);
      alert(`エラーが発生しました: ${err.message || '新しいセッションの作成に失敗しました。'}`);
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteSession = async (sessionId: string) => {
    if (window.confirm('このセッションを削除しますか？この操作は元に戻せません。')) {
      setDeletingSessionId(sessionId);
      try {
        await deleteSession(sessionId);
        await loadSessions(); // Reload sessions after deletion
      } catch (err: any) {
        console.error("Session deletion failed:", err);
        alert(`エラーが発生しました: ${err.message || 'セッションの削除に失敗しました。'}`);
      } finally {
        setDeletingSessionId(null);
      }
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('ja-JP', {
      year: 'numeric',
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (!isAuthenticated) {
    return (
      <div className={styles.pageContainer}>
        <p>セッション一覧を表示するには、ログインが必要です。</p>
        <Button as={Link} to="/characters">キャラクター一覧に戻る</Button>
      </div>
    );
  }

  return (
    <div className={styles.pageContainer}>
      <div className={styles.header}>
        <h2>
          {isLoadingCharacter ? 'セッション一覧' : `${character?.name || '不明なキャラクター'}のセッション一覧`}
        </h2>
        <div className={styles.headerActions}>
          <Button as={Link} to="/characters" variant="secondary" size="sm">
            キャラクター一覧に戻る
          </Button>
          <Button 
            onClick={handleCreateNewSession} 
            variant="primary" 
            disabled={isCreating}
          >
            {isCreating ? '作成中...' : '新しいセッション'}
          </Button>
        </div>
      </div>

      {error && <p className={styles.errorMessage}>エラー: {error}</p>}
      
      {isLoading ? (
        <p>セッション一覧を読み込み中...</p>
      ) : (
        <div className={styles.sessionList}>
          {sessions.length === 0 ? (
            <div className={styles.emptyState}>
              <p>まだセッションがありません。</p>
              <p>新しいセッションを作成して会話を始めましょう。</p>
            </div>
          ) : (
            sessions.map((session) => (
              <div key={session.id} className={styles.sessionItem}>
                <div className={styles.sessionInfo}>
                  <div className={styles.sessionMeta}>
                    <span className={styles.sessionDate}>
                      {formatDate(session.createdAt)}
                    </span>
                    <span className={styles.messageCount}>
                      メッセージ数: {session.messageCount}
                    </span>
                  </div>
                  {session.lastMessageSnippet && (
                    <div className={styles.lastMessage}>
                      {session.lastMessageSnippet}
                    </div>
                  )}
                </div>
                <div className={styles.sessionActions}>
                  <Button 
                    as={Link} 
                    to={`/chat/${characterId}/${session.id}`}
                    variant="primary" 
                    size="sm"
                  >
                    会話を続ける
                  </Button>
                  <Button 
                    onClick={() => handleDeleteSession(session.id)}
                    variant="danger" 
                    size="sm"
                    disabled={deletingSessionId === session.id}
                  >
                    {deletingSessionId === session.id ? '削除中...' : '削除'}
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

export default SessionListPage;