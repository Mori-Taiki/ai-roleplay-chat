// frontend/src/App.tsx (修正・リファクタリング案)
import React from 'react';
// Routes, Route, Link, useParams を react-router-dom からインポート
import { Routes, Route, Link } from 'react-router-dom';
import './index.css';
// 将来的には実際のコンポーネントを import します
import ChatPage from './pages/ChatPage';
import CharacterListPage from './pages/CharacterListPage';
import CharacterSetupPage from './pages/CharacterSetupPage';

// --- 仮のプレースホルダーコンポーネント (後で別ファイルに移動推奨) ---
// ナビゲーションとタイトル、子要素を表示する簡単なラッパー
const Placeholder = ({ title, children }: { title: string; children?: React.ReactNode }) => (
  <div>
    {/* 簡単なナビゲーションリンク */}
    <nav
      style={{
        padding: '1rem',
        borderBottom: '1px solid #ccc',
        marginBottom: '1rem',
      }}
    >
      <Link to="/">Chat</Link> | <Link to="/characters">Characters</Link> |{' '}
      <Link to="/characters/new">New Character</Link>
    </nav>
    <h2>{title}</h2>
    {/* props.children があれば表示 */}
    {children}
    <p>
      <i>(This is a placeholder page)</i>
    </p>
  </div>
);

function App() {
  return (
    <Routes>
      <Route path="/" element={<div>ホームページ (仮)</div>} />
      <Route path="/characters" element={<CharacterListPage />} />
      <Route path="/characters/new" element={<CharacterSetupPage />} />
      <Route path="/characters/edit/:id" element={<CharacterSetupPage />} />
      <Route path="/chat/:id" element={<ChatPage />} />
      <Route
        path="*"
        element={
          <Placeholder title="404 Not Found">
            <p>お探しのページは見つかりませんでした。</p>
          </Placeholder>
        }
      />
    </Routes>
  );
}

export default App;
