import { ComponentPropsWithoutRef, ReactNode } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

const iconButtonVariants = cva('rounded-lg p-1.5 inline-flex items-center justify-center', {
  variants: {
    intent: {
      normal: 'text-gray-400 hover:text-gray-600',
      danger: 'text-red-600 hover:text-red-800',
      header: 'text-white hover:text-gray-400',
    },
    size: {
      normal: 'h-8 w-8 -mx-1.5 -my-1.5',
      big: 'h-10 w-10 -mx-2 -my-2',
    },
    disabled: {
      true: 'opacity-50 cursor-not-allowed',
      false: 'cursor-pointer',
    },
  },
  defaultVariants: {
    intent: 'normal',
    size: 'normal',
    disabled: false,
  },
});

interface Props
  extends Omit<ComponentPropsWithoutRef<'button'>, 'disabled'>,
    Omit<VariantProps<typeof iconButtonVariants>, 'disabled'> {
  icon: ReactNode;
  onClick: () => void;
  disabled?: boolean;
}

export const BgtIconButton = (props: Props) => {
  const { icon, onClick, disabled, className, intent, size, ...rest } = props;

  const buttonClasses = iconButtonVariants({
    intent,
    size,
    disabled: !!disabled,
    className,
  });

  return (
    <button onClick={onClick} type="button" className={buttonClasses} disabled={disabled} {...rest}>
      {icon}
    </button>
  );
};
