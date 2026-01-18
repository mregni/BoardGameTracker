import { ComponentPropsWithoutRef } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'font-bold rounded-sm uppercase inline-flex flex-row gap-1 justify-center items-center align-middle focus-visible:outline-hidden',
  {
    variants: {
      variant: {
        primary: 'bg-primary/60  hover:bg-primary/50 text-white',
        cancel: 'hover:bg-cancel/10 text-cancel border border-cancel/60',
        error: 'bg-error hover:bg-error/80',
        text: 'text-primary hover:text-primary/70',
      },
      size: {
        '1': 'py-2 px-2 text-xs',
        '2': 'py-2 px-3',
        '3': 'py-3 px-8',
      },
      disabled: {
        true: 'text-gray-500 border border-gray-500',
        false: 'hover:cursor-pointer',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: '2',
      disabled: false,
    },
  }
);

interface Props
  extends Omit<ComponentPropsWithoutRef<'button'>, 'color'>,
    Omit<VariantProps<typeof buttonVariants>, 'disabled'> {}

const BgtButton = (props: Props) => {
  const { children, variant, size, type = 'button', disabled, className, ...rest } = props;

  const buttonClasses = buttonVariants({
    variant,
    size,
    disabled: !!disabled,
    className,
  });

  return (
    <button className={buttonClasses} type={type} disabled={disabled} {...rest}>
      {children}
    </button>
  );
};

export default BgtButton;
export { BgtButton };
