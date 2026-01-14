import { ComponentPropsWithoutRef } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

import CloseIcon from '@/assets/icons/x.svg?react';

const badgeVariants = cva('group px-3 py-1 rounded-full uppercase text-xs flex flex-row gap-2 border', {
  variants: {
    variant: {
      soft: '',
    },
    color: {
      green: 'text-card-value bg-[#34FFAA1F]',
      red: 'text-error-dark y bg-error-dark/20 border-error-dark/30',
      primary: 'text-primary bg-primary/20 border-primary/30',
    },
    interactive: {
      true: 'cursor-pointer',
      false: '',
    },
  },
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
  const { className, children, variant, color, onClick, onClose, ...rest } = props;

  const badgeClasses = badgeVariants({
    variant,
    color,
    interactive: !!onClick,
    className,
  });

  const handleClose = (e: React.MouseEvent<SVGSVGElement>) => {
    e.stopPropagation();
    onClose?.();
  };

  return (
    <div className={badgeClasses} onClick={onClick} {...rest}>
      {children}
      {onClose && <CloseIcon onClick={handleClose} className="cursor-pointer" />}
    </div>
  );
};
