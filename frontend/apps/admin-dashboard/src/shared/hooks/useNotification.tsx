import { message, notification } from 'antd';
import { useCallback } from 'react';

/**
 * Unified notification hook for consistent success/error/warning/info notifications
 * across the entire Aegis application.
 * 
 * Usage:
 * const notification = useNotification();
 * notification.success('Store created successfully');
 * notification.error('Failed to delete store');
 * notification.warning('Item will be deleted', { action: <Button>Undo</Button> });
 */
export const useNotification = () => {
  const success = useCallback((msg: string, duration: number = 3) => {
    message.success({
      content: msg,
      duration,
      style: { marginTop: '20px' }
    });
  }, []);

  const error = useCallback((msg: string, duration: number = 4.5) => {
    message.error({
      content: msg,
      duration,
      style: { marginTop: '20px' }
    });
  }, []);

  const warning = useCallback((msg: string, options?: { duration?: number; action?: React.ReactNode }) => {
    const duration = options?.duration ?? 3;
    
    if (options?.action) {
      // Use notification API for rich content
      notification.warning({
        message: msg,
        description: null,
        duration: duration,
        btn: options.action,
        style: { marginTop: '20px' }
      });
    } else {
      // Use message API for simple text
      message.warning({
        content: msg,
        duration,
        style: { marginTop: '20px' }
      });
    }
  }, []);

  const info = useCallback((msg: string, duration: number = 3) => {
    message.info({
      content: msg,
      duration,
      style: { marginTop: '20px' }
    });
  }, []);

  // Extended notification method for complex notifications
  const notifyRich = useCallback((config: {
    type: 'success' | 'error' | 'warning' | 'info';
    title: string;
    description?: string;
    duration?: number;
    action?: React.ReactNode;
  }) => {
    notification.open({
      type: config.type,
      message: config.title,
      description: config.description || null,
      duration: config.duration ?? 3,
      btn: config.action,
      style: { marginTop: '20px' },
    });
  }, []);

  return { success, error, warning, info, notifyRich };
};


