import { ComponentPropsWithoutRef, ReactNode } from 'react';
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
      green: 'text-green-500',
    },
  },
  defaultVariants: {
    color: undefined,
  },
});

interface Props extends Omit<ComponentPropsWithoutRef<'div'>, 'color'>, VariantProps<typeof textVariants> {
  children: ReactNode | ReactNode[];
  size?: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' | undefined;
  weight?: 'bold' | 'light' | 'regular' | 'medium' | undefined;
}

export const BgtText = (props: Props) => {
  const { children, size = '3', className, weight = undefined, color, ...rest } = props;

  const textClasses = textVariants({
    color,
    className,
  });

  return (
    <Text as="div" size={size} className={textClasses} weight={weight} {...rest}>
      {children}
    </Text>
  );
};
