import { useTranslation } from 'react-i18next';
import { createFileRoute } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';

import { EditPlayerModal } from './-modals/EditPlayerModal';
import { usePlayerModals } from './-hooks/usePlayerModals';
import { usePlayerData } from './-hooks/usePlayerData';
import { usePlayerActions } from './-hooks/usePlayerActions';
import { RecentPlayerSessionsCard } from './-components/RecentPlayerSessionsCard';
import { PlayerWinRecordCard } from './-components/PlayerWinRecordCard';
import { PlayerStatisticsGrid } from './-components/PlayerStatisticsGrid';
import { PlayerHeroSection } from './-components/PlayerHeroSection';
import { PlayerHeader } from './-components/PlayerHeader';
import { PlayerAchievementsCard } from './-components/PlayerAchievementsCard';
import { MostPlayedGamesCard } from './-components/MostPlayedGamesCard';

import { playerIdParamSchema } from '@/utils/routeSchemas';
import { getPlayer, getPlayerStatistics } from '@/services/queries/players';
import { getBadges } from '@/services/queries/basdges';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';

export const Route = createFileRoute('/players/$playerId')({
  component: RouteComponent,
  params: playerIdParamSchema,
  loader: ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getPlayer(params.playerId));
    queryClient.prefetchQuery(getPlayerStatistics(params.playerId));
    queryClient.prefetchQuery(getBadges());
  },
});

function RouteComponent() {
  const { playerId } = Route.useParams();
  const { t } = useTranslation();
  const { infoToast, errorToast } = useToasts();

  const { player, statistics, deletePlayer, badges, sessions, settings, isLoading } = usePlayerData({
    playerId,
    onDeleteError: () => errorToast('player.delete.failed'),
    onDeleteSuccess: () => infoToast('player.delete.successfull'),
  });

  const modals = usePlayerModals();

  const actions = usePlayerActions({
    playerId,
    deletePlayer,
    onDeleteModalClose: modals.deleteModal.hide,
  });

  return (
    <BgtPage>
      <BgtPageHeader backAction={actions.handleBackToPlayers} backText={t('player.back')} />
      <BgtPageContent isLoading={isLoading} data={{ player, statistics, badges, settings }}>
        {({ player, statistics, badges, settings }) => (
          <>
            <PlayerHeader playerName={player.name} onDelete={modals.deleteModal.show} onEdit={modals.editModal.show} />
            <PlayerHeroSection player={player} />
            {statistics.playCount !== 0 && (
              <>
                <PlayerStatisticsGrid statistics={statistics} settings={settings} />
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 xl:gap-6">
                  <div className="flex flex-col gap-3 xl:gap-6">
                    <RecentPlayerSessionsCard
                      sessions={sessions}
                      playerId={playerId}
                      dateFormat={settings.dateFormat}
                      uiLanguage={settings.uiLanguage}
                    />
                    <MostPlayedGamesCard games={statistics.mostPlayedGames} />
                  </div>
                  <div className="flex flex-col gap-3 xl:gap-6">
                    <PlayerWinRecordCard total={statistics.playCount} wins={statistics.winCount} />
                    <PlayerAchievementsCard playerBadges={player.badges} badges={badges} />
                  </div>
                </div>
              </>
            )}
            <BgtDeleteModal
              title={player.name}
              open={modals.deleteModal.isOpen}
              close={modals.deleteModal.hide}
              onDelete={actions.handleDelete}
              description={t('player.delete.description', { name: player.name })}
            />
            {modals.editModal.isOpen && (
              <EditPlayerModal open={modals.editModal.isOpen} setOpen={modals.editModal.hide} player={player} />
            )}
          </>
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
