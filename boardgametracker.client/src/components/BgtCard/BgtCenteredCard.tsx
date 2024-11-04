import { HTMLAttributes } from 'react';
import clsx from 'clsx';

interface Props extends HTMLAttributes<HTMLDivElement> {
  hide?: boolean;
}

export const BgtCenteredCard = (props: Props) => {
  const { children, className, ...rest } = props;

  return (
    <div className="grid place-items-center md:h-full" {...rest}>
      <div
        className={clsx(
          'border-card-border border rounded-lg bg-card-black p-6 md:p-10 flex flex-col items-center gap-6',
          'min-w-full xl:min-w-[650px] lg:min-w-[500px] md:min-w-[450px]',
          className
        )}
      >
        {children}
      </div>
    </div>
  );
};
