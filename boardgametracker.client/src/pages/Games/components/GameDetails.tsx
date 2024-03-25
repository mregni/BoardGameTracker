import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {BgtCard} from '../../../components/BgtLayout/BgtCard';
import {BgtStatistic} from '../../../components/BgtStatistic/BgtStatistic';
import {useGame} from '../../../hooks/useGame';
import {useSettings} from '../../../hooks/useSettings';
import {RoundDecimal} from '../../../utils/roundDecimal';

export const GameDetails = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { game } = useGame(id);
  const { statistics } = useGame(id);
  const { settings } = useSettings();

  if (game === undefined) return null;

  if (statistics === undefined || settings === undefined) return null;

  const sanitiseValues = (val1: number | null, val2: number | null): string | null => {
    if (val1 === val2) {
      return val1?.toString() ?? null
    }

    return `${val1} - ${val2}`
  }

  return (
    <div className='grid grid-cols-3 md:grid-cols-3 md:col-span-2 2xl:grid-cols-5 auto-rows-min gap-3'>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!game.minPlayTime || !game.maxPlayTime}>
        <BgtStatistic content={sanitiseValues(game.minPlayTime, game.maxPlayTime)} title={t('common.play-time')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!game.minPlayers || !game.maxPlayTime}>
        <BgtStatistic content={sanitiseValues(game.minPlayers, game.maxPlayers)} title={t('common.players')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!game.rating}>
        <BgtStatistic content={game.rating?.toFixed(1)} suffix='/10' title={t('common.rating')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!game.weight}>
        <BgtStatistic content={RoundDecimal(game.weight)} suffix='/5' title={t('common.weight')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!game.minAge}>
        <BgtStatistic content={game.minAge} title={t('common.age')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!statistics.highScore}>
        <BgtStatistic content={statistics.highScore} title={t('statistics.high-score')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!statistics.playCount}>
        <BgtStatistic content={statistics.playCount} title={t('statistics.play-count')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!statistics.totalPlayedTime}>
        <BgtStatistic content={statistics.totalPlayedTime} title={t('statistics.total-play-time')} suffix={t('common.minutes_abbreviation')} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!statistics.pricePerPlay}>
        <BgtStatistic content={statistics.pricePerPlay} title={t('statistics.price-per-play')} suffix={settings.currency} />
      </BgtCard>
      <BgtCard contentStyle='bg-gradient-to-r from-sky-600 to-sky-900' hide={!statistics.mostWinsPlayer}>
        <BgtStatistic content={statistics.mostWinsPlayer?.name} title={t('statistics.most-wint')} />
      </BgtCard>
    </div>
  )
}
