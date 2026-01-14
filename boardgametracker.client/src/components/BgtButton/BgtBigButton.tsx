import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

import BgtButton from './BgtButton';

interface Props {
  title: string;
  subText: string;
  onClick: () => void;
  disabled?: boolean;
}

const BgtBigButton = (props: Props) => {
  const { title, subText, onClick, disabled = false } = props;

  return (
    <BgtButton
      disabled={disabled}
      variant="primary"
      onClick={onClick}
      className={cx('h-28!', !disabled && 'hover:cursor-pointer')}
    >
      <div className="flex flex-col p-3 gap-3">
        <Text align="center" as="p" size="4">
          {title}
        </Text>
        <Text align="center" as="p" size="1" className="lowercase first-letter:uppercase">
          {subText}
        </Text>
      </div>
    </BgtButton>
  );
};

export default BgtBigButton;
