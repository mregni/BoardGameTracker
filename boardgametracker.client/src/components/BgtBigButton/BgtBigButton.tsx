import clsx from 'clsx';

import { Button, Text } from '@radix-ui/themes';

interface Props {
  title: string;
  subText: string;
  onClick: () => void;
  disabled?: boolean;
}

const BgtBigButton = (props: Props) => {
  const { title, subText, onClick, disabled = false } = props;

  return (
    <Button disabled={disabled} variant="surface" onClick={onClick} className={clsx('!h-28', !disabled && 'hover:cursor-pointer')}>
      <div className="flex flex-col p-3 gap-3">
        <Text align="center" as="p" size="4">
          {title}
        </Text>
        <Text align="center" as="p" size="1">
          {subText}
        </Text>
      </div>
    </Button>
  );
};

export default BgtBigButton;
