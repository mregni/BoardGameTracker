import { ArrayPath, Control, FieldArrayWithId, UseFieldArrayRemove, useController } from 'react-hook-form';
import { Dispatch, SetStateAction } from 'react';
import { t } from 'i18next';
import { Badge, Text } from '@radix-ui/themes';
import { ClockIcon, PencilIcon, TrashIcon, TrophyIcon } from '@heroicons/react/24/outline';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import BgtButton from '../BgtButton/BgtButton';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { CreateSession } from '../../models/Session/CreateSession';
import { usePlayerById } from '../../hooks/usePlayerById';

import { BgtFormErrors } from './BgtFormErrors';

interface Props {
  name: ArrayPath<CreateSession>;
  control: Control<CreateSession>;
  setCreateModalOpen: Dispatch<SetStateAction<boolean>>;
  setPlayerIdToEdit: Dispatch<SetStateAction<string | null>>;
  setUpdateModalOpen: Dispatch<SetStateAction<boolean>>;
  remove: UseFieldArrayRemove;
  players: FieldArrayWithId<CreateSession>[];
  disabled: boolean;
}

export const BgtPlayerSelector = (props: Props) => {
  const { name, control, setCreateModalOpen, remove, players, setPlayerIdToEdit, setUpdateModalOpen, disabled } = props;
  const { playerById } = usePlayerById();

  const {
    fieldState: { error },
  } = useController({ name, control });

  const editPlayer = (playerId: string): void => {
    setPlayerIdToEdit(playerId);
    setUpdateModalOpen(true);
  };

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col md:flex-row md:justify-between md:items-center gap-1">
        <div className="text-[15px] font-medium leading-[35px] uppercase flex flex-row gap-2 items-center">
          {t('player-session.new.players.label')}
          <BgtButton
            className="w-fit"
            type="button"
            size="1"
            onClick={() => setCreateModalOpen(true)}
            disabled={disabled}
          >
            {t('player-session.new.players.add')}
          </BgtButton>
        </div>
        <BgtFormErrors error={error} />
      </div>
      {players.map((x, index) => (
        <div key={x.playerId} className="flex flex-row gap-3 justify-between">
          <div className="flex items-center gap-3">
            <BgtAvatar
              noTooltip
              size="large"
              title={playerById(x.playerId)?.name}
              image={playerById(x.playerId)?.image}
              color={StringToHsl(playerById(x.playerId)?.name)}
            />
            <Text>{playerById(x.playerId)?.name}</Text>
            {x.won && <TrophyIcon className="w-4  text-yellow-600" />}
            {x.firstPlay && <ClockIcon className="w-4 text-green-600" />}
            {'score' in x && x.score !== undefined && <Badge>{x.score}</Badge>}
          </div>
          <div className="flex items-center gap-1">
            <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlayer(x.id)} disabled={disabled} />
            <BgtIconButton
              size={17}
              icon={<TrashIcon />}
              onClick={() => remove(index)}
              type="danger"
              disabled={disabled}
            />
          </div>
        </div>
      ))}
    </div>
  );
};
