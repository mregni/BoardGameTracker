import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {ArrowTrendingDownIcon, ArrowTrendingUpIcon, MinusIcon} from '@heroicons/react/24/outline';

import {BgtAvatar} from '../../../components/BgtAvatar/BgtAvatar';
import {BgtCard} from '../../../components/BgtCard/BgtCard';
import {BgtIcon} from '../../../components/BgtIcon/BgtIcon';
import {BgtNoData} from '../../../components/BgtNoData/BgtNoData';
import {useGame} from '../../../hooks/useGame';
import {usePlayers} from '../../../hooks/usePlayers';
import {Trend} from '../../../models/Games/TopPlayer';
import {RoundDecimal} from '../../../utils/numberUtils';

export const GameTopPlayers = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { topPlayers } = useGame(id);
  const { byId } = usePlayers();
  const navigate = useNavigate();

  if (topPlayers === undefined) return null;

  if (topPlayers.length === 0) {
    return <BgtNoData />
  }

  return (
    <BgtCard
      title={t('games.cards.top-players')}
      contentStyle='bg-sky-800 min-h-[300px]'
    >
      <div className='flex flex-col gap-3 divide-y divide-sky-600'>
        {topPlayers.map((player) =>
          <div key={player.playerId} className='grid grid-cols-4 md:grid-cols-5 gap-3 justify-between items-center divide-x divide-sky-600 pt-3'>
            <div className='flex gap-1 items-center col-span-2 md:col-span-3'>
              <BgtAvatar
                onClick={() => navigate(`/players/${player.playerId}`)}
                title={byId(player.playerId)?.name}
                image={byId(player.playerId)?.image}
                key={player.playerId}
              />
              <div>{byId(player.playerId)?.name}</div>
            </div>
            <div className='flex pl-3 justify-end items-end'>
              <div>
                <span className='text-xl'>{player.wins}</span>
                <span className='text-sm'>&nbsp;/{player.playCount}</span>
              </div>
            </div>
            <div className='pl-3 flex text-xl gap-1 items-center justify-end'>
              <div>{RoundDecimal(player.winPercentage * 100, 0.1)}%</div>
              {player.trend === Trend.Up && <BgtIcon icon={<ArrowTrendingUpIcon />} className='text-green-400' size={17} />}
              {player.trend === Trend.Down && <BgtIcon icon={<ArrowTrendingDownIcon />} className='text-red-500' size={17} />}
              {player.trend === Trend.Equal && <BgtIcon icon={<MinusIcon />} className='text-orange-400' size={17} />}
            </div>
          </div>
        )}
      </div>
    </BgtCard>
  )
}