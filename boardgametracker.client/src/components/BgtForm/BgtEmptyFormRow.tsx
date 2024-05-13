import { ReactNode } from 'react';

interface Props {
  right: ReactNode;
}

export const BgtEmptyFormRow = (props: Props) => {
  const { right } = props;
  return (
    <div className="grid grid-cols-1 gap-1 md:gap-0 md:grid-cols-2 justify-center w-full min-h-16">
      <div className="md:col-start-2 md:pl-3">{right}</div>
    </div>
  );
};
