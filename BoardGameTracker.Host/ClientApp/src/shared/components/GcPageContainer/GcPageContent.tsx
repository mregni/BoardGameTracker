import {ReactNode} from 'react';

import {GcNoDataLoader} from '../GcNoDataLoader';

interface Props {
  children: ReactNode;
  isLoading: boolean;
  hasData?: boolean;
}

export const GcPageContent = (props: Props) => {
  const { isLoading, children, hasData = true } = props;
  return (
    <GcNoDataLoader isLoading={isLoading} hasData={hasData}>
      {children}
    </GcNoDataLoader>
  )
};
