import { ComponentPropsWithoutRef, MouseEventHandler, ReactNode } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'font-bold rounded uppercase flex flex-row gap-2 justify-center focus-visible:outline-none',
  {
    variants: {
      variant: {
        solid: '',
        outline: 'border',
        soft: 'border',
        inline: 'h-7',
      },
      size: {
        '1': 'py-2 px-2 text-xs',
        '2': 'py-2 px-3',
        '3': 'py-3 px-8',
      },
      color: {
        primary: '',
        error: '',
        cancel: '',
      },
      disabled: {
        true: 'text-gray-500 border border-gray-500',
        false: 'hover:cursor-pointer',
      },
    },
    compoundVariants: [
      // Solid variants
      {
        variant: 'solid',
        color: 'primary',
        disabled: false,
        className: 'bg-primary text-white',
      },
      {
        variant: 'solid',
        color: 'cancel',
        disabled: false,
        className: 'bg-[--gray-3] text-white',
      },
      {
        variant: 'solid',
        color: 'error',
        disabled: false,
        className: 'bg-red-800 text-white',
      },
      // Outline variants
      {
        variant: 'outline',
        color: 'primary',
        disabled: false,
        className: 'border-white text-white hover:bg-gray-800',
      },
      {
        variant: 'outline',
        color: 'error',
        disabled: false,
        className: 'border-red-800 text-red-800',
      },
      {
        variant: 'outline',
        color: 'cancel',
        disabled: false,
        className: 'border-[--gray-6] text-white',
      },
      // Soft variants
      {
        variant: 'soft',
        color: 'primary',
        disabled: false,
        className: 'border-primary bg-primary-dark text-white hover:bg-primary',
      },
      {
        variant: 'soft',
        color: 'error',
        disabled: false,
        className: 'border-red-500 bg-red-950 text-white hover:bg-red-800',
      },
      {
        variant: 'soft',
        color: 'cancel',
        disabled: false,
        className: 'border-[--gray-6] bg-[--gray-3] text-white hover:bg-[--gray-4]',
      },
      // Inline variants
      {
        variant: 'inline',
        color: 'primary',
        disabled: false,
        className: 'text-white hover:text-gray-300',
      },
      {
        variant: 'inline',
        color: 'error',
        disabled: false,
        className: 'text-red-800 hover:text-red-600',
      },
    ],
    defaultVariants: {
      variant: 'solid',
      size: '2',
      color: 'primary',
      disabled: false,
    },
  }
);

interface Props
  extends Omit<ComponentPropsWithoutRef<'button'>, 'color' | 'disabled'>,
    Omit<VariantProps<typeof buttonVariants>, 'disabled'> {
  children: ReactNode | ReactNode[];
  onClick?: MouseEventHandler<HTMLButtonElement> | undefined;
  disabled?: boolean;
}

const BgtButton = (props: Props) => {
  const {
    children,
    onClick,
    variant = 'solid',
    size = '2',
    type = 'button',
    color = 'primary',
    disabled,
    className,
    ...rest
  } = props;

  const buttonClasses = buttonVariants({
    variant,
    size,
    color,
    disabled: !!disabled,
    className,
  });

  return (
    <button className={buttonClasses} type={type} onClick={onClick} disabled={disabled} {...rest}>
      <div className="flex flex-row justify-center gap-1 items-center align-middle">{children}</div>
    </button>
  );
};

export default BgtButton;
