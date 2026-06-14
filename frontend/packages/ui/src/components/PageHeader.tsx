type PageHeaderProps = {
  title: string;
  subtitle?: string;
};

export function PageHeader({ title, subtitle }: PageHeaderProps) {
  return (
    <header>
      <h1 className="page-title">{title}</h1>
      {subtitle ? <p>{subtitle}</p> : null}
    </header>
  );
}
