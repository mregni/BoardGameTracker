import { Bars } from 'react-loading-icons';
import { ComponentPropsWithoutRef } from 'react';
import { Command as CommandPrimitive } from 'cmdk';
import clsx from 'clsx';
import { MagnifyingGlassIcon, PlusCircleIcon } from '@heroicons/react/24/outline';

export const BgtCommand = (props: ComponentPropsWithoutRef<typeof CommandPrimitive>) => {
  const { className, ...rest } = props;

  return (
    <CommandPrimitive
      className={clsx(
        'mx-auto w-full transform divide-gray-100 overflow-hidden rounded-md bg-[--gray-2] shadow-md ring-1 ring-black ring-opacity-5 transition-all',
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
    <div className="relative" cmdk-input-wrapper="">
      <MagnifyingGlassIcon className="pointer-events-none absolute left-3 top-2 mr-2 size-4 shrink-0 opacity-50" />
      <CommandPrimitive.Input
        className={clsx('h-8 w-full border-0 bg-transparent pl-10 pr-4 focus:ring-0 focus-visible:outline-0 sm:text-sm', className)}
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
      className={clsx('group flex gap-3 cursor-default select-none items-center rounded-md px-3 py-1 text-sm hover:bg-orange-600', className)}
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
      className={clsx('group flex gap-3 cursor-default select-none items-center rounded-md px-3 py-1 text-sm hover:bg-orange-600', className)}
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
