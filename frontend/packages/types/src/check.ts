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
