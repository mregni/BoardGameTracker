import clsx from 'clsx';
import { ComponentPropsWithoutRef } from 'react';

import { Text } from '@radix-ui/themes';

interface Props extends ComponentPropsWithoutRef<'div'> {
  contentStyle?: string;
  transparant?: boolean;
  title?: string;
  hide?: boolean;
  noTitleSpacing?: boolean;
  noPadding?: boolean;
}

export const BgtCard = (props: Props) => {
  const { children, className, contentStyle = '', transparant = false, title, hide = false, noPadding = false } = props;

  return (
    <div className={clsx('flex flex-col gap-1', className, hide && 'hidden')}>
      <div className={clsx(contentStyle, !transparant && 'border-blue-500 border bg-gray-900', !noPadding && 'p-3')}>
        {title && (
          <Text size="3" align="center" weight="bold">
            {title}
          </Text>
        )}
        {children}
      </div>
    </div>
  );
};
