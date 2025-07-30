import { useTranslation } from 'react-i18next';
import { ReactNode } from 'react';
import { createFileRoute } from '@tanstack/react-router';

import { useCompareData } from './-hooks/useCompareData';
import { WinnerCount } from './-components/WinnerCount';
import { TotalWinPercentage } from './-components/TotalWinPercentage';
import { TotalWinCount } from './-components/TotalWinCount';
import { TotalSessions } from './-components/TotalSessions';
import { TotalDuration } from './-components/TotalDuration';
import { PlayerSpotlight } from './-components/PlayerSpotlight';
import { BestPlayerGame } from './-components/BestPlayerGame';

import { getPlayers } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';

export const Route = createFileRoute('/compare/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getPlayers());
    queryClient.prefetchQuery(getGames());
  },
});

interface RowTableProps {
  left: ReactNode;
  right: ReactNode;
  withDivider?: boolean;
}
const Row = ({ left, right, withDivider }: RowTableProps) => {
  return (
    <div className="flex flex-row w-full gap-2 xl:mx-8">
      <div className="flex justify-end flex-1 text-right">{left}</div>
      <div className="flex justify-center lg:w-[100px] w-[5px] items-center">
        {withDivider && <img src="/images/common/vs.png" alt="Divider" className="lg:w-18 lg:h-28 mb-16" />}
      </div>
      <div className="flex justify-start flex-1">{right}</div>
    </div>
  );
};

function RouteComponent() {
  const { t } = useTranslation();
  const { players, compare, games } = useCompareData({ playerLeft: 2, playerRight: 1 });

  if (!players || !compare) {
    return null;
  }

  const playerLeft = players[4];
  const playerRight = players[3];

  return (
    <BgtPage>
      <BgtPageHeader header={t('compare.title')} actions={[]}></BgtPageHeader>
      <BgtPageContent className="!gap-10 xs:!gap-6 text-sm lg:!gap-3 lg:text-base">
        <Row
          left={<PlayerSpotlight player={playerLeft} />}
          right={<PlayerSpotlight player={playerRight} />}
          withDivider
        />
        <Row
          left={
            <WinnerCount
              row={compare.directWins}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="col-start-2 text-right"
            />
          }
          right={
            <WinnerCount
              row={compare.directWins}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
        />
        <Row
          left={
            <TotalWinCount
              row={compare.winCount}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
          right={
            <TotalWinCount
              row={compare.winCount}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
        />
        <Row
          left={
            <TotalWinPercentage
              row={compare.winPercentageCount}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
          right={
            <TotalWinPercentage
              row={compare.winPercentageCount}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
        />
        <Row
          left={
            <TotalDuration
              row={compare.totalDuration}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
          right={
            <TotalDuration
              row={compare.totalDuration}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
        />
        <Row
          left={
            <TotalSessions
              row={compare.sessionCounts}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
          right={
            <TotalSessions
              row={compare.sessionCounts}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              className="text-left"
            />
          }
        />
        <Row
          left={
            <BestPlayerGame
              games={games}
              position="left"
              playerLeft={playerLeft}
              playerRight={playerRight}
              row={compare.mostWonGame}
            />
          }
          right={
            <BestPlayerGame
              games={games}
              position="right"
              playerLeft={playerLeft}
              playerRight={playerRight}
              row={compare.mostWonGame}
            />
          }
        />
      </BgtPageContent>
    </BgtPage>
  );
}
