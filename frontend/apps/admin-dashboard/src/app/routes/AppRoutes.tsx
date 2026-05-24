import { Navigate, Route, Routes } from 'react-router-dom';
import { MainLayout } from '@/shared/layouts';
import { ProtectedRoute } from '@/shared/ui';
import { LoginPage } from '@/features/auth';
import { protectedRoutes } from './route-config';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/stores" replace />} />
        {protectedRoutes.map((route) => (
          <Route key={route.path} path={route.path} element={route.element} />
        ))}
      </Route>
      <Route path="*" element={<Navigate to="/stores" replace />} />
    </Routes>
  );
}



