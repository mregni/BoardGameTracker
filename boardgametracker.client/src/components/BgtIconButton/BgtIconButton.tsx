import {ReactNode} from 'react';

import {BgtIcon} from '../BgtIcon/BgtIcon';

interface Props {
  icon: ReactNode;
  color?: string;
  hoverColor?: string;
  size?: number;
}

export const BgtIconButton = (props: Props) => {
  const { icon, color = 'text-gray-400', hoverColor = 'text-white', size = 20} = props;
  return (
    <button
      type="button"
      className={`ms-auto -mx-1.5 -my-1.5 ${color} hover:${hoverColor} rounded-lg p-1.5 inline-flex items-center justify-center h-8 w-8`}>
      <BgtIcon
        size={size}
        icon={icon}
      />
    </button>
  )
}