import { useTranslation } from 'react-i18next';
import { memo, useMemo } from 'react';

import { useGameById } from '../../-hooks/useGameById';

import { toDisplay } from '@/utils/dateUtils';
import { CompareResult } from '@/models';
import { Player } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtCard } from '@/components/BgtCard/BgtCard';

interface HeadToHeadProps {
  playerOne: Player;
  playerTwo: Player;
  compare: CompareResult;
  dateFormat: string;
  uiLanguage: string;
}

const HeadToHeadComponent = ({ playerOne, playerTwo, compare, dateFormat, uiLanguage }: HeadToHeadProps) => {
  const { t } = useTranslation();

  const { gameById } = useGameById();

  const headToHeadStats = useMemo(
    () =>
      [
        {
          key: 'direct-wins-player-one',
          isValid: true,
          text: t('compare.head-to-head.direct-wins', {
            player: playerOne.name,
            count: compare.directWins.playerOne,
            opponent: playerTwo.name,
          }),
        },
        {
          key: 'direct-wins-player-two',
          isValid: true,
          text: t('compare.head-to-head.direct-wins', {
            player: playerTwo.name,
            count: compare.directWins.playerTwo,
            opponent: playerOne.name,
          }),
        },
        compare.mostWonGame.playerOne?.gameId && compare.mostWonGame.playerOne?.count
          ? {
              key: 'most-won-player-one',
              isValid: true,
              text: t('compare.head-to-head.most-won-game', {
                player: playerOne.name,
                opponent: playerTwo.name,
                game: gameById(compare.mostWonGame.playerOne.gameId)?.title,
                count: compare.mostWonGame.playerOne.count,
              }),
            }
          : null,
        compare.mostWonGame.playerTwo?.gameId && compare.mostWonGame.playerTwo?.count
          ? {
              key: 'most-won-player-two',
              isValid: true,
              text: t('compare.head-to-head.most-won-game', {
                player: playerTwo.name,
                opponent: playerOne.name,
                game: gameById(compare.mostWonGame.playerTwo.gameId)?.title,
                count: compare.mostWonGame.playerTwo.count,
              }),
            }
          : null,
        compare.lastWonGame?.gameId && compare.lastWonGame?.playerId
          ? {
              key: 'last-session',
              isValid: true,
              text: t('compare.head-to-head.last-session', {
                player: compare.lastWonGame.playerId === playerOne.id ? playerOne.name : playerTwo.name,
                game: gameById(compare.lastWonGame.gameId)?.title,
              }),
            }
          : null,
        compare.preferredGame?.gameId && compare.preferredGame?.sessionCount
          ? {
              key: 'most-played-together',
              isValid: true,
              text: t('compare.head-to-head.most-played-together', {
                game: gameById(compare.preferredGame.gameId)?.title,
                count: compare.preferredGame.sessionCount,
              }),
            }
          : null,
        compare.closestGame?.gameId && compare.closestGame?.playerId && compare.closestGame?.scoringDifference != null
          ? {
              key: 'closest-game',
              isValid: true,
              text: t('compare.head-to-head.closest-game', {
                player: compare.closestGame.playerId === playerOne.id ? playerOne.name : playerTwo.name,
                points: compare.closestGame.scoringDifference,
                game: gameById(compare.closestGame.gameId)?.title,
              }),
            }
          : null,
        compare.firstGameTogether?.gameId && compare.firstGameTogether?.startDate
          ? {
              key: 'first-game-together',
              isValid: true,
              text: t('compare.head-to-head.first-game-together', {
                game: gameById(compare.firstGameTogether.gameId)?.title,
                date: toDisplay(compare.firstGameTogether.startDate, dateFormat, uiLanguage),
              }),
            }
          : null,
      ].filter((stat): stat is { key: string; isValid: boolean; text: string } => stat !== null),
    [t, playerOne, playerTwo, compare, gameById, dateFormat, uiLanguage]
  );

  return (
    <div>
      <BgtHeading size="6" className="text-white pb-6 text-center">
        {t('compare.head-to-head.title')}
      </BgtHeading>
      <div className="space-y-3 max-w-3xl mx-auto">
        {headToHeadStats.map((stat) => {
          return (
            <BgtCard key={stat.key}>
              <BgtText color="white" opacity={90} className="text-center">
                {stat.text}
              </BgtText>
            </BgtCard>
          );
        })}
      </div>
    </div>
  );
};

HeadToHeadComponent.displayName = 'HeadToHead';

export const HeadToHead = memo(HeadToHeadComponent);
