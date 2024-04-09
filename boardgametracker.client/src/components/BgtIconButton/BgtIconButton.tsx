import {ReactNode} from 'react';

import {BgtIcon} from '../BgtIcon/BgtIcon';

interface Props {
  icon: ReactNode;
  onClick: () => void;
  color?: string;
  hoverColor?: string;
  size?: number;
}

export const BgtIconButton = (props: Props) => {
  const { icon, onClick, color = 'text-white', hoverColor = 'text-gray-800', size = 20 } = props;
  return (
    <button
      onClick={onClick}
      type="button"
      className={`-mx-1.5 -my-1.5 ${color} hover:${hoverColor} rounded-lg p-1.5 inline-flex items-center justify-center h-8 w-8`}>
      <BgtIcon
        size={size}
        icon={icon}
      />
    </button>
  )
}