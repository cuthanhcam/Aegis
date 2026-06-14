import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';

export interface UseUrlStateOptions {
  defaultValues?: Record<string, string>;
}

export function useUrlState(options: UseUrlStateOptions = {}) {
  const { defaultValues = {} } = options;
  const [searchParams, setSearchParams] = useSearchParams();

  // Get current state from URL
  const state = useMemo(() => {
    const currentState: Record<string, string> = { ...defaultValues };
    searchParams.forEach((value, key) => {
      currentState[key] = value;
    });
    return currentState;
  }, [searchParams, defaultValues]);

  // Update a single field
  const setState = useCallback(
    (key: string, value: string | null) => {
      const newParams = new URLSearchParams(searchParams);
      if (value === null || value === '') {
        newParams.delete(key);
      } else {
        newParams.set(key, value);
      }
      setSearchParams(newParams);
    },
    [searchParams, setSearchParams],
  );

  // Update multiple fields at once
  const setStates = useCallback(
    (updates: Record<string, string | null>) => {
      const newParams = new URLSearchParams(searchParams);
      Object.entries(updates).forEach(([key, value]) => {
        if (value === null || value === '') {
          newParams.delete(key);
        } else {
          newParams.set(key, value);
        }
      });
      setSearchParams(newParams);
    },
    [searchParams, setSearchParams],
  );

  // Clear all params (reset to defaults)
  const clearState = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  // Get specific value with fallback
  const getState = useCallback(
    (key: string, defaultValue: string = '') => {
      return state[key] ?? defaultValue;
    },
    [state],
  );

  return {
    state,
    setState,
    setStates,
    getState,
    clearState,
  };
}
