import { useState, useMemo } from 'react';
import { t } from 'i18next';
import { format } from 'date-fns';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';

import { usePlayerSessionData } from './-hooks/usePlayerSessionData';

import { StringToHsl } from '@/utils/stringUtils';
import { getSettings } from '@/services/queries/settings';
import { getPlayer, getPlayers, getPlayerSessions } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import { Session } from '@/models';
import { useToasts } from '@/hooks/useToasts';
import { DataTableProps, BgtDataTable } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import { BgtPlayerAvatar } from '@/components/BgtAvatar/BgtPlayerAvatar';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

export const Route = createFileRoute('/players/$playerId_/sessions')({
  component: RouteComponent,
  loader: ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
    queryClient.prefetchQuery(getGames());
    queryClient.prefetchQuery(getPlayers());
    queryClient.prefetchQuery(getPlayer(params.playerId));
    queryClient.prefetchQuery(getPlayerSessions(params.playerId));
  },
});

function RouteComponent() {
  const { playerId } = Route.useParams();
  const navigate = useNavigate();
  const { infoToast } = useToasts();
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  const onDeleteSuccess = () => {
    infoToast('sessions.notifications.deleted');
    setSessionToDelete(null);
  };

  const { sessions, deleteSession, settings, games, player, players } = usePlayerSessionData({
    playerId,
    onDeleteSuccess,
  });

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
        cell: ({ row }) => {
          const game = games.find((x) => x.id == row.original.gameId);
          return (
            <div className="flex flex-row gap-2 items-center">
              <BgtAvatar
                image={game?.image}
                color={StringToHsl(game?.title)}
                title={game?.title}
                onClick={() => navigate({ to: `/games/${game?.id}` })}
              />
              <span>{game?.title}</span>
            </div>
          );
        },
        header: t('common.game'),
      },
      {
        accessorKey: '3',
        cell: ({ row }) => `${row.original.minutes} ${t(`common.minutes`, { count: row.original.minutes })}`,
        header: t('common.duration'),
      },
      {
        accessorKey: '4',
        cell: ({ row }) => (
          <div className="flex flex-row gap-1">
            {row.original.playerSessions
              .filter((x) => x.won)
              .map((player) => (
                <BgtPlayerAvatar
                  key={`${player.playerId}_${player.sessionId}`}
                  player={players.find((x) => x.id === player.playerId)}
                  playerSession={player}
                  game={games.find((x) => x.id == row.original.gameId)}
                />
              ))}
          </div>
        ),
        header: t('common.winners'),
      },
      {
        accessorKey: '5',
        cell: ({ row }) => (
          <div className="flex flex-row gap-1">
            {row.original.playerSessions
              .filter((x) => !x.won)
              .map((player) => (
                <BgtPlayerAvatar
                  key={`${player.playerId}_${player.sessionId}`}
                  player={players.find((x) => x.id === player.playerId)}
                  playerSession={player}
                  game={games.find((x) => x.id == row.original.gameId)}
                />
              ))}
          </div>
        ),
        header: t('common.other-players'),
      },
      {
        accessorKey: '6',
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
    [games, navigate, settings?.dateFormat, settings?.timeFormat, players]
  );

  if (player === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        backAction={() => navigate({ to: `/games/${playerId}` })}
        header={`${player.name} - ${t(`sessions.title`)}`}
        actions={[{ onClick: () => navigate({ to: `/sessions/new` }), variant: `solid`, content: `game.add` }]}
      />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={sessions}
            noDataMessage={t('common.no-data')}
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
