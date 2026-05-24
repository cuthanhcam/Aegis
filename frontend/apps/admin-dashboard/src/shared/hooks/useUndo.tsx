import { useCallback, useState } from 'react';
import { Button } from 'antd';
import { useNotification } from './useNotification';

export interface UndoableAction {
  id: string;
  label: string;
  onUndo: () => Promise<void>;
  timestamp: number;
}

export interface UseUndoOptions {
  timeoutMs?: number; // default 7000ms
  maxHistory?: number; // default 10 actions
}

const DEFAULT_TIMEOUT = 7000;
const DEFAULT_MAX_HISTORY = 10;

export function useUndo(options: UseUndoOptions = {}) {
  const { timeoutMs = DEFAULT_TIMEOUT, maxHistory = DEFAULT_MAX_HISTORY } = options;
  const notificationHook = useNotification();
  const [history, setHistory] = useState<UndoableAction[]>([]);
  const [activeTimers, setActiveTimers] = useState<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const undo = useCallback(
    (actionId: string) => {
      const action = history.find((a) => a.id === actionId);
      if (!action) return;

      // Clear the timeout if it exists
      const timer = activeTimers.get(actionId);
      if (timer) {
        clearTimeout(timer);
        setActiveTimers((prev) => {
          const next = new Map(prev);
          next.delete(actionId);
          return next;
        });
      }

      setHistory((prev) => prev.filter((a) => a.id !== actionId));
      notificationHook.success(`✓ ${action.label} - cancelled, item is safe`);
    },
    [history, activeTimers, notificationHook],
  );

  const registerAction = useCallback(
    (action: Omit<UndoableAction, 'timestamp'>) => {
      const fullAction: UndoableAction = {
        ...action,
        timestamp: Date.now(),
      };

      // Add to history
      setHistory((prev) => [fullAction, ...prev].slice(0, maxHistory));

      // Show persistent notification with timeout and undo button
      const timeoutSecs = Math.round(timeoutMs / 1000);

      // Create a callback to handle undo
      const handleUndo = () => undo(fullAction.id);

      notificationHook.notifyRich({
        type: 'warning',
        title: `⏳ ${action.label}`,
        description: `Will be permanently deleted in ${timeoutSecs}s. This action cannot be undone.`,
        duration: timeoutMs / 1000,
        action: (
          <Button 
            size="small" 
            onClick={handleUndo}
            style={{
              background: '#1890ff',
              color: 'white',
            }}
          >
            Cancel Deletion
          </Button>
        ),
      });

      // Auto-execute deletion after timeout
      const timer = setTimeout(async () => {
        try {
          await fullAction.onUndo();
          notificationHook.notifyRich({
            type: 'success',
            title: '✓ Deleted',
            description: `${action.label} has been permanently deleted.`,
            duration: 3,
          });
          setHistory((prev) => prev.filter((a) => a.id !== fullAction.id));
        } catch (error) {
          notificationHook.notifyRich({
            type: 'error',
            title: '✗ Deletion Failed',
            description: error instanceof Error ? error.message : 'Unable to complete deletion.',
            duration: 5,
          });
          // Keep in history for retry
        } finally {
          setActiveTimers((prev) => {
            const next = new Map(prev);
            next.delete(fullAction.id);
            return next;
          });
        }
      }, timeoutMs);

      setActiveTimers((prev) => new Map(prev).set(fullAction.id, timer));

      return () => {
        clearTimeout(timer);
        setActiveTimers((prev) => {
          const next = new Map(prev);
          next.delete(fullAction.id);
          return next;
        });
      };
    },
    [timeoutMs, maxHistory, notificationHook, undo],
  );

  return {
    registerAction,
    undo,
    history,
  };
}
