import { useContext } from 'react';
import { StoreContext } from './store-context';

export function useActiveStore() {
  const ctx = useContext(StoreContext);
  if (!ctx) {
    throw new Error('useActiveStore must be used inside StoreProvider');
  }

  return ctx;
}

