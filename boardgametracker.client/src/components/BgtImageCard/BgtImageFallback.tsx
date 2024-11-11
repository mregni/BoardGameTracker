import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

import { StringToHsl } from '@/utils/stringUtils';

interface Props {
  display: boolean;
  title: string;
  roundBottom?: boolean;
  fullWidth?: boolean;
}

export const BgtImageFallback = (props: Props) => {
  const { display, title, roundBottom = true, fullWidth = false } = props;

  if (!display) return null;

  return (
    <div
      style={{ backgroundColor: StringToHsl(title) }}
      className={cx(
        'shadow-black drop-shadow-md flex justify-center items-center h-full aspect-square',
        !roundBottom && 'rounded-t-sm',
        roundBottom && 'rounded-sm',
        fullWidth && 'w-full',
        !fullWidth && 'w-24 md:w-56'
      )}
    >
      <Text size="8" weight="bold">
        {title[0]}
      </Text>
    </div>
  );
};
