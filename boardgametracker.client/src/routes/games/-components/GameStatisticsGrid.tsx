import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';

import { RoundDecimal } from '@/utils/numberUtils';
import { formatMinutesToDuration, toRelative } from '@/utils/dateUtils';
import { getSettings } from '@/services/queries/settings';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';

interface GameStats {
  totalPlayedTime: number | null;
  pricePerPlay: number | null;
  highScore: number | null;
  averageScore: number | null;
  averagePlayTime: number | null;
  lastPlayed: string | null;
}

interface Props {
  gameStats: GameStats;
  expansionCount: number;
  currency: string;
  uiLanguage: string;
  dateFormat: string;
}

export const GameStatisticsGrid = (props: Props) => {
  const { gameStats, expansionCount, currency } = props;
  const { t } = useTranslation();
  const { data: settings } = useQuery(getSettings());

  const lastPlayedRelative =
    gameStats.lastPlayed && settings?.uiLanguage
      ? toRelative(gameStats.lastPlayed, settings.uiLanguage, { addSuffix: false })
      : null;

  const totalPlayedTime = formatMinutesToDuration(
    gameStats.totalPlayedTime,
    ['weeks', 'days', 'hours', 'minutes'],
    settings?.uiLanguage
  );
  const averagePlayTime = formatMinutesToDuration(
    gameStats.averagePlayTime,
    ['hours', 'minutes', 'seconds'],
    settings?.uiLanguage
  );

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 xl:gap-6">
      <BgtTextStatistic content={totalPlayedTime} title={t('statistics.total-play-time')} />
      <BgtTextStatistic content={gameStats.pricePerPlay} title={t('statistics.price-per-play')} prefix={currency} />
      <BgtTextStatistic content={RoundDecimal(gameStats.highScore)} title={t('statistics.high-score')} />
      <BgtTextStatistic content={RoundDecimal(gameStats.averageScore)} title={t('statistics.average-score')} />
      <BgtTextStatistic content={averagePlayTime} title={t('statistics.average-playtime')} />
      <BgtTextStatistic content={lastPlayedRelative} title={t('statistics.last-played')} />
      <BgtTextStatistic content={expansionCount} title={t('statistics.expansion-count')} />
    </div>
  );
};

//locale: getDateFnsLocale(settings?.uiLanguage ?? ''),
