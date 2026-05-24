import { useState } from 'react';
import { Alert, Button, Card, Form, Input, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/app/providers/useAuth';

export function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const demoUsername = import.meta.env.VITE_AEGIS_DEMO_USERNAME ?? '';
  const demoPassword = import.meta.env.VITE_AEGIS_DEMO_PASSWORD ?? '';

  const onFinish = async (values: { username: string; password: string }) => {
    setError('');
    setLoading(true);
    try {
      await login(values.username, values.password);
      navigate('/stores', { replace: true });
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <Card className="login-card" variant="borderless">
        <Typography.Title level={3} style={{ marginTop: 0, marginBottom: 4 }}>
          Aegis Admin
        </Typography.Title>
        <Typography.Paragraph type="secondary" style={{ marginTop: 0 }}>
          Sign in to manage authorization stores, models, relationships and audits.
        </Typography.Paragraph>

        {error ? <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} /> : null}

        <Form
          layout="vertical"
          initialValues={{ username: demoUsername, password: demoPassword }}
          onFinish={onFinish}
        >
          <Form.Item name="username" label="Username" rules={[{ required: true }]}>
            <Input autoComplete="username" />
          </Form.Item>
          <Form.Item name="password" label="Password" rules={[{ required: true }]}>
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={loading} block>
            Login
          </Button>
        </Form>

        <Typography.Paragraph className="demo-hint" style={{ marginTop: 12 }}>
          Demo credentials are loaded from environment variables when configured.
        </Typography.Paragraph>
      </Card>
    </div>
  );
}



