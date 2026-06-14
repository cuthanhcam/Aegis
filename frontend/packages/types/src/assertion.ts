export type AssertionTupleKey = {
  user: string;
  relation: string;
  object: string;
};

export type AssertionItem = {
  tuple_key: AssertionTupleKey;
  expectation: boolean;
};

export type ReadAssertionsResponse = {
  authorization_model_id: string;
  assertions: AssertionItem[];
};

export type WriteAssertionsRequest = {
  assertions: AssertionItem[];
};
