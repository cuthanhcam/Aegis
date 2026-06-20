export type StoreCheckRequest = {
  user: string;
  relation: string;
  object: string;
  contextualTuples?: Array<{
    subject: string;
    relation: string;
    object: string;
    effect?: 'allow' | 'deny';
  }>;
  context?: Record<string, unknown>;
  consistency?: string;
  authorizationModelId?: string;
};

export type ExplainTraceStep = {
  step: string;
  result: string;
  tuple?: string;
};

export type CheckResult = {
  allowed: boolean;
  decision: string;
  reasonCode: string;
  trace?: ExplainTraceStep[];
};

export type BatchCheckItemRequest = StoreCheckRequest & {
  correlationId?: string;
};

export type BatchCheckItemResult = {
  correlationId: string;
  result: CheckResult;
};

export type BatchCheckResponse = {
  results: BatchCheckItemResult[];
};

export type OpenFgaTupleKey = {
  user: string;
  relation: string;
  object: string;
};

export type OpenFgaContextualTuples = {
  tuple_keys: OpenFgaTupleKey[];
};

export type OpenFgaCheckRequest = {
  tuple_key: OpenFgaTupleKey;
  contextual_tuples?: OpenFgaContextualTuples;
  consistency?: string;
  authorization_model_id?: string;
  context?: Record<string, unknown>;
};

export type OpenFgaCheckResponse = {
  allowed: boolean;
};

export type OpenFgaBatchCheckItemRequest = {
  tuple_key: OpenFgaTupleKey;
  correlation_id: string;
  contextual_tuples?: OpenFgaContextualTuples;
  consistency?: string;
  authorization_model_id?: string;
  context?: Record<string, unknown>;
};

export type OpenFgaBatchCheckRequest = {
  checks: OpenFgaBatchCheckItemRequest[];
  authorization_model_id?: string;
};

export type OpenFgaErrorResponse = {
  code: string;
  message: string;
};

export type OpenFgaBatchCheckResultItem = {
  correlation_id: string;
  allowed?: boolean | null;
  error?: OpenFgaErrorResponse | null;
};

export type OpenFgaBatchCheckResponse = {
  result: OpenFgaBatchCheckResultItem[];
};
