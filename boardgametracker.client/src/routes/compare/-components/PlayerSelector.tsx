import { useTranslation } from 'react-i18next';
import { memo, useCallback, useMemo } from 'react';

import { PlayerAvatarWithCrown } from './PlayerAvatarWithCrown';

import { Player } from '@/models';
import { BgtSimpleSelect } from '@/components/BgtForm/BgtSimpleSelect';

interface PlayerSelectorProps {
  player: Player;
  players: Player[];
  isWinner: boolean;
  onPlayerChange: (playerId: number) => void;
}

const PlayerSelectorComponent = ({ player, players, isWinner, onPlayerChange }: PlayerSelectorProps) => {
  const { t } = useTranslation();

  const playerItems = useMemo(() => players.map((p) => ({ value: p.id, label: p.name, image: p.image })), [players]);

  const handleValueChange = useCallback(
    (value: string | number) => {
      onPlayerChange(Number(value));
    },
    [onPlayerChange]
  );

  return (
    <div className="flex flex-col items-center">
      <PlayerAvatarWithCrown player={player} isWinner={isWinner} />
      <BgtSimpleSelect
        items={playerItems}
        placeholder={t('compare.select-player')}
        hasSearch={true}
        value={player.id}
        onValueChange={handleValueChange}
      />
    </div>
  );
};

PlayerSelectorComponent.displayName = 'PlayerSelector';

export const PlayerSelector = memo(PlayerSelectorComponent);
