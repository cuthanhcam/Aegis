import { CopyOutlined } from '@ant-design/icons';
import { Button, Card, Descriptions, Space, Tag, Typography, message } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useAuth } from '@/app/providers/useAuth';
import { apiClient } from '@/shared/api';
import { OnboardingWizard } from '@/features/profile/components/OnboardingWizard';

export function ProfilePage() {
  const { accessToken } = useAuth();
  const [showOnboarding, setShowOnboarding] = useState(false);
  const [hasShownWizard, setHasShownWizard] = useState(false);

  const profileQuery = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiClient.getProfile(),
    enabled: Boolean(accessToken),
  });

  const storesQuery = useQuery({
    queryKey: ['stores'],
    queryFn: () => apiClient.listStores(),
    enabled: Boolean(accessToken),
  });

  const shouldAutoShowOnboarding = !hasShownWizard && Boolean(storesQuery.data) && (storesQuery.data?.length ?? 0) === 0;
  const onboardingVisible = showOnboarding || shouldAutoShowOnboarding;

  const copyToken = async () => {
    if (!accessToken) {
      return;
    }

    await navigator.clipboard.writeText(accessToken);
    message.success('Access token copied.');
  };

  return (
    <>
      <OnboardingWizard
        visible={onboardingVisible}
        onClose={() => {
          setShowOnboarding(false);
          setHasShownWizard(true);
        }}
        onComplete={() => {
          setShowOnboarding(false);
          setHasShownWizard(true);
          storesQuery.refetch();
        }}
      />

      <Card>
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <div>
            <Typography.Title level={4} style={{ marginBottom: 4 }}>
              User Profile
            </Typography.Title>
            <Typography.Text type="secondary">
              Session and identity details for the currently logged-in account.
            </Typography.Text>
          </div>

          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Username">{profileQuery.data?.username || '-'}</Descriptions.Item>
            <Descriptions.Item label="Tenant">{profileQuery.data?.tenantId || '-'}</Descriptions.Item>
            <Descriptions.Item label="Roles">
              {(profileQuery.data?.roles ?? []).length > 0 ? (
                (profileQuery.data?.roles ?? []).map((role) => <Tag key={role}>{role}</Tag>)
              ) : (
                <Tag>none</Tag>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Token Expires At">
              {profileQuery.data?.expiresAt ? new Date(profileQuery.data.expiresAt).toLocaleString('en-US') : '-'}
            </Descriptions.Item>
          </Descriptions>

          {profileQuery.error ? (
            <Typography.Text type="danger">{(profileQuery.error as Error).message}</Typography.Text>
          ) : null}

          <Space>
            <Button icon={<CopyOutlined />} onClick={copyToken}>
              Copy Access Token
            </Button>
          </Space>

          <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
            This profile view is read-only. User creation and lifecycle are currently controlled by backend/auth providers.
          </Typography.Paragraph>
        </Space>
      </Card>
    </>
  );
}



