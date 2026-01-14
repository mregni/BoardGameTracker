import { BgtText } from '../BgtText/BgtText';
import { BgtCard } from '../BgtCard/BgtCard';

interface Props {
  title: string;
  content: string | number | Date | undefined;
  suffix?: string | null;
}

export const BgtFancyTextStatistic = (props: Props) => {
  const { title, content, suffix } = props;

  if (content === null || content === undefined) return null;

  return (
    <BgtCard className="col-span-1 bg-linear-to-br from-primary/20 to-[#ec4899]/10 border border-primary/30 rounded-lg p-4 relative overflow-hidden group hover:border-primary/50 transition-all">
      <div className="absolute top-0 right-0 w-16 h-16 bg-primary/10 rounded-full -mr-8 -mt-8 group-hover:scale-110 transition-transform" />
      <div className="relative">
        <BgtText size="5" color="cyan" weight="bold">
          {content.toString()}
        </BgtText>
        <BgtText color="white" opacity={70} className=" uppercase mb-1">
          {title}
        </BgtText>
        <BgtText color="primary" opacity={60} className="text-[10px]">
          {suffix}
        </BgtText>
      </div>
    </BgtCard>
  );
};
