import { ComponentPropsWithoutRef } from 'react';
import clsx from 'clsx';

interface Props extends ComponentPropsWithoutRef<'div'> {
  hide?: boolean;
}

export const BgtCenteredCard = (props: Props) => {
  const { children, className } = props;

  return (
    <div className="grid place-items-center md:h-full">
      <div
        className={clsx(
          'border-card-border border rounded-lg bg-card-black p-6 md:p-10 flex flex-col items-center gap-6',
          'xl:min-w-[650px] lg:min-w-[500px] md:min-w-[450px]',
          className
        )}
      >
        {children}
      </div>
    </div>
  );
};
