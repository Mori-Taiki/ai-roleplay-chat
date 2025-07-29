import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // ポートを明示的に指定
    port: 5173,
    // Dockerコンテナ内で外部からのアクセスを許可
    host: '0.0.0.0',
    proxy: {
      // '/api' というパスで始まるリクエストをプロキシする
      '/api': {
        // 転送先はdocker-compose.ymlで定義したバックエンドサービスのURL
        target: 'http://backend:7000',
        // オリジンを書き換える
        changeOrigin: true,
        // パスの '/api' を削除しない（バックエンドのルートに合わせる）
        rewrite: (path) => path.replace(/^\/api/, '/api'),
      },
    },
  },
})