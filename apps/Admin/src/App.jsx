import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { AuthProvider } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout/Layout';
import Login from './pages/Login/Login';

const DiagnosticosPage = lazy(() => import('./pages/Diagnosticos/index'));
const KanbanPage       = lazy(() => import('./pages/Kanban/index'));

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <Router basename="/admin">
          <Suspense fallback={<div className="p-6 text-muted-foreground">Carregando...</div>}>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <Layout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<Navigate to="/diagnosticos" replace />} />
                <Route path="diagnosticos" element={<DiagnosticosPage />} />
                <Route path="kanban"       element={<KanbanPage />} />
              </Route>
              <Route path="*" element={<Navigate to="/diagnosticos" replace />} />
            </Routes>
          </Suspense>
        </Router>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
