export type Store = {
  id: string;
  name: string;
  createdAt?: string;
  updatedAt?: string;
  modelCount?: number | null;
  relationshipCount?: number | null;
};
