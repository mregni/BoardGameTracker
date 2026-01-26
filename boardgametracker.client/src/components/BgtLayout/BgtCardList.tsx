export const BgtCardList = ({ children, ...props }: React.HTMLAttributes<HTMLDivElement>) => {
  return (
    <div
      className="grid gap-3 grid-cols-2 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-7 3xl:grid-cols-8"
      {...props}
    >
      {children}
    </div>
  );
};
