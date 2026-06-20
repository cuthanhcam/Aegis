import { Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { MainLayout } from '@/shared/layouts';
import { ProtectedRoute, TableSkeleton } from '@/shared/ui';
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
        <Route index element={<Navigate to="/overview" replace />} />
        {protectedRoutes.map(({ Component, path }) => (
          <Route
            key={path}
            path={path}
            element={
              <Suspense fallback={<TableSkeleton rows={6} columns={5} />}>
                <Component />
              </Suspense>
            }
          />
        ))}
      </Route>
      <Route path="*" element={<Navigate to="/overview" replace />} />
    </Routes>
  );
}
