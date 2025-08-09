// App.tsx (修正案)
import React from 'react';
// ★ Outlet を react-router-dom からインポート
import { Routes, Route, Link, Outlet} from 'react-router-dom';
import './index.css'; // または App.css
import ChatPage from './pages/ChatPage';
import CharacterListPage from './pages/CharacterListPage';
import CharacterSetupPage from './pages/CharacterSetupPage';
import SettingsPage from './pages/SettingsPage';
import SessionListPage from './pages/SessionListPage';
import { AuthStatus } from './components/AuthStatus'; // AuthStatus をインポート

const AppLayout: React.FC = () => {
  return (
    <div>
      {/* アプリケーション共通のヘッダーやナビゲーション */}
      <nav style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        borderBottom: '1px solid #eee',
        marginBottom: '1rem'
      }}>
        {/* 左側のナビゲーションリンク */}
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', gap: '1rem' }}>
          {/* <li><Link to="/">ホーム (仮)</Link></li> */} {/* 必要ならホームへのリンク */}
          <li><Link to="/characters">キャラクター一覧</Link></li>
          <li><Link to="/characters/new">新規キャラクター作成</Link></li>
          <li><Link to="/settings">設定</Link></li>
          {/* 他に共通リンクがあればここに追加 */}
        </ul>

        {/* ★ 右側に認証ステータスを表示 */}
        <AuthStatus />
      </nav>

      {/* ページごとのコンテンツがここに表示される */}
      <main>
        <Outlet /> {/* react-router-dom v6 の機能 */}
      </main>

      {/* (任意) フッターなど共通要素 */}
    </div>
  );
};

function App() {
  return (
      <Routes>
        {/* ★ AppLayout を使うルートを定義 */}
        <Route path="/" element={<AppLayout />}> {/* ← 親ルート */}
          {/* ↓ AppLayout の Outlet に表示される子ルートたち */}
          <Route index element={<CharacterListPage />} /> {/* / にアクセスした場合 */}
          <Route path="characters" element={<CharacterListPage />} />
          <Route path="characters/new" element={<CharacterSetupPage />} />
          <Route path="characters/edit/:id" element={<CharacterSetupPage />} />
          <Route path="characters/:id/sessions" element={<SessionListPage />} />
          <Route path="chat/:id" element={<ChatPage />} />
          <Route path="chat/:characterId/:sessionId" element={<ChatPage />} />
          <Route path="settings" element={<SettingsPage />} />

          {/* 404 Not Found ページ */}
          <Route
            path="*"
            element={
              <div>
                <h2>404 Not Found</h2>
                <p>お探しのページは見つかりませんでした。</p>
              </div>
            }
          />
        </Route> 

        {/* もしログインページなど、AppLayout を使わない独立したページがあれば、ここに追加 */}
        {/* <Route path="/signin-oidc" element={<SigninCallback />} /> */}

      </Routes>
  );
}

export default App;