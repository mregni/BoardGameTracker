import { ReactNode } from 'react';

interface Props {
  icon: ReactNode;
  className?: string;
}

export const BgtIcon = (props: Props) => {
  const { icon, className = '' } = props;
  return <div className={className}>{icon}</div>;
};
