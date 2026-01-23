import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import { DashboardTopPlayer } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import Trophy from '@/assets/icons/trophy.svg?react';
interface Props extends React.HTMLAttributes<HTMLDivElement> {
  topPlayers: DashboardTopPlayer[];
}

export const TopPlayersCard = (props: Props) => {
  const { topPlayers, className } = props;
  const { t } = useTranslation();

  return (
    <BgtCard title={t('game.titles.top-players')} icon={Trophy} className={className}>
      <div className="flex flex-col gap-3">
        {topPlayers.map((player) => (
          <TopPlayerCardItem key={player.id} player={player} />
        ))}
      </div>
    </BgtCard>
  );
};

interface ItemProps {
  player: DashboardTopPlayer;
}

const TopPlayerCardItem = (props: ItemProps) => {
  const { player } = props;
  const { t } = useTranslation();
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  const playerObj = playerById(player.id);

  return (
    <div className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
      <BgtAvatar
        onClick={() => navigate({ to: `/players/${player.id}` })}
        image={playerObj?.image}
        title={playerObj?.name}
        color={StringToHsl(playerObj?.name)}
        size="large"
      />
      <div className="flex-1">
        <BgtText color="white" className="text-white uppercase">
          {playerObj?.name}
        </BgtText>
        <BgtText color="white" opacity={50} size="2">
          {t('common.win', { count: player.winCount })} • {t('common.game', { count: player.playCount })}
        </BgtText>
      </div>
    </div>
  );
};
