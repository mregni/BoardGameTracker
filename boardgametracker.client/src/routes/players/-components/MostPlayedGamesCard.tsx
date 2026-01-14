import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { MostPlayedGame } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import Trophy from '@/assets/icons/trophy.svg?react';

interface Props {
  games: MostPlayedGame[];
}

export const MostPlayedGamesCard = (props: Props) => {
  const { games } = props;
  const { t } = useTranslation();

  return (
    <BgtCard title={t('player.cards.most-played-games')} icon={Trophy}>
      <div className="flex flex-col gap-3">
        {games.map((game) => (
          <MostPlayedGameItem key={game.id} game={game} />
        ))}
      </div>
    </BgtCard>
  );
};

interface MostPlayedGameItemProps {
  game: MostPlayedGame;
}

const MostPlayedGameItem = ({ game }: MostPlayedGameItemProps) => {
  const navigate = useNavigate();

  return (
    <BgtCard>
      <div className="flex items-center gap-4">
        <div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
          <BgtAvatar
            onClick={() => navigate({ to: `/games/${game.id}` })}
            image={game.image}
            title={game.title}
            color={StringToHsl(game.title)}
            size="large"
          />
        </div>
        <div className="flex-1">
          <BgtText color="white">{game.title}</BgtText>
          <div className="text-white/50 text-sm">
            {game.totalSessions} plays • {game.totalWins} wins
          </div>
        </div>
        <div className="text-cyan-400 font-bold">{game.winningPercentage}%</div>
      </div>
    </BgtCard>
  );
};
