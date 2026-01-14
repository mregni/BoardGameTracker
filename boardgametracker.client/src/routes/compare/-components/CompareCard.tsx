import { ReactNode, memo } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';

import { BgtText } from '@/components/BgtText/BgtText';
import Crown from '@/assets/icons/crown.svg?react';

const cardVariants = cva('border rounded-xl p-5 backdrop-blur-sm relative transition-all duration-200', {
  variants: {
    color: {
      red: 'bg-error/10 border-error/30',
      blue: 'bg-blue-600/10 border-blue-600/30',
    },
    isWinner: {
      true: '',
    },
  },
  compoundVariants: [
    {
      color: 'red',
      isWinner: true,
      class: 'ring-4 ring-error/20 ',
    },
    {
      color: 'blue',
      isWinner: true,
      class: 'ring-4 ring-blue-600/20 ',
    },
  ],
});

const iconVariants = cva('', {
  variants: {
    color: {
      red: 'text-error',
      blue: 'text-blue-600',
    },
  },
});

const labelVariants = cva('text-xs uppercase tracking-wider mb-1', {
  variants: {
    color: {
      red: 'text-error/80',
      blue: 'text-blue-600/90',
    },
  },
});

interface CompareCardProps<T> extends VariantProps<typeof cardVariants> {
  value: T;
  label: string;
  icon?: ReactNode;
  color: 'red' | 'blue';
}

const CompareCardComponent = <T,>({ value, label, icon, color, isWinner }: CompareCardProps<T>) => {
  return (
    <div className={cardVariants({ color, isWinner })}>
      <div className="flex items-center gap-4">
        {icon && <div className={iconVariants({ color })}>{icon}</div>}
        <div className="flex-1">
          <div className={labelVariants({ color })}>{label}</div>
          <BgtText color="cyan" size="6">
            {String(value)}
          </BgtText>
        </div>
        {isWinner && <Crown className="text-yellow-400 fill-yellow-400" />}
      </div>
    </div>
  );
};

export const CompareCard = memo(CompareCardComponent) as typeof CompareCardComponent;
