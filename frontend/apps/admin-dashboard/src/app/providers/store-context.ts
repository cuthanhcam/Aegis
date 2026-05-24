import { createContext } from 'react';

export type StoreContextValue = {
  activeStoreId: string;
  setActiveStoreId: (storeId: string) => void;
};

export const StoreContext = createContext<StoreContextValue | null>(null);

