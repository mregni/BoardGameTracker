import {format} from 'date-fns';
import {Fragment} from 'react';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {PencilIcon, TrashIcon} from '@heroicons/react/24/outline';
import {Text} from '@radix-ui/themes';

import {BgtAvatar} from '../../../components/BgtAvatar/BgtAvatar';
import {BgtIconButton} from '../../../components/BgtIconButton/BgtIconButton';
import {useGame} from '../../../hooks/useGame';
import {usePlayers} from '../../../hooks/usePlayers';
import {useSettings} from '../../../hooks/useSettings';
import {Play} from '../../../models';
import {getPlayerImage} from '../../../utils/getPlayerUtils';

interface Props {
  play: Play;
}

const DesktopDetails = (props: Props) => {
  const { play } = props;
  const { t } = useTranslation();
  const { players } = usePlayers();
  const { settings } = useSettings();
  const navigate = useNavigate();

  if (settings === undefined) return null;

  return (
    <div className='hidden md:block'>
      <div key={play.id} className='rounded-md bg-sky-900 p-3 flex flex-row justify-between gap-3 divide-x divide-sky-600'>
        <div className='flex flex-col justify-center flex-none'>
          <div className='font-bold'>{format(play.start, settings.dateFormat)}</div>
          <div className='text-xs'>{format(play.start, settings.timeFormat)}</div>
        </div>
        <div className='flex flex-col gap-1 px-3 flex-none'>
          <div className='font-bold'>
            {t('common.winner', { count: play.players.filter(x => x.won).length })}
          </div>
          <div className='flex flex-row grow gap-1'>
            {
              play.players
                .filter(x => x.won)
                .map((player) => <BgtAvatar
                  onClick={() => navigate(`/players/${player.playerId}`)}
                  src={`/${getPlayerImage(player.playerId, players)}`}
                  key={player.playerId}
                />)
            }
          </div>
        </div>
        <div className='flex flex-col gap-1 px-3 flex-none'>
          <div className='font-bold'>
            {t('common.other', { count: play.players.filter(x => !x.won).length })}
          </div>
          <div className='flex flex-row gap-1'>
            {
              play.players
                .filter(x => !x.won)
                .map((player) => <BgtAvatar
                  onClick={() => navigate(`/players/${player.playerId}`)}
                  src={`/${getPlayerImage(player.playerId, players)}`}
                  key={`other${player.playerId}`}
                />)
            }
          </div>
        </div>
        <div className='px-3 flex flex-row grow content-center flex-wrap'>
          <Text as='span' size="8">{play.minutes}</Text>
          <div className='flex content-end flex-wrap'>
            <Text as='span' size="3">&nbsp;{t('common.minutes_abbreviation')}</Text>
          </div>
        </div>
        <div className='px-3 flex flex-col justify-center gap-3 flex-none'>
          <BgtIconButton
            size={17}
            icon={<PencilIcon />}
          />
          <BgtIconButton
            size={17}
            icon={<TrashIcon />}
            color='text-red-600'
            hoverColor='text-red-400'
          />
        </div>
      </div>
    </div>
  )
}

const MobileDetails = (props: Props) => {
  const { play } = props;
  const { t } = useTranslation();
  const { players } = usePlayers();
  const { settings } = useSettings();
  const navigate = useNavigate();

  if (settings === undefined) return null;

  return (
    <div className='md:hidden'>
      <div className='flex flex-col gap-2 rounded-md bg-sky-900 p-3'>
        <div className='flex flex-row justify-between'>
          <div>
            <Text weight="bold">{format(play.start, settings.dateFormat)}&nbsp;</Text>
            <Text>{format(play.start, settings.timeFormat)}</Text>
          </div>
          <div>
            <BgtIconButton
              size={17}
              icon={<PencilIcon />}
            />
            <BgtIconButton
              size={17}
              icon={<TrashIcon />}
              color='text-red-600'
              hoverColor='text-red-400'
            />
          </div>
        </div>
        <div key={play.id} className='flex flex-row justify-between gap-3 divide-x divide-sky-600'>
          <div className='flex flex-col gap-1 px-3'>
            <div className='font-bold'>
              {t('common.winner', { count: play.players.filter(x => x.won).length })}
            </div>
            <div className='flex flex-row gap-1'>
              {
                play.players
                  .filter(x => x.won)
                  .map((player) => <BgtAvatar
                    onClick={() => navigate(`/players/${player.playerId}`)}
                    src={`/${getPlayerImage(player.playerId, players)}`}
                    key={player.playerId}
                  />)
              }
            </div>
          </div>
          <div className='flex flex-col gap-1 px-3'>
            <div className='font-bold'>
              {t('common.other', { count: play.players.filter(x => !x.won).length })}
            </div>
            <div className='flex flex-row gap-1'>
              {
                play.players
                  .filter(x => !x.won)
                  .map((player) => <BgtAvatar
                    onClick={() => navigate(`/players/${player.playerId}`)}
                    src={`/${getPlayerImage(player.playerId, players)}`}
                    key={player.playerId}
                  />)
              }
            </div>
          </div>
          <div className='px-3 flex flex-row content-center flex-wrap'>
            <Text as='span' size="8">{play.minutes}</Text>
            <div className='flex content-end flex-wrap'>
              <Text as='span' size="3">&nbsp;{t('common.minutes_abbreviation')}</Text>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export const GamePlays = () => {
  const { id } = useParams();
  const { plays } = useGame(id);

  if (plays === undefined) return null;

  return (
    <div className='flex flex-col gap-3'>
      {
        plays.map((play) =>
          <Fragment key={play.id}>
            <MobileDetails play={play} />
            <DesktopDetails play={play} />
          </Fragment>
        )
      }
    </div>
  )
}