import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { toRelative } from '@/utils/dateUtils';
import { RecentActivity } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import Trophy from '@/assets/icons/trophy.svg?react';
import Calendar from '@/assets/icons/calendar.svg?react';

interface Props extends React.HTMLAttributes<HTMLDivElement> {
  activities: RecentActivity[];
}

export const RecentActivityCard = (props: Props) => {
  const { activities, className } = props;
  const { t } = useTranslation();

  return (
    <BgtCard title={t('dashboard.recent-activity')} icon={Calendar} className={className}>
      <div className="flex flex-col gap-3">
        {activities.map((activity) => (
          <ActivityItem key={activity.id} activity={activity} />
        ))}
      </div>
    </BgtCard>
  );
};

interface ItemProps {
  activity: RecentActivity;
}

const ActivityItem = ({ activity }: ItemProps) => {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();

  return (
    <BgtCard className="cursor-pointer p-3">
      <div className="flex items-center gap-4">
        <div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
          <BgtAvatar
            onClick={() => navigate({ to: `/games/${activity.gameId}` })}
            image={activity.gameImage}
            title={activity.gameTitle}
            color={StringToHsl(activity.gameTitle)}
            size="large"
          />
        </div>
        <div className="flex-1">
          <BgtText color="white">
            {activity.winnerName && (
              <>
                <span className="font-bold" onClick={() => navigate({ to: `/players/${activity.winnerId}` })}>
                  {activity.winnerName}
                </span>{' '}
                <span className="lowercase">{t('common.won')}</span>{' '}
              </>
            )}
            <span className="text-primary" onClick={() => navigate({ to: `/games/${activity.gameId}` })}>
              {activity.gameTitle}
            </span>
          </BgtText>
          <BgtText color="white" size="2" opacity={50}>
            {t('common.player', { count: activity.playerCount })} • {activity.durationInMinutes}
            {t('common.minutes-abbreviation')} • {toRelative(activity.start, i18n.language)}
          </BgtText>
        </div>
        <div className="font-bold">
          <Trophy className="size-5 text-yellow-500" />
        </div>
      </div>
    </BgtCard>
  );
};
