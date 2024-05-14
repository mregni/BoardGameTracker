import { ArrayPath, Control, FieldArrayWithId, UseFieldArrayRemove, useController } from 'react-hook-form';
import { Dispatch, SetStateAction } from 'react';
import { t } from 'i18next';
import { Badge, Button, Text } from '@radix-ui/themes';
import { ClockIcon, PencilIcon, RocketLaunchIcon, TrashIcon, TrophyIcon } from '@heroicons/react/24/outline';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { CreatePlay } from '../../models/Plays/CreatePlay';
import { usePlayers } from '../../hooks/usePlayers';

import { BgtFormErrors } from './BgtFormErrors';

interface Props {
  name: ArrayPath<CreatePlay>;
  control: Control<CreatePlay>;
  setCreateModalOpen: Dispatch<SetStateAction<boolean>>;
  setPlayerIdToEdit: Dispatch<SetStateAction<string | null>>;
  setUpdateModalOpen: Dispatch<SetStateAction<boolean>>;
  remove: UseFieldArrayRemove;
  players: FieldArrayWithId<CreatePlay>[];
}

export const BgtPlayerSelector = (props: Props) => {
  const { name, control, setCreateModalOpen, remove, players, setPlayerIdToEdit, setUpdateModalOpen } = props;
  const { byId } = usePlayers();

  const {
    fieldState: { error },
  } = useController({ name, control });

  const editPlayer = (playerId: string): void => {
    setPlayerIdToEdit(playerId);
    setUpdateModalOpen(true);
  };

  return (
    <div className="flex flex-col gap-3 md:max-w-96">
      <div className="flex flex-col gap-1">
        <Button className="w-fit" type="button" onClick={() => setCreateModalOpen(true)}>
          {t('playplayer.new.players.add')}
        </Button>
        <BgtFormErrors error={error} />
      </div>
      {players.map((x, index) => (
        <div key={x.playerId} className="flex flex-row gap-3 md:max-w-96 justify-between">
          <div className="flex items-center gap-3">
            <BgtAvatar
              noTooltip
              size="large"
              title={byId(x.playerId)?.name}
              image={byId(x.playerId)?.image}
              color={StringToHsl(byId(x.playerId)?.name)}
            />
            <Text>{byId(x.playerId)?.name}</Text>
            {x.won && <TrophyIcon className="w-4  text-yellow-600" />}
            {x.firstPlay && <ClockIcon className="w-4 text-green-600" />}
            {x.isBot && <RocketLaunchIcon className="w-4 text-red-600" />}
            {'score' in x && x.score !== undefined && <Badge>{x.score}</Badge>}
          </div>
          <div className="flex items-center gap-1">
            <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlayer(x.id)} />
            <BgtIconButton size={17} icon={<TrashIcon />} onClick={() => remove(index)} type="danger" />
          </div>
        </div>
      ))}
    </div>
  );
};
