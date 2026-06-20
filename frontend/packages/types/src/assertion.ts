export type AssertionTupleKey = {
  user: string;
  relation: string;
  object: string;
};

export type AssertionItem = {
  tuple_key: AssertionTupleKey;
  expectation: boolean;
  contextual_tuples?: {
    tuple_keys: AssertionTupleKey[];
  } | null;
};

export type ReadAssertionsResponse = {
  authorization_model_id: string;
  assertions: AssertionItem[];
};

export type WriteAssertionsRequest = {
  assertions: AssertionItem[];
};

export type AssertionRunSummary = {
  total: number;
  passed: number;
  failed: number;
};

export type AssertionRunResultItem = {
  tuple_key: AssertionTupleKey;
  expected: boolean;
  actual: boolean;
  passed: boolean;
  decision: string;
  reason: string;
  explain_trace_id?: string | null;
};

export type AssertionRunRecord = {
  run_id: string;
  store_id: string;
  authorization_model_id: string;
  started_at: string;
  completed_at: string;
  summary: AssertionRunSummary;
  results: AssertionRunResultItem[];
};

export type AssertionRunListResponse = {
  runs: AssertionRunRecord[];
};
