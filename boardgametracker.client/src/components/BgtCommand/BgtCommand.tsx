import { Bars } from 'react-loading-icons';
import { ComponentPropsWithoutRef } from 'react';
import { Command as CommandPrimitive } from 'cmdk';
import clsx from 'clsx';
import { MagnifyingGlassIcon, PlusCircleIcon } from '@heroicons/react/24/outline';

import { BgtIcon } from '../BgtIcon/BgtIcon';

export const BgtCommand = (props: ComponentPropsWithoutRef<typeof CommandPrimitive>) => {
  const { className, ...rest } = props;

  return (
    <CommandPrimitive
      className={clsx(
        'mx-auto w-full transform overflow-hidden rounded-md bg-input shadow-md transition-all p-1',
        className
      )}
      {...rest}
    />
  );
};

export const BgtCommandInput = (props: ComponentPropsWithoutRef<typeof CommandPrimitive.Input>) => {
  const { className, ...rest } = props;

  return (
    // eslint-disable-next-line react/no-unknown-property
    <div className="flex flex-row justify-start gap-2 items-center p-2" cmdk-input-wrapper="">
      <BgtIcon icon={<MagnifyingGlassIcon />} />
      <CommandPrimitive.Input
        className={clsx(
          'h-8 w-full border-0 bg-transparent focus:ring-0 focus-visible:outline-0 text-[14px]',
          className
        )}
        {...rest}
      />
    </div>
  );
};

export const BgtCommandGroup = (props: ComponentPropsWithoutRef<typeof CommandPrimitive.Group>) => {
  const { className, ...rest } = props;
  return <CommandPrimitive.Group className={className} {...rest} />;
};

export const BgtCommandItem = (props: ComponentPropsWithoutRef<typeof CommandPrimitive.Item>) => {
  const { className, ...rest } = props;
  return (
    <CommandPrimitive.Item
      className={clsx(
        'group flex gap-3 cursor-default select-none items-center rounded-md p-2 text-[14px] hover:bg-primary-dark',
        className
      )}
      {...rest}
    />
  );
};
interface CommandCreateProps extends ComponentPropsWithoutRef<'div'> {
  onSelect: () => void;
  isLoading: boolean;
}

export const BgtCommandCreate = (props: CommandCreateProps) => {
  const { isLoading, children, onSelect, className, ...rest } = props;

  return (
    <div
      className={clsx(
        'group p-2 flex gap-3 cursor-default select-none items-center rounded-md text-[14px] hover:bg-primary-dark',
        className
      )}
      {...rest}
      role="option"
      onClick={() => onSelect()}
    >
      {isLoading && <Bars className="size-4" />}
      {!isLoading && <PlusCircleIcon className="size-4" />}
      {children}
    </div>
  );
};
