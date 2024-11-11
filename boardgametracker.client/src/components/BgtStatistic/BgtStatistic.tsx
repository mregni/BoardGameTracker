import { useNavigate } from 'react-router-dom';
import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

import { BgtAvatar } from '../BgtAvatar/BgtAvatar';

import { StringToHsl } from '@/utils/stringUtils';
import { Game, Player } from '@/models';

interface Props {
  title: string;
  content: string | number | null | undefined;
  suffix?: string | number | null;
  player?: Player | null;
  game?: Game | null;
}

export const BgtStatistic = (props: Props) => {
  const { title, content, suffix, player = null, game = null } = props;
  const navigate = useNavigate();

  if (content === null || content === undefined || Number.isNaN(content)) return null;

  return (
    <div className="flex flex-col justify-center items-center min-w-24 p-3">
      <div className={cx('flex flex-row', (player ?? game) && 'gap-1 items-center')}>
        {player && (
          <BgtAvatar
            title={player.name}
            image={player.image}
            onClick={() => navigate(`/players/${player.id}`)}
            color={StringToHsl(player.name)}
          />
        )}
        {game && (
          <BgtAvatar
            title={game.title}
            image={game.image}
            onClick={() => navigate(`/games/${game.id}`)}
            color={StringToHsl(game.title)}
          />
        )}
        <div className="text-xl font-bold line-clamp-1">
          {content}
          {suffix && <span className="text-sm">&nbsp;{suffix}</span>}
        </div>
      </div>
      <Text size="1">{title}</Text>
    </div>
  );
};
