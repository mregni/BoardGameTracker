import { useTranslation } from 'react-i18next';

import { TopPlayerCardItem } from './TopPlayerCardItem';

import { TopPlayer } from '@/models';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import Trophy from '@/assets/icons/trophy.svg?react';

interface Props {
  topPlayers: TopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
  const { topPlayers } = props;
  const { t } = useTranslation();

  return (
    <BgtCard title={t('game.titles.top-players')} icon={Trophy}>
      <div className="flex flex-col gap-3">
        {topPlayers.map((player) => (
          <TopPlayerCardItem key={player.playerId} player={player} />
        ))}
      </div>
    </BgtCard>
  );
};
