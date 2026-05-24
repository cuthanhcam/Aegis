type AccessGateProps = {
  title: string;
  message: string;
};

export function AccessGate({ title, message }: AccessGateProps) {
  return (
    <section className="card">
      <h1 className="page-title">{title}</h1>
      <p className="hint">{message}</p>
    </section>
  );
}
