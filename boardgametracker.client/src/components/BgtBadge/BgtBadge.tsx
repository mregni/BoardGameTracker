import { ComponentPropsWithoutRef } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

import CloseIcon from '@/assets/icons/x.svg?react';

const badgeVariants = cva('group px-3 py-2 rounded-md uppercase text-xs flex flex-row gap-2', {
  variants: {
    variant: {
      soft: '',
    },
    color: {
      green: 'text-mint-green bg-[#34FFAA1F]',
      red: '',
    },
    interactive: {
      true: 'cursor-pointer hover:bg-[#34ffaa4a] transition-colors duration-200',
      false: '',
    },
  },
  compoundVariants: [
    {
      variant: 'soft',
      color: 'green',
      className: 'group-hover:bg-[#34ffaa4a]',
    },
  ],
  defaultVariants: {
    variant: 'soft',
    color: 'green',
    interactive: false,
  },
});

interface Props extends Omit<ComponentPropsWithoutRef<'div'>, 'color'>, VariantProps<typeof badgeVariants> {
  onClose?: () => void;
}

export const BgtBadge = (props: Props) => {
  const { className, children, variant, color, onClick, onClose } = props;

  const badgeClasses = badgeVariants({
    variant,
    color,
    interactive: !!onClick,
    className,
  });

  return (
    <div className={badgeClasses} onClick={onClick}>
      {children}
      {onClose && <CloseIcon onClick={onClose} className="size-4 cursor-pointer" />}
    </div>
  );
};
