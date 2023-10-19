import {addMinutes, formatDuration, intervalToDuration} from 'date-fns';
import {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GcStatisticsRow, StatisticsCard} from '../../../components/GcStatistics';
import {limitStringLength} from '../../../utils';
import {GamesContext} from '../../Games/context';
import {PlayerDetailContext} from '../context/PlayerDetailState';

export const PlayerStatistics = () => {
  const { statistics } = useContext(PlayerDetailContext);
  const { games } = useContext(GamesContext);
  const {t} = useTranslation();
  const [cards, setCards] = useState<StatisticsCard[]>([]);

  useEffect(() => {
    if (statistics !== null) {
      const game = games.find(x => x.id == statistics.bestGameId);

      setCards([
        { title: t('statistics.play-count'), value: statistics.playCount },
        { title: t('statistics.total-wins'), value: statistics.winCount },
        { title: t('statistics.games-played'), value: statistics.distinctGameCount},
        { title: t('statistics.best-game'), value: limitStringLength(game?.title) ?? null },
        {
          title: t('statistics.total-play-time'), value: formatDuration(
            intervalToDuration({
              start: new Date(2000, 1, 1, 0, 0, 0),
              end: addMinutes(new Date(2000, 1, 1, 0, 0, 0), statistics.totalPlayedTime)
            }),
            { format: ['days', 'hours', 'minutes'], zero: false }
          )
        },
        { title: t('statistics.favorite-color'), value: statistics.favoriteColor }
      ]);
    }
  }, [games, statistics, t])

  return (
    statistics && <GcStatisticsRow cards={cards} />
  )
}