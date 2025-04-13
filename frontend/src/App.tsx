// frontend/src/App.tsx (修正・リファクタリング案)
import React from 'react';
// Routes, Route, Link, useParams を react-router-dom からインポート
import { Routes, Route, Link, useParams } from 'react-router-dom';
import './index.css';
// 将来的には実際のコンポーネントを import します
// import ChatPage from './pages/ChatPage';
import CharacterListPage from './pages/CharacterListPage';
import CharacterSetupPage from './pages/CharacterSetupPage';

// --- 仮のプレースホルダーコンポーネント (後で別ファイルに移動推奨) ---
// ナビゲーションとタイトル、子要素を表示する簡単なラッパー
const Placeholder = ({ title, children }: { title: string, children?: React.ReactNode }) => (
  <div>
    {/* 簡単なナビゲーションリンク */}
    <nav style={{ padding: '1rem', borderBottom: '1px solid #ccc', marginBottom: '1rem' }}>
      <Link to="/">Chat</Link> | {' '}
      <Link to="/characters">Characters</Link> | {' '}
      <Link to="/characters/new">New Character</Link>
    </nav>
    <h2>{title}</h2>
    {/* props.children があれば表示 */}
    {children}
    <p><i>(This is a placeholder page)</i></p>
  </div>
);

// 各ルートに対応する仮のコンポーネント定義
const ChatPagePlaceholder = () => <Placeholder title="Chat Interface" />;
const CharacterListPagePlaceholder = () => <Placeholder title="Character List" />;
const CharacterSetupPagePlaceholder = () => {
  // URLから :id パラメータを取得してみる例 (編集モード用)
  const { id } = useParams<{ id: string }>(); // id パラメータを取得
  const isEditMode = !!id; // id が存在すれば編集モード
  const title = isEditMode ? `Edit Character (ID: ${id})` : "Create New Character";
  // モードに応じてタイトルを変更
  return <Placeholder title={title} />;
};
// --- ここまで仮のコンポーネント ---

function App() {

  return (
      <Routes>
        {/* ルートと対応するコンポーネントのマッピング */}
        <Route path="/" element={<ChatPagePlaceholder />} />
        <Route path="/characters" element={<CharacterListPage />} />
        <Route path="/characters/new" element={<CharacterSetupPage />} />
        {/* :id はURLパラメータ。例: /characters/edit/5 の '5' が id になる */}
        <Route path="/characters/edit/:id" element={<CharacterSetupPage />} />
        {/* 上記以外のパスにマッチした場合の Not Found ページ */}
        <Route path="*" element={
          <Placeholder title="404 Not Found">
            <p>お探しのページは見つかりませんでした。</p>
          </Placeholder>
        } />
      </Routes>
  );
}

export default App;