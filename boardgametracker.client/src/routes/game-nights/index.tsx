import { useTranslation } from 'react-i18next';
import { useState, useMemo } from 'react';
import { isPast } from 'date-fns';
import { createFileRoute } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';

import { ManageRSVPsModal } from './-modals/ManageRSVPsModal';
import { CreateGameNightModal } from './-modals/CreateGameNightModal';
import { useGameNightModals } from './-hooks/useGameNightModals';
import { useGameNightData } from './-hooks/useGameNightData';
import { useGameNightActions } from './-hooks/useGameNightActions';
import { NoGameNights } from './-components/NoGameNights';
import { GameNightCard } from './-components/GameNightCard';
import { FilterTabs, FilterType } from './-components/FilterTabs';

import { getPlayers } from '@/services/queries/players';
import { getLocations } from '@/services/queries/locations';
import { getGames } from '@/services/queries/games';
import { getGameNights, getGameNightStatistics } from '@/services/queries/gameNights';
import { GameNight } from '@/models';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import Calendar from '@/assets/icons/calendar.svg?react';

export const Route = createFileRoute('/game-nights/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getGameNights());
    queryClient.prefetchQuery(getGameNightStatistics());
    queryClient.prefetchQuery(getPlayers());
    queryClient.prefetchQuery(getGames());
    queryClient.prefetchQuery(getLocations());
  },
});

function RouteComponent() {
  const { t } = useTranslation();
  const { errorToast, successToast } = useToasts();

  const [filter, setFilter] = useState<FilterType>('all');
  const [selectedGameNight, setSelectedGameNight] = useState<GameNight | null>(null);

  const { gameNights, settings, players, games, locations, isLoading, createGameNight, deleteGameNight, updateRsvp, isCreating } =
    useGameNightData({
      onCreateSuccess: () => successToast('game-nights.notifications.created'),
      onCreateError: () => errorToast('game-nights.notifications.create-failed'),
      onDeleteSuccess: () => successToast('game-nights.notifications.deleted'),
      onDeleteError: () => errorToast('game-nights.notifications.delete-failed'),
      onRsvpUpdateSuccess: () => successToast('game-nights.notifications.rsvp-updated'),
      onRsvpUpdateError: () => errorToast('game-nights.notifications.rsvp-update-failed'),
    });

  const modals = useGameNightModals();

  const actions = useGameNightActions({
    createGameNight,
    deleteGameNight,
    selectedGameNight,
    onCreateModalClose: modals.createModal.hide,
    onDeleteModalClose: modals.deleteModal.hide,
    onManageRsvpModalOpen: modals.manageRsvpModal.show,
    setSelectedGameNight,
  });

  const upcomingCount = useMemo(() => gameNights.filter((gn) => !isPast(gn.startDate)).length, [gameNights]);
  const pastCount = useMemo(() => gameNights.filter((gn) => isPast(gn.startDate)).length, [gameNights]);

  const filteredGameNights = useMemo(() => {
    switch (filter) {
      case 'upcoming':
        return gameNights.filter((gn) => !isPast(gn.startDate));
      case 'past':
        return gameNights.filter((gn) => isPast(gn.startDate));
      default:
        return gameNights;
    }
  }, [gameNights, filter]);

  return (
    <BgtPage>
      <BgtPageHeader
        icon={Calendar}
        header={t('game-nights.title')}
        actions={[{ onClick: modals.createModal.show, variant: 'primary', content: 'game-nights.create.button' }]}
      />
      <BgtPageContent isLoading={isLoading} data={{ settings, gameNights }}>
        {({ settings }) => (
          <>
            <FilterTabs
              filter={filter}
              onFilterChange={setFilter}
              allCount={gameNights.length}
              upcomingCount={upcomingCount}
              pastCount={pastCount}
            />

            {filteredGameNights.length === 0 ? (
              <NoGameNights filter={filter} onCreateClick={modals.createModal.show} />
            ) : (
              <div className="space-y-4">
                {filteredGameNights.map((gameNight) => (
                  <GameNightCard
                    key={gameNight.id}
                    gameNight={gameNight}
                    settings={settings}
                    onEdit={actions.handleEditGameNight}
                    onDelete={() => {
                      actions.handleDeleteClick(gameNight);
                      modals.deleteModal.show();
                    }}
                    onManageRsvps={actions.handleManageRsvps}
                  />
                ))}
              </div>
            )}

            <CreateGameNightModal
              open={modals.createModal.isOpen}
              setOpen={modals.createModal.setIsOpen}
              players={players}
              games={games}
              locations={locations}
              isLoading={isCreating}
              onSave={actions.handleCreate}
            />

            <ManageRSVPsModal
              open={modals.manageRsvpModal.isOpen}
              setOpen={modals.manageRsvpModal.setIsOpen}
              gameNight={selectedGameNight}
              onUpdateRsvp={updateRsvp}
              isLoading={isLoading}
            />

            <BgtDeleteModal
              open={modals.deleteModal.isOpen}
              close={modals.deleteModal.hide}
              onDelete={actions.handleDelete}
              title={t('game-nights.delete.title')}
              description={t('game-nights.delete.description', { title: selectedGameNight?.title })}
            />
          </>
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
