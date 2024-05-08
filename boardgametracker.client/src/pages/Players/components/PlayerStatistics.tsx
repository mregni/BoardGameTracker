import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';

import { BgtCard } from '../../../components/BgtCard/BgtCard';
import { BgtStatistic } from '../../../components/BgtStatistic/BgtStatistic';
import { BgtStatisticsContainer } from '../../../components/BgtStatistics/BgtStatisticsContainer';
import { useGames } from '../../../hooks/useGames';
import { usePlayer } from '../../../hooks/usePlayer';
import { useSettings } from '../../../hooks/useSettings';
import { GetPercentage } from '../../../utils/numberUtils';

export const PlayerStatistics = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { player, statistics } = usePlayer(id);
  const { settings } = useSettings();
  const { byId } = useGames();

  if (player === undefined || statistics === undefined || settings === undefined) return null;

  return (
    <BgtStatisticsContainer>
      <BgtCard hide={!statistics.playCount}>
        <BgtStatistic content={statistics.playCount} title={t('statistics.play-count')} />
      </BgtCard>
      <BgtCard hide={!statistics.winCount}>
        <BgtStatistic
          content={statistics.winCount}
          title={t('statistics.win-count')}
          suffix={`(${GetPercentage(statistics.winCount, statistics.playCount)}%)`}
        />
      </BgtCard>
      <BgtCard hide={!statistics.totalPlayedTime}>
        <BgtStatistic content={statistics.totalPlayedTime} title={t('statistics.total-play-time')} suffix={t('common.minutes_abbreviation')} />
      </BgtCard>
      <BgtCard hide={!statistics.distinctGameCount}>
        <BgtStatistic content={statistics.distinctGameCount} title={t('statistics.distinct-game-count')} />
      </BgtCard>
      <BgtCard hide={!statistics.favoriteColor}>
        <BgtStatistic content={statistics.favoriteColor} title={t('statistics.favorite-color')} />
      </BgtCard>
      <BgtCard hide={!statistics.bestGameId}>
        <BgtStatistic content={byId(statistics.bestGameId)?.title} game={byId(statistics.bestGameId)} title={t('statistics.best-game')} />
      </BgtCard>
    </BgtStatisticsContainer>
  );
};
