import { ComponentPropsWithoutRef, ElementRef, forwardRef } from 'react';
import { cx, cva } from 'class-variance-authority';
import * as PopoverPrimitive from '@radix-ui/react-popover';

export const BgtPopover = PopoverPrimitive.Root;

export const BgtPopoverTrigger = PopoverPrimitive.Trigger;

export const BgtPopoverAnchor = PopoverPrimitive.Anchor;

export const BgtPopoverContent = forwardRef<
  ElementRef<typeof PopoverPrimitive.Content>,
  ComponentPropsWithoutRef<typeof PopoverPrimitive.Content>
>(({ className, align = 'end', sideOffset = 4, ...props }, ref) => (
  <PopoverPrimitive.Portal>
    <PopoverPrimitive.Content
      ref={ref}
      align={align}
      sideOffset={sideOffset}
      className={cx(
        'w-[--radix-popover-trigger-width]',
        'z-50 rounded-md border-gray-600 border bg-popover text-popover-foreground shadow-md outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2',
        className
      )}
      {...props}
    />
  </PopoverPrimitive.Portal>
));

BgtPopoverContent.displayName = PopoverPrimitive.Content.displayName;
