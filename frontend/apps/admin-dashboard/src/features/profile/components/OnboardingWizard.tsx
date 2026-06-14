import { useState } from 'react';
import { Modal, Form, Input, Button, Space, Spin } from 'antd';
import { PlusOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/shared/api';
import { useNotification } from '@/shared/hooks';

interface OnboardingWizardProps {
  visible: boolean;
  onClose: () => void;
  onComplete?: () => void;
}

/**
 * First-time user onboarding wizard
 * Guides users to create their first store and authorization model
 */
export const OnboardingWizard: React.FC<OnboardingWizardProps> = ({
  visible,
  onClose,
  onComplete
}) => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const notification = useNotification();
  const [currentStep, setCurrentStep] = useState<'welcome' | 'create'>('welcome');

  const createStoreMutation = useMutation({
    mutationFn: (name: string) => apiClient.createStore(name),
    onSuccess: () => {
      notification.success('Sample store created! Start by creating an authorization model.');
      queryClient.invalidateQueries({ queryKey: ['stores'] });
      setCurrentStep('welcome');
      form.resetFields();
      onClose();
      onComplete?.();
    },
    onError: (error: unknown) => {
      notification.error(error instanceof Error ? error.message : 'Failed to create store');
    },
  });

  const handleCreateSample = async () => {
    try {
      await form.validateFields();
      const storeName = form.getFieldValue('storeName');
      createStoreMutation.mutate(storeName || 'My First Store');
    } catch {
      // Validation failed
    }
  };

  return (
    <Modal
      title="Welcome to Aegis"
      open={visible}
      onCancel={onClose}
      width={600}
      footer={null}
      centered
      wrapClassName="onboarding-wizard-modal"
    >
      {currentStep === 'welcome' && (
        <div className="onboarding-content">
          <div className="onboarding-section">
            <h3>Getting Started</h3>
            <p>
              Aegis is an open-source authorization-as-a-service platform powered by OpenFGA.
            </p>
            <p className="text-muted">
              Let's create your first <strong>Store</strong> and <strong>Authorization Model</strong> to get started.
            </p>
          </div>

          <div className="onboarding-section">
            <h4>What is a Store?</h4>
            <p>
              A Store is a container for authorization models and relationships. Each store manages
              its own set of access control rules.
            </p>
          </div>

          <div className="onboarding-section">
            <h4>What is an Authorization Model?</h4>
            <p>
              An Authorization Model (using OpenFGA DSL) defines the structure of your access control:
              types, relations, and permission rules.
            </p>
          </div>

          <div className="onboarding-actions">
            <Space direction="vertical" style={{ width: '100%' }}>
              <Button
                block
                type="primary"
                size="large"
                icon={<PlusOutlined />}
                onClick={() => setCurrentStep('create')}
              >
                Create Sample Store
              </Button>
              <Button
                block
                onClick={onClose}
              >
                Skip for Now
              </Button>
            </Space>
          </div>
        </div>
      )}

      {currentStep === 'create' && (
        <div className="onboarding-content">
          <Spin spinning={createStoreMutation.isPending}>
            <Form
              form={form}
              layout="vertical"
              autoComplete="off"
            >
              <Form.Item
                label="Store Name"
                name="storeName"
                rules={[
                  { required: false },
                  { max: 256, message: 'Store name must be less than 256 characters' }
                ]}
              >
                <Input
                  placeholder="e.g., Document Management, GitHub-like Repos"
                  size="large"
                />
              </Form.Item>

              <Form.Item>
                <p className="text-muted text-sm">
                  If left empty, we'll create a store called "My First Store"
                </p>
              </Form.Item>
            </Form>

            <div className="onboarding-actions">
              <Space style={{ width: '100%' }}>
                <Button
                  onClick={() => setCurrentStep('welcome')}
                  disabled={createStoreMutation.isPending}
                >
                  Back
                </Button>
                <Button
                  type="primary"
                  onClick={handleCreateSample}
                  icon={<CheckCircleOutlined />}
                  loading={createStoreMutation.isPending}
                  style={{ flex: 1 }}
                >
                  Create Store
                </Button>
              </Space>
            </div>
          </Spin>
        </div>
      )}

      <style>{`
        .onboarding-wizard-modal .ant-modal-content {
          padding: 24px;
        }

        .onboarding-content {
          max-height: 400px;
          overflow-y: auto;
        }

        .onboarding-section {
          margin-bottom: 20px;
        }

        .onboarding-section h3 {
          margin: 0 0 12px 0;
          font-size: 18px;
          font-weight: 600;
        }

        .onboarding-section h4 {
          margin: 0 0 8px 0;
          font-size: 14px;
          font-weight: 600;
          color: #1890ff;
        }

        .onboarding-section p {
          margin: 0 0 8px 0;
          color: #595959;
          line-height: 1.5;
        }

        .text-muted {
          color: #8c8c8c !important;
        }

        .text-sm {
          font-size: 12px;
        }

        .onboarding-actions {
          margin-top: 24px;
          padding-top: 16px;
          border-top: 1px solid #f0f0f0;
        }
      `}</style>
    </Modal>
  );
};
