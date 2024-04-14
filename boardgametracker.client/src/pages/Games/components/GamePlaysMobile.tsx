import {format} from 'date-fns';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {InformationCircleIcon, PencilIcon, TrashIcon} from '@heroicons/react/24/outline';
import {Badge, Text} from '@radix-ui/themes';

import {BgtAvatar} from '../../../components/BgtAvatar/BgtAvatar';
import {BgtCard} from '../../../components/BgtCard/BgtCard';
import {BgtIconButton} from '../../../components/BgtIconButton/BgtIconButton';
import {BgtNoData} from '../../../components/BgtNoData/BgtNoData';
import {useLocations} from '../../../hooks/useLocations';
import {usePlayers} from '../../../hooks/usePlayers';
import {useGamePlays} from '../../../hooks/usePlays';
import {useSettings} from '../../../hooks/useSettings';
import {PlayFlagToString} from '../../../utils/stringUtils';

const deletePlay = (id: number) => {
  //TODO: Implement
  console.log("delete: " + id)
}

const editPlay = (id: number) => {
  //TODO: Implement
  console.log("edit: " + id)
}

export const MobileDetails = () => {
  const { id } = useParams();
  const { plays } = useGamePlays(id, 0, 10);
  const { t } = useTranslation();
  const { settings } = useSettings();
  const navigate = useNavigate();
  const { byId: playerById } = usePlayers();
  const { byId: locationById } = useLocations();

  if (settings === undefined) return null;

  if (plays.length === 0) {
    return (
      <BgtCard
        title={t('games.cards.games')}
        contentStyle='bg-sky-800'
        className='md:hidden'>
        <BgtNoData />
      </BgtCard>
    )
  }

  return (
    <div className='md:hidden rounded-md flex flex-col gap-3'>
      {
        plays.map((play, i) =>
          <BgtCard
            title={i === 0 ? t('games.cards.games') : undefined}
            contentStyle='bg-sky-800'
            key={play.id}>
            <div className='flex flex-col gap-3'>
              <div className='flex flex-row justify-between'>
                <div className='flex flex-col justify-center flex-none'>
                  <div className='font-bold'>{format(play.start, settings?.dateFormat ?? '')}</div>
                  <div className='text-xs'>{format(play.start, settings?.timeFormat ?? '')}</div>
                </div>
                <div className='flex flex-row justify-center gap-1'>
                  <BgtIconButton size={17} icon={<InformationCircleIcon />} onClick={() => editPlay(play.id)} />
                  <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlay(play.id)} />
                  <BgtIconButton size={17} icon={<TrashIcon />} onClick={() => deletePlay(play.id)} color='text-red-600' hoverColor='text-red-400' />
                </div>
              </div>
              <div className='flex flex-row justify-center gap-1'>
                {
                  play.playFlags.map(flag => (
                    <Badge key={flag} variant="solid">{t(PlayFlagToString(flag))}</Badge>
                  ))
                }
              </div>
              <div className='flex flex-row justify-between'>
                <div className='flex flex-col'>
                  <Text weight="bold" align="left">{t('common.winners', {})}</Text>
                  <div className='flex gap-1 justify-start'>
                    {play.players.filter(x => x.won).map((player) => <BgtAvatar
                      onClick={() => navigate(`/players/${player.playerId}`)}
                      title={playerById(player.playerId)?.name}
                      image={playerById(player.playerId)?.image}
                      key={player.playerId}
                    />
                    )}
                  </div>
                </div>
                <div className='flex flex-col'>
                  <Text weight="bold" align="right">{t('common.loosers')}</Text>
                  <div className='flex gap-1 justify-end'>
                    {play.players.filter(x => !x.won).map((player) => <BgtAvatar
                      onClick={() => navigate(`/players/${player.playerId}`)}
                      title={playerById(player.playerId)?.name}
                      image={playerById(player.playerId)?.image}
                      key={player.playerId}
                    />
                    )}
                  </div>
                </div>
              </div>
              <div className='flex flex-row justify-between'>
                <div className='flex flex-col'>
                  <Text weight="bold" align="left">{t('common.duration')}</Text>
                  <div className='flex gap-1 justify-start'>
                    {play.minutes} minutes
                  </div>
                </div>
                <div className='flex flex-col justify-end'>
                  <Text weight="bold" align="right">{t('common.location')}</Text>
                  <div className='flex gap-1 justify-end'>
                    {locationById(play.locationId)?.name}
                  </div>
                </div>
              </div>
            </div>
          </BgtCard>)
      }
    </div>
  )
}