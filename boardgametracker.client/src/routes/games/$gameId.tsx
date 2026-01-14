import { useTranslation } from 'react-i18next';
import { createFileRoute } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';

import { ExpansionSelectorModal } from './-modals/ExpansionSelectorModal';
import { useGameModals } from './-hooks/useGameModals';
import { useGameData } from './-hooks/useGameData';
import { useGameActions } from './-hooks/useGameActions';
import { TopPlayersCard } from './-components/TopPlayersCard';
import { SessionCountChartCard } from './-components/SessionCountChartCard';
import { ScoringResultsCard } from './-components/ScoringResultsCard';
import { RecentSessionsCard } from './-components/RecentSessionsCard';
import { PlayerCountChartCard } from './-components/PlayerCountChartCard';
import { GameStatisticsGrid } from './-components/GameStatisticsGrid';
import { GameStaticSection } from './-components/GameStaticSection';
import { GameHeader } from './-components/GameHeader';
import { GameDetailEmptyState } from './-components/GameDetailEmptyState';
import { ExpansionsCard } from './-components/ExpansionsCard';

import { gameIdParamSchema } from '@/utils/routeSchemas';
import { getSettings } from '@/services/queries/settings';
import { getGame, getGameSessionsShortList, getGameStatistics } from '@/services/queries/games';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';

export const Route = createFileRoute('/games/$gameId')({
  component: RouteComponent,
  params: gameIdParamSchema,
  loader: async ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getGame(params.gameId));
    queryClient.prefetchQuery(getGameStatistics(params.gameId));
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getGameSessionsShortList(params.gameId, 5));
  },
});

function RouteComponent() {
  const { gameId } = Route.useParams();
  const { errorToast, successToast } = useToasts();
  const { t } = useTranslation();

  const { game, deleteGame, settings, statistics, sessions, deleteExpansion, isLoading } = useGameData({
    gameId,
    onDeleteError: () => errorToast('game.delete.failed'),
    onDeleteSuccess: () => successToast('game.delete.successfull'),
    onDeleteExpansionSuccess: () => successToast('expansions.delete.successfull'),
    onDeleteExpansionError: () => errorToast('expansions.delete.failed'),
  });

  const modals = useGameModals();

  const actions = useGameActions({
    gameId,
    deleteGame,
    deleteExpansion,
    onDeleteModalClose: modals.deleteModal.hide,
    onExpansionModalOpen: modals.expansionModal.show,
  });

  return (
    <BgtPage>
      <BgtPageHeader backAction={actions.handleBackToGames} backText={t('games.back')} />
      <BgtPageContent isLoading={isLoading} data={{ game, settings, statistics, sessions }}>
        {({ game, settings, statistics, sessions }) => (
          <>
            <GameHeader
              gameTitle={game.title}
              gameState={game.state}
              isLoaned={game.isLoaned}
              onAddSession={actions.handleAddSession}
              onEdit={actions.handleEdit}
              onDelete={modals.deleteModal.show}
            />
            <GameStaticSection
              game={game}
              playCount={statistics.gameStats.playCount}
              currency={settings.currency}
              dateFormat={settings.dateFormat}
              uiLanguage={settings.uiLanguage}
            />
            {statistics.gameStats.playCount === 0 && <GameDetailEmptyState onLogSession={actions.handleAddSession} />}
            {statistics.gameStats.playCount !== 0 && (
              <>
                <GameStatisticsGrid
                  gameStats={statistics.gameStats}
                  expansionCount={game.expansions.length}
                  currency={settings.currency}
                  dateFormat={settings.dateFormat}
                  uiLanguage={settings.uiLanguage}
                />
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 xl:gap-6">
                  <div className="flex flex-col gap-3 xl:gap-6">
                    <TopPlayersCard topPlayers={statistics.topPlayers} />
                    <RecentSessionsCard
                      sessions={sessions}
                      dateFormat={settings.dateFormat}
                      uiLanguage={settings.uiLanguage}
                      gameId={gameId.toString()}
                    />
                    <SessionCountChartCard playByDayChart={statistics.playByDayChart} />
                  </div>
                  <div className="flex flex-col gap-3 xl:gap-6">
                    <ScoringResultsCard scoreRankChart={statistics.scoreRankChart} />
                    <PlayerCountChartCard playerCountChart={statistics.playerCountChart} />
                    <ExpansionsCard
                      expansions={game.expansions}
                      onAddExpansion={actions.handleAddExpansion}
                      onDeleteExpansion={actions.handleDeleteExpansion}
                    />
                  </div>
                </div>
              </>
            )}
            <BgtDeleteModal
              title={game.title}
              open={modals.deleteModal.isOpen}
              close={modals.deleteModal.hide}
              onDelete={actions.handleDelete}
              description={t('game.delete.description', { title: game.title })}
            />
            {modals.expansionModal.isOpen && (
              <ExpansionSelectorModal
                open={modals.expansionModal.isOpen}
                setOpen={modals.expansionModal.hide}
                gameId={gameId}
                selectedExpansions={game.expansions.map((x) => x.bggId)}
              />
            )}
          </>
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
