import {ReactNode} from 'react';

interface Props {
  size?: number;
  icon: ReactNode;
  className?: string;
}

export const BgtIcon = (props: Props) => {
  const { size = 20, icon, className = '' } = props;
  return (
    <svg 
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      width={size}
      height={size}>
      {icon}
    </svg>
  )
}