import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import i18n from '@/utils/i18n';
import { toRelative } from '@/utils/dateUtils';
import { useSettingsData } from '@/routes/settings/-hooks/useSettingsData';
import { RecentGame } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import Sparkles from '@/assets/icons/sparkles.svg?react';

interface Props extends React.HTMLAttributes<HTMLDivElement> {
  games: RecentGame[];
}

export const RecentAddedGamesCard = ({ games, className }: Props) => {
  const { t } = useTranslation();

  return (
    <BgtCard title={t('dashboard.recent-added-games')} icon={Sparkles} className={className}>
      <div className="flex flex-col gap-3">
        {games.map((game) => (
          <GameCardItem key={game.id} game={game} />
        ))}
      </div>
    </BgtCard>
  );
};

interface ItemProps {
  game: RecentGame;
}

const GameCardItem = (props: ItemProps) => {
  const { game } = props;
  const navigate = useNavigate();
  const { settings } = useSettingsData({});

  if (settings === undefined) return null;

  return (
    <div className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
      <BgtAvatar
        onClick={() => navigate({ to: `/players/${game.id}` })}
        image={game.image}
        title={game.title}
        color={StringToHsl(game.title)}
        size="large"
      />
      <div className="flex-1">
        <BgtText color="white" className="text-white uppercase">
          {game.title}
        </BgtText>
        <BgtText color="white" opacity={50} size="2">
          {toRelative(game.additionDate, i18n.language)}
          {game.price !== null && game.price > 0 && (
            <span>
              {' '}
              • {settings.currency}
              {game.price}
            </span>
          )}
        </BgtText>
      </div>
    </div>
  );
};
