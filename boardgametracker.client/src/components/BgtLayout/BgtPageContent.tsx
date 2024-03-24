import {ReactNode} from 'react';

interface Props {
  children: ReactNode;
  className?: string;
}

export const BgtPageContent = (props: Props) => {
  const { children, className = '' } = props;

  return (
    <div className={className}>
      {children}
    </div>
  )
};
