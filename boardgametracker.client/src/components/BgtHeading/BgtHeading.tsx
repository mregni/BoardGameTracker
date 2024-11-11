import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';
import { Heading } from '@radix-ui/themes';

interface Props extends ComponentPropsWithoutRef<'div'> {
  size?: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' | undefined;
}

export const BgtHeading = (props: Props) => {
  const { children, className, size = '8' } = props;

  return (
    <Heading as="h3" size={size} className={cx('uppercase line-clamp-1 pr-2', className)}>
      {children}
    </Heading>
  );
};
