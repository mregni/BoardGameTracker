import { useTranslation } from 'react-i18next';

import { GetPercentage } from '@/utils/numberUtils';
import { formatMinutesToDuration } from '@/utils/dateUtils';
import { Settings } from '@/models';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';

interface PlayerStatistics {
  playCount: number;
  totalPlayedTime: number | null;
  winCount: number;
  distinctGameCount: number;
}

interface Props {
  statistics: PlayerStatistics;
  settings?: Settings;
}

export const PlayerStatisticsGrid = (props: Props) => {
  const { statistics, settings } = props;
  const { t } = useTranslation();

  const totalPlayedTime = formatMinutesToDuration(
    statistics.totalPlayedTime,
    ['months', 'weeks', 'days', 'hours', 'minutes'],
    settings?.uiLanguage
  );

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-3 xl:gap-6">
      <BgtTextStatistic content={statistics.playCount} title={t('statistics.play-count')} />
      <BgtTextStatistic content={totalPlayedTime} title={t('statistics.total-play-time')} />
      <BgtTextStatistic content={statistics.winCount} title={t('statistics.win-count')} />
      <BgtTextStatistic
        content={GetPercentage(statistics.winCount, statistics.playCount)}
        title={t('statistics.win-percentage')}
        suffix={'%'}
      />
      <BgtTextStatistic content={statistics.distinctGameCount} title={t('statistics.distinct-game-count')} />
    </div>
  );
};
