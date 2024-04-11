import {formatDistanceToNowStrict} from 'date-fns';
import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {BgtCard} from '../../../components/BgtLayout/BgtCard';
import {BgtStatistic} from '../../../components/BgtStatistic/BgtStatistic';
import {BgtStatisticsContainer} from '../../../components/BgtStatistics/BgtStatisticsContainer';
import {useGame} from '../../../hooks/useGame';
import {usePlayers} from '../../../hooks/usePlayers';
import {useSettings} from '../../../hooks/useSettings';
import {RoundDecimal} from '../../../utils/roundDecimal';

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
      <BgtCard hide={!statistics.highScore}>
        <BgtStatistic content={statistics.highScore} title={t('statistics.high-score')} />
      </BgtCard>
      <BgtCard hide={!statistics.averageScore}>
        <BgtStatistic content={RoundDecimal(statistics.averageScore)} title={t('statistics.average-score')} />
      </BgtCard>
      <BgtCard hide={!statistics.playCount}>
        <BgtStatistic content={statistics.playCount} title={t('statistics.play-count')} />
      </BgtCard>
      <BgtCard hide={!statistics.lastPlayed}>
        <BgtStatistic content={formatDistanceToNowStrict(new Date(statistics.lastPlayed ?? '01-01-1970'), {addSuffix: true})} title={t('statistics.last-played')} />
      </BgtCard>
      <BgtCard hide={!statistics.totalPlayedTime}>
        <BgtStatistic content={statistics.totalPlayedTime} title={t('statistics.total-play-time')} suffix={t('common.minutes_abbreviation')} />
      </BgtCard>
      <BgtCard hide={!statistics.pricePerPlay}>
        <BgtStatistic content={statistics.pricePerPlay} title={t('statistics.price-per-play')} suffix={settings.currency} />
      </BgtCard>
      <BgtCard hide={!statistics.mostWinsPlayer}>
        <BgtStatistic content={statistics?.mostWinsPlayer?.name} title={t('statistics.most-wint')} player={byId(statistics.mostWinsPlayer?.id)} />
      </BgtCard>
    </BgtStatisticsContainer>
  )
}
