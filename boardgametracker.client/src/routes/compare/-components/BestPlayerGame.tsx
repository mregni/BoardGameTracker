import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { CompareRow, Game, MostWonGame, Player } from '@/models';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props {
  games: Game[];
  position: 'left' | 'right';
  playerLeft: Player;
  playerRight: Player;
  row: CompareRow<MostWonGame | null>;
}

export const BestPlayerGame = (props: Props) => {
  const { games, position, playerLeft, playerRight, row } = props;
  const navigate = useNavigate();
  const { t } = useTranslation();

  const isLeftPosition = position === 'left';
  const currentPlayer = isLeftPosition ? playerLeft : playerRight;
  const otherPlayer = isLeftPosition ? playerRight : playerLeft;
  const currentGameResult = isLeftPosition ? row.left : row.right;
  const gameId = isLeftPosition ? currentGameResult?.gameId : currentGameResult?.gameId;

  const game = useMemo(() => games.filter((game) => game.id === gameId?.toString())[0], [gameId, games]);

  if (!row || !row.left || !row.right || !currentGameResult || !game) {
    return null;
  }

  return (
    <div className="flex flex-row gap-2 items-center">
      {!isLeftPosition && (
        <BgtAvatar
          image={game?.image}
          color={StringToHsl(game?.title)}
          title={game?.title}
          onClick={() =>
            navigate({
              to: `/games/${game?.id}`,
            })
          }
        />
      )}
      {t('compare.best-game.label', {
        player: currentPlayer.name,
        count: currentGameResult.count,
        game: game?.title,
        otherPlayer: otherPlayer.name,
      })}
      {isLeftPosition && (
        <BgtAvatar
          image={game?.image}
          color={StringToHsl(game?.title)}
          title={game?.title}
          onClick={() =>
            navigate({
              to: `/games/${game?.id}`,
            })
          }
        />
      )}
    </div>
  );
};
