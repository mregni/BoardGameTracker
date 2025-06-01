import { ReactNode } from 'react';
import { cx, cva, VariantProps } from 'class-variance-authority';

const variants = cva('rounded-lg p-1.5 inline-flex items-center justify-center', {
  variants: {
    intent: {
      normal: 'text-gray-400 hover:text-gray-600',
      danger: 'text-red-600 hover:text-red-800',
      header: 'text-white hover:text-gray-400'
    },
    size: {
      normal: 'h-8 w-8 -mx-1.5 -my-1.5',
      big: 'h-10 w-10 -mx-2 -my-2',
    },
  },
  defaultVariants: {
    intent: 'normal',
    size: 'normal',
  },
});

interface Props extends VariantProps<typeof variants> {
  icon: ReactNode;
  onClick: () => void;
  disabled?: boolean;
  className?: string;
}

export const BgtIconButton = (props: Props) => {
  const { icon, onClick, disabled, className, intent, size } = props;
  return (
    <button onClick={onClick} type="button" className={cx(className, variants({ intent, size }))} disabled={disabled}>
      {icon}
    </button>
  );
};
