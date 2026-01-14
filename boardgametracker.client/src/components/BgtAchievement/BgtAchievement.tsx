import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';
import * as Tooltip from '@radix-ui/react-tooltip';

import { BgtCard } from '../BgtCard/BgtCard';

import { Badge } from '@/models';
import Award from '@/assets/icons/award.svg?react';

interface Props {
  badge: Badge;
  earned?: boolean;
}

export const BgtAchievement = (props: Props) => {
  const { badge, earned = true } = props;
  const { t } = useTranslation();

  return (
    <BgtCard className={cx('col-span-1 p-3', !earned ? 'opacity-40 grayscale' : '')}>
      <div className="flex flex-row gap-3">
        <img src={`/images/badges/${badge.image}`} alt="Badge image" className="h-10 aspect-square" />
        <div className="flex flex-row justify-between w-full items-center">
          <div className="flex flex-col">
            <div className="font-bold">{t(`badges.${badge.titleKey}`)}</div>
            <div className="text-xs line-clamp-1">{t(`badges.${badge.descriptionKey}`)}</div>
          </div>
          {earned && (
            <div className="text-green-400">
              <Award />
            </div>
          )}
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
            className="select-none rounded-sm bg-card-black border-card-border border-2 border-solid px-[15px] py-2.5 text-[15px] leading-none will-change-[transform,opacity] data-[state=delayed-open]:data-[side=bottom]:animate-slide-up-and-fade data-[state=delayed-open]:data-[side=left]:animate-slide-right-and-fade data-[state=delayed-open]:data-[side=right]:animate-slide-left-and-fade data-[state=delayed-open]:data-[side=top]:animate-slide-down-and-fade"
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
