import {addMinutes, formatDuration, intervalToDuration} from 'date-fns';
import {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GcStatisticsRow, StatisticsCard} from '../../../components/GcStatistics';
import {SettingsContext} from '../../../context/settingsContext';
import {GameDetailContext} from '../context/GameDetailState';

export const GameStatistics = () => {
  const { statistics } = useContext(GameDetailContext);
  const {settings} = useContext(SettingsContext);
  const {t} = useTranslation();
  const [cards, setCards] = useState<StatisticsCard[]>([]);

  useEffect(() => {
    if (statistics !== null) {
      setCards([
        { title: t('statistics.play-count'), value: statistics.playCount },
        { title: t('statistics.price-per-play'), value: statistics.pricePerPlay, suffix: settings.currency, precision: 2 },
        { title: t('statistics.unique-players'), value: statistics.uniquePlayerCount },
        {
          title: t('statistics.total-play-time'), value: formatDuration(
            intervalToDuration({
              start: new Date(2000, 1, 1, 0, 0, 0),
              end: addMinutes(new Date(2000, 1, 1, 0, 0, 0), statistics.totalPlayedTime)
            }),
            { format: ['days', 'hours', 'minutes'], zero: false }
          )
        },
        { title: t('statistics.high-score'), value: statistics.highScore },
        { title: t('statistics.average-score'), value: statistics.averageScore, precision: 2 },
        { title: t('statistics.most-wins'), value: statistics.mostWinsPlayer?.name ?? null }
      ]);
    }
  }, [statistics, t])

  return (
    statistics && <GcStatisticsRow cards={cards} />
  )
}