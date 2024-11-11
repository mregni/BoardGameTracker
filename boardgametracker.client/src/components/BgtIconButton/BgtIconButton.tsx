import { ReactNode } from 'react';
import { cx } from 'class-variance-authority';

import { BgtIcon } from '../BgtIcon/BgtIcon';

interface Props {
  icon: ReactNode;
  onClick: () => void;
  size?: number;
  type?: 'normal' | 'danger';
  disabled?: boolean;
}

export const BgtIconButton = (props: Props) => {
  const { icon, onClick, size = 20, type = 'normal', disabled } = props;
  return (
    <button
      onClick={onClick}
      type="button"
      className={cx(
        '-mx-1.5 -my-1.5 rounded-lg p-1.5 inline-flex items-center justify-center h-8 w-8',
        type === 'normal' && 'text-gray-400 hover:text-gray-600',
        type === 'danger' && 'text-red-600 hover:text-red-800'
      )}
      disabled={disabled}
    >
      <BgtIcon size={size} icon={icon} />
    </button>
  );
};
