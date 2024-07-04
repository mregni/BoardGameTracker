import { ComponentPropsWithoutRef, ReactNode } from 'react';
import { Text } from '@radix-ui/themes';

interface Props extends Omit<ComponentPropsWithoutRef<'div'>, 'color'> {
  children: ReactNode | ReactNode[];
  size?: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' | undefined;
  weight?: 'bold' | 'light' | 'regular' | 'medium' | undefined;
  color?: 'amber' | 'orange' | 'red' | 'purple' | 'blue' | 'green' | undefined;
}

export const BgtText = (props: Props) => {
  const { children, size = '3', className, weight = undefined, ...rest } = props;
  return (
    <Text as="div" size={size} className={className} weight={weight} {...rest}>
      {children}
    </Text>
  );
};
