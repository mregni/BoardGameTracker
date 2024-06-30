import { Text } from '@radix-ui/themes';

import { BgtCard } from '../BgtCard/BgtCard';

interface Props {
  title: string;
  content: string | number | null;
  suffix?: string | number | null;
}

export const BgtTextStatistic = (props: Props) => {
  const { title, content, suffix = null } = props;

  if (content === null || content === undefined) return null;

  return (
    <BgtCard>
      <div className="flex flex-col gap-4">
        <Text as="div">{title}</Text>
        <Text as="div" weight="bold">
          {content}
          {suffix && <span className="text-sm">&nbsp;{suffix}</span>}
        </Text>
      </div>
    </BgtCard>
  );
};
