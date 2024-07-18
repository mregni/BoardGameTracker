import { Text } from '@radix-ui/themes';

import { BgtText } from '../BgtText/BgtText';
import { BgtCard } from '../BgtCard/BgtCard';

interface Props {
  title: string;
  content: string | number | null;
  suffix?: string | number | null;
  prefix?: string | number | null;
}

export const BgtTextStatistic = (props: Props) => {
  const { title, content, suffix = null, prefix = null } = props;

  if (content === null || content === undefined) return null;

  return (
    <BgtCard className="col-span-1">
      <div className="flex flex-col items-center">
        <BgtText weight="bold" className="text-mint-green text-lg md:text-3xl">
          {prefix && <span>{prefix}&nbsp;</span>}
          {content}
          {suffix && <span className="text-sm lowercase">&nbsp;{suffix}</span>}
        </BgtText>
        <BgtText className="uppercase text-[10px] md:text-base">{title}</BgtText>
      </div>
    </BgtCard>
  );
};
