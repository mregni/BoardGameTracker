import {addMinutes, formatDuration, intervalToDuration} from 'date-fns';
import {useContext, useEffect, useState} from 'react';

import {GcStatisticsRow, StatisticsCard} from '../../../components/GcStatistics';
import {GameDetailContext} from '../context/GameDetailState';

export const GameStatistics = () => {
  const { statistics } = useContext(GameDetailContext);
  const [cards, setCards] = useState<StatisticsCard[]>([]);

  useEffect(() => {
    if (statistics !== null) {
      setCards([
        { title: "Play count", value: statistics.playCount },
        { title: "Price per play", value: statistics.pricePerPlay, suffix: "€", precision: 2 },
        { title: "Unique players", value: statistics.uniquePlayerCount },
        {
          title: "Total play time", value: formatDuration(
            intervalToDuration({
              start: new Date(2000, 1, 1, 0, 0, 0),
              end: addMinutes(new Date(2000, 1, 1, 0, 0, 0), statistics.totalPlayedTime)
            }),
            { format: ['days', 'hours', 'minutes'], zero: false }
          )
        },
        { title: 'High score', value: statistics.highScore },
        { title: 'Average score', value: statistics.averageScore, precision: 2 },
        { title: "Most wins", value: statistics.mostWinsPlayer?.name ?? null }
      ]);
    }
  }, [statistics])

  return (
    statistics && <GcStatisticsRow cards={cards} />
  )
}