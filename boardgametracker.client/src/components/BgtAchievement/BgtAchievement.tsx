import { useTranslation } from 'react-i18next';
import * as Tooltip from '@radix-ui/react-tooltip';

import { BgtCard } from '../BgtCard/BgtCard';

import { Badge } from '@/models';

interface Props {
  badge: Badge;
}

export const BgtAchievement = (props: Props) => {
  const { badge } = props;
  const { t } = useTranslation();

  return (
    <BgtCard className="col-span-1 p-3">
      <div className="flex flex-row gap-3">
        <img src={`/images/badges/${badge.image}`} alt="Badge image" className="h-10 aspect-square" />
        <div className="flex flex-col">
          <div className="font-bold">{t(`badges.${badge.titleKey}`)}</div>
          <div className="text-xs line-clamp-1">{t(`badges.${badge.descriptionKey}`)}</div>
        </div>
      </div>
    </BgtCard>
  );
};

export const BgtAchievementIcon = (props: Props) => {
  const { badge } = props;
  const { t } = useTranslation();

  return (
    <Tooltip.Provider>
      <Tooltip.Root>
        <Tooltip.Trigger asChild>
          <img src={`/images/badges/${badge.image}`} alt="Badge image" className="h-10 aspect-square" />
        </Tooltip.Trigger>
        <Tooltip.Portal>
          <Tooltip.Content
            className="select-none rounded bg-card-black border-card-border border-2 border-solid px-[15px] py-2.5 text-[15px] leading-none will-change-[transform,opacity] data-[state=delayed-open]:data-[side=bottom]:animate-slideUpAndFade data-[state=delayed-open]:data-[side=left]:animate-slideRightAndFade data-[state=delayed-open]:data-[side=right]:animate-slideLeftAndFade data-[state=delayed-open]:data-[side=top]:animate-slideDownAndFade"
            sideOffset={5}
          >
            <div className="flex flex-col justify-center">
              <div className="font-bold">{t(`badges.${badge.titleKey}`)}</div>
              <div className="text-xs">{t(`badges.${badge.descriptionKey}`)}</div>
            </div>
          </Tooltip.Content>
        </Tooltip.Portal>
      </Tooltip.Root>
    </Tooltip.Provider>
  );
};
