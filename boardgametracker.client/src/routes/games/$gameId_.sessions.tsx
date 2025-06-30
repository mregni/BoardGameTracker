import { useState, useMemo } from 'react';
import { t } from 'i18next';
import { format } from 'date-fns';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';

import { useGameSessionsData } from './-hooks/useGameSessionsData';

import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getGame, getGameSessions } from '@/services/queries/games';
import { Session } from '@/models';
import { DataTableProps, BgtDataTable } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import { BgtPlayerAvatar } from '@/components/BgtAvatar/BgtPlayerAvatar';

export const Route = createFileRoute('/games/$gameId_/sessions')({
  component: RouteComponent,
  loader: ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getGame(params.gameId));
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getPlayers());
    queryClient.prefetchQuery(getGameSessions(params.gameId));
  },
});

function RouteComponent() {
  const { gameId } = Route.useParams();

  const navigate = useNavigate();
  const { infoToast, errorToast } = useToasts();

  const onDeleteSuccess = () => {
    infoToast('sessions.notifications.deleted');
    setSessionToDelete(null);
  };

  const onDeleteError = () => {
    errorToast('sessions.notifications.delete-failed');
    setSessionToDelete(null);
  };

  const { settings, game, sessions, deleteSession, players } = useGameSessionsData({
    gameId,
    onDeleteSuccess,
    onDeleteError,
  });

  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  const columns: DataTableProps<Session>['columns'] = useMemo(
    () => [
      {
        accessorKey: '0',
        cell: ({ row }) => format(new Date(row.original.start), `${settings?.dateFormat}`),
        header: t('common.date'),
      },
      {
        accessorKey: '1',
        cell: ({ row }) => format(new Date(row.original.start), `${settings?.timeFormat}`),
        header: t('common.time'),
      },
      {
        accessorKey: '2',
        cell: ({ row }) => `${row.original.minutes} ${t('common.minutes', { count: row.original.minutes })}`,
        header: t('common.duration'),
      },
      {
        accessorKey: '3',
        cell: ({ row }) => (
          <div className="flex flex-row gap-1">
            {row.original.playerSessions
              .filter((x) => x.won)
              .map((player) => (
                <BgtPlayerAvatar
                  key={`${player.playerId}_${player.sessionId}`}
                  playerSession={player}
                  game={game}
                  player={players.find((x) => x.id === player.playerId)}
                />
              ))}
          </div>
        ),
        header: t('common.winners'),
      },
      {
        accessorKey: '4',
        cell: ({ row }) => (
          <div className="flex flex-row gap-1">
            {row.original.playerSessions
              .filter((x) => !x.won)
              .map((player) => (
                <BgtPlayerAvatar
                  key={`${player.playerId}_${player.sessionId}`}
                  player={players.find((x) => x.id === player.playerId)}
                  playerSession={player}
                  game={game}
                />
              ))}
          </div>
        ),
        header: t('common.other-players'),
      },
      {
        accessorKey: '5',
        cell: ({ row }) => {
          const highScore = row.original.playerSessions
            .filter((x) => x.score !== undefined)
            .sort((a, b) => b.score! - a.score!);

          if (highScore.length === 0) return '';

          return highScore[0].score;
        },
        header: t('common.high-score'),
      },
      {
        accessorKey: '200',
        cell: ({ row }) => (
          <BgtEditDeleteButtons
            onDelete={() => setSessionToDelete(row.original.id)}
            onEdit={() => navigate({ to: `/sessions/update/${row.original.id}` })}
          />
        ),
        header: <div className="flex justify-end">{t('common.actions')}</div>,
      },
    ],
    [game, navigate, players, settings?.dateFormat, settings?.timeFormat]
  );

  if (game === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        backAction={() => navigate({ to: `/games/${gameId}` })}
        header={`${game.title} - ${t('sessions.title')}`}
        actions={[
          { onClick: () => navigate({ to: `/sessions/new/${gameId}` }), variant: 'solid', content: 'game.add' },
        ]}
      />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={sessions}
            noDataMessage={t('common.no-data-yet')}
            widths={['w-[70px]', 'w-[100px]', 'w-[75px]']}
          />
          <BgtDeleteModal
            open={sessionToDelete !== null}
            close={() => setSessionToDelete(null)}
            onDelete={() => sessionToDelete && deleteSession(sessionToDelete)}
            title={t('sessions.delete.title')}
            description={t('sessions.delete.description')}
          />
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  );
}
