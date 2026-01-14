import { Dispatch, SetStateAction } from 'react';
import { t } from 'i18next';
import { ValidationError } from '@tanstack/react-form';

import { BgtText } from '../BgtText/BgtText';
import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import BgtButton from '../BgtButton/BgtButton';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';

import { StringToHsl } from '@/utils/stringUtils';
import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import { CreateSessionPlayer, CreatePlayerSessionNoScoring } from '@/models';
import TrophyIcon from '@/assets/icons/trophy.svg?react';
import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';
import ClockIcon from '@/assets/icons/clock.svg?react';

interface Props {
  setCreateModalOpen: Dispatch<SetStateAction<boolean>>;
  setPlayerIdToEdit: Dispatch<SetStateAction<number | null>>;
  setUpdateModalOpen: Dispatch<SetStateAction<boolean>>;
  remove: (index: number) => void;
  players: (CreateSessionPlayer | CreatePlayerSessionNoScoring)[];
  disabled: boolean;
  errors?: ValidationError[];
}

export const BgtPlayerSelector = (props: Props) => {
  const { setCreateModalOpen, remove, players, setPlayerIdToEdit, setUpdateModalOpen, disabled } = props;
  const errors = props.errors ?? [];
  const { playerById } = usePlayerById();

  const editPlayer = (playerId: number): void => {
    setPlayerIdToEdit(playerId);
    setUpdateModalOpen(true);
  };

  const hasErrors = errors.length > 0;

  return (
    <div className="flex flex-col gap-3 mt-3">
      <div className="flex flex-col md:flex-row md:justify-between md:items-center">
        <div className="text-[15px] font-medium leading-[35px] uppercase flex flex-row items-center justify-between w-full">
          <BgtText>{t('player-session.new.players.label')}</BgtText>
          <BgtButton
            className="w-fit"
            type="button"
            variant="primary"
            size="1"
            onClick={() => setCreateModalOpen(true)}
            disabled={disabled}
          >
            {t('player-session.new.players.add')}
          </BgtButton>
        </div>
      </div>
      {players.length === 0 && !hasErrors && <BgtText color={'primary'}>{t('player.new.players.none')}</BgtText>}
      {players.length === 0 && hasErrors && <BgtText color={'red'}>{String(errors[0])}</BgtText>}
      {players.map((x, index) => {
        const player = playerById(x.playerId);
        const playerName = player?.name ?? '';

        return (
          <div
            key={x.playerId}
            className="flex flex-row gap-3 justify-between w-full bg-background font- text-white px-4 py-3 rounded-lg border border-primary/30 focus:border-primary focus:outline-none"
          >
            <div className="flex items-center gap-3">
              <BgtAvatar size="large" title={playerName} image={player?.image} color={StringToHsl(playerName)} />
              <BgtText>{playerName}</BgtText>
              {x.won || x.firstPlay || 'score' in x ? <BgtText> - </BgtText> : ''}
              {'score' in x && x.score !== undefined && (
                <BgtText color="cyan" weight="bold">
                  {x.score}
                </BgtText>
              )}
              {x.won && <TrophyIcon className="w-4  text-yellow-600" />}
              {x.firstPlay && <ClockIcon className="w-4 text-green-600" />}
            </div>
            <div className="flex items-center gap-1">
              <BgtIconButton icon={<PencilIcon />} onClick={() => editPlayer(x.playerId)} disabled={disabled} />
              <BgtIconButton icon={<TrashIcon />} onClick={() => remove(index)} intent="danger" disabled={disabled} />
            </div>
          </div>
        );
      })}
    </div>
  );
};
