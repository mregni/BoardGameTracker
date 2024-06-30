import { ComponentPropsWithoutRef, MouseEventHandler, ReactNode } from 'react';
import clsx from 'clsx';

interface Props extends ComponentPropsWithoutRef<'button'> {
  children: ReactNode | ReactNode[];
  onClick?: MouseEventHandler<HTMLButtonElement> | undefined;
  variant?: 'solid' | 'outline' | 'soft' | undefined;
  size?: '1' | '2' | '3' | undefined;
  color?: 'primary' | 'error' | undefined;
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

  return (
    <button
      className={clsx(
        'font-bold rounded uppercase',
        !disabled && 'hover:cursor-pointer',
        !disabled && variant === 'solid' && color === 'primary' && 'bg-primary text-white',
        !disabled && variant === 'solid' && color === 'error' && 'bg-red-800 text-white',
        !disabled && variant === 'outline' && color === 'primary' && 'border border-white text-white hover:bg-gray-800',
        !disabled && variant === 'outline' && color === 'error' && 'border border-red-800 text-red-800',
        !disabled &&
          variant === 'soft' &&
          color === 'primary' &&
          'border border-primary bg-primary-dark text-white hover:bg-primary',
        !disabled &&
          variant === 'soft' &&
          color === 'error' &&
          'border border-red-500 bg-red-950 text-white hover:bg-red-800',
        disabled && 'text-gray-500 border border-gray-500',
        size === '1' && 'py-1 px-2 text-xs',
        size === '2' && 'py-2 px-3',
        size === '3' && 'py-3 px-4',
        className
      )}
      type={type}
      onClick={onClick}
      disabled={disabled}
      {...rest}
    >
      <div className="flex flex-row justify-center gap-1 items-center">{children}</div>
    </button>
  );
};

export default BgtButton;
