import { ComponentPropsWithoutRef } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

const textVariants = cva('', {
  variants: {
    color: {
      amber: 'text-amber-500',
      orange: 'text-orange-500',
      red: 'text-red-500',
      purple: 'text-purple-500',
      blue: 'text-blue-500',
      cyan: 'text-cyan-500',
      green: 'text-green-500',
      white: 'text-white',
      primary: 'text-primary',
    },
    opacity: {
      100: '',
      90: 'opacity-90',
      80: 'opacity-80',
      70: 'opacity-70',
      60: 'opacity-60',
      50: 'opacity-50',
      40: 'opacity-40',
      30: 'opacity-30',
      20: 'opacity-20',
      10: 'opacity-10',
    },
  },
  defaultVariants: {
    opacity: 100,
  },
});

interface Props extends Omit<ComponentPropsWithoutRef<'div'>, 'color'>, VariantProps<typeof textVariants> {
  size?: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9';
  weight?: 'bold' | 'light' | 'regular' | 'medium';
}

export const BgtText = (props: Props) => {
  const { children, size = '3', className, weight, color = 'white', opacity, ...rest } = props;

  const textClasses = textVariants({
    color,
    opacity,
    className,
  });

  return (
    <Text as="div" size={size} className={textClasses} weight={weight} {...rest}>
      {children}
    </Text>
  );
};
