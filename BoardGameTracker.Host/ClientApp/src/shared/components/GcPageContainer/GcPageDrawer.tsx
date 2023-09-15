import {ReactNode} from 'react';

interface Props {
  children: ReactNode | ReactNode[];
}

export const GcPageDrawer = (props: Props) => {
  const { children } = props;
  return (<>{children}</>);
};