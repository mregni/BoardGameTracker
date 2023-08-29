import {addMinutes, formatDuration, intervalToDuration} from 'date-fns';
import {useContext, useEffect, useState} from 'react';

import {GcStatisticsRow, StatisticsCard} from '../../../components/GcStatistics';
import {GamesContext} from '../../Games/context';
import {PlayerDetailContext} from '../context/PlayerDetailState';

export const PlayerStatistics = () => {
  const { statistics } = useContext(PlayerDetailContext);
  const { games } = useContext(GamesContext);
  const [cards, setCards] = useState<StatisticsCard[]>([]);

  useEffect(() => {
    if (statistics !== null) {
      const game = games.find(x => x.id == statistics.bestGameId);

      setCards([
        { title: "Play count", value: statistics.playCount },
        { title: "Total wins", value: statistics.winCount },
        { title: "Best game", value: game?.title ?? null },
        {
          title: "Total play time", value: formatDuration(
            intervalToDuration({
              start: new Date(2000, 1, 1, 0, 0, 0),
              end: addMinutes(new Date(2000, 1, 1, 0, 0, 0), statistics.totalPlayedTime)
            }),
            { format: ['days', 'hours', 'minutes'], zero: false }
          )
        },
        { title: 'Favorite color', value: statistics.favoriteColor }
      ]);
    }
  }, [statistics])

  return (
    statistics && <GcStatisticsRow cards={cards} />
  )
}