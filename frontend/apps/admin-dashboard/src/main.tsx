import '@ant-design/v5-patch-for-react-19';
import React from 'react';
import { createRoot } from 'react-dom/client';
import { ConfigProvider } from 'antd';
import enUS from 'antd/locale/en_US';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppRoutes } from './app/routes/AppRoutes';
import { AuthProvider } from './app/providers/AuthProvider';
import { StoreProvider } from './app/providers/StoreProvider';
import { APP_CODE_FONT_FAMILY, APP_FONT_FAMILY } from './shared/utils/fonts';
import 'antd/dist/reset.css';
import './app/styles.css';

const queryClient = new QueryClient();

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <StoreProvider>
          <ConfigProvider
            locale={enUS}
            theme={{
              token: {
                colorPrimary: '#1677ff',
                borderRadius: 8,
                fontFamily: APP_FONT_FAMILY,
                fontFamilyCode: APP_CODE_FONT_FAMILY,
              },
            }}
          >
            <BrowserRouter>
              <AppRoutes />
            </BrowserRouter>
          </ConfigProvider>
        </StoreProvider>
      </AuthProvider>
    </QueryClientProvider>
  </React.StrictMode>,
);



