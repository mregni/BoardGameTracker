import { ReactNode } from 'react';
import { Heading } from '@radix-ui/themes';

interface Props {
  children: ReactNode | ReactNode[];
}
export const BgtHeading = (props: Props) => {
  const { children } = props;

  return (
    <Heading as="h3" size="8" className="line-clamp-1 pr-2 !font-chakra-petch">
      {children}
    </Heading>
  );
};
