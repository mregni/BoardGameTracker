import { ReactNode } from 'react';
import { Text } from '@radix-ui/themes';

interface Props {
  title: string;
  subTitle?: string;
  right: ReactNode;
}

export const BgtFormRow = (props: Props) => {
  const { title, subTitle, right } = props;
  return (
    <div className="grid grid-cols-1 gap-1 md:gap-0 md:grid-cols-2 justify-center md:divide-x md:divide-blue-500 w-full min-h-16">
      <div className="pr-3 flex flex-col md:items-end">
        <Text size="3" weight="bold">
          {title}
        </Text>
        {subTitle && (
          <Text size="1" className="text-left md:text-right">
            {subTitle}
          </Text>
        )}
      </div>
      <div className="md:pl-3">{right}</div>
    </div>
  );
};
