import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { formatDistanceToNowStrict } from 'date-fns';

import { RoundDecimal } from '../../../utils/numberUtils';
import { useSettings } from '../../../hooks/useSettings';
import { usePlayers } from '../../../hooks/usePlayers';
import { useGame } from '../../../hooks/useGame';
import { BgtStatisticsContainer } from '../../../components/BgtStatistics/BgtStatisticsContainer';
import { BgtStatistic } from '../../../components/BgtStatistic/BgtStatistic';

export const GameStatistics = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { game } = useGame(id);
  const { statistics } = useGame(id);
  const { settings } = useSettings();
  const { byId } = usePlayers();

  if (game === undefined || statistics === undefined || settings === undefined) return null;

  return (
    <BgtStatisticsContainer>
      <BgtStatistic content={statistics.highScore} title={t('statistics.high-score')} />
      <BgtStatistic content={RoundDecimal(statistics.averageScore)} title={t('statistics.average-score')} />
      <BgtStatistic content={statistics.playCount} title={t('statistics.play-count')} />
      <BgtStatistic
        content={formatDistanceToNowStrict(new Date(statistics.lastPlayed ?? '01-01-1970'), { addSuffix: true })}
        title={t('statistics.last-played')}
      />
      <BgtStatistic
        content={statistics.totalPlayedTime}
        title={t('statistics.total-play-time')}
        suffix={t('common.minutes_abbreviation')}
      />
      <BgtStatistic
        content={statistics.pricePerPlay}
        title={t('statistics.price-per-play')}
        suffix={settings.currency}
      />
      <BgtStatistic
        content={statistics?.mostWinsPlayer?.name}
        title={t('statistics.most-wins')}
        player={byId(statistics.mostWinsPlayer?.id)}
      />
    </BgtStatisticsContainer>
  );
};
