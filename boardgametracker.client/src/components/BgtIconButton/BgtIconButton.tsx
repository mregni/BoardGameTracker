import { ComponentPropsWithoutRef, ReactNode } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

const iconButtonVariants = cva('rounded-lg inline-flex items-center justify-center p-2', {
  variants: {
    intent: {
      primary: 'text-primary bg-primary/20 hover:bg-primary/40 transition-colors',
      danger: 'text-error bg-error/20 hover:bg-error/40 transition-colors',
      header: 'text-white hover:text-gray-400 transition-colors',
    },
    size: {
      '1': 'h-8 w-8',
      '2': 'h-10 w-10',
    },
    disabled: {
      true: 'opacity-50 cursor-not-allowed',
      false: 'cursor-pointer',
    },
  },
  defaultVariants: {
    intent: 'primary',
    size: '1',
    disabled: false,
  },
});

interface Props extends ComponentPropsWithoutRef<'button'>, Omit<VariantProps<typeof iconButtonVariants>, 'disabled'> {
  icon: ReactNode;
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
