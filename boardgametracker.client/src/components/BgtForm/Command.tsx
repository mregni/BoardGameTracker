import clsx from 'clsx';
import { Command as CommandPrimitive } from 'cmdk';
import { ComponentPropsWithoutRef } from 'react';
import { Bars } from 'react-loading-icons';

import { MagnifyingGlassIcon, PlusCircleIcon } from '@heroicons/react/24/outline';

const Command = ({ className, ...props }: ComponentPropsWithoutRef<typeof CommandPrimitive>) => (
  <CommandPrimitive
    className={clsx(
      'mx-auto w-full transform divide-gray-100 overflow-hidden rounded-md bg-[--gray-2] shadow-md ring-1 ring-black ring-opacity-5 transition-all',
      className
    )}
    {...props}
  />
);

const CommandInput = ({ className, ...props }: ComponentPropsWithoutRef<typeof CommandPrimitive.Input>) => (
  // eslint-disable-next-line react/no-unknown-property
  <div className="relative" cmdk-input-wrapper="">
    <MagnifyingGlassIcon className="pointer-events-none absolute left-3 top-2 mr-2 size-4 shrink-0 opacity-50" />
    <CommandPrimitive.Input
      className={clsx('h-8 w-full border-0 bg-transparent pl-10 pr-4 focus:ring-0 focus-visible:outline-0 sm:text-sm', className)}
      {...props}
    />
  </div>
);

const CommandGroup = ({ className, ...props }: ComponentPropsWithoutRef<typeof CommandPrimitive.Group>) => (
  <CommandPrimitive.Group className={clsx(className)} {...props} />
);

const CommandItem = ({ className, ...props }: ComponentPropsWithoutRef<typeof CommandPrimitive.Item>) => (
  <CommandPrimitive.Item
    className={clsx('group flex gap-3 cursor-default select-none items-center rounded-md px-3 py-1 text-sm hover:bg-orange-600', className)}
    {...props}
  />
);

interface CommandCreateProps extends ComponentPropsWithoutRef<'div'> {
  onSelect: () => void;
  isLoading: boolean;
}

const CommandCreate = ({ isLoading, children, onSelect, className, ...rest }: CommandCreateProps) => (
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

export { Command, CommandInput, CommandCreate, CommandGroup, CommandItem };
