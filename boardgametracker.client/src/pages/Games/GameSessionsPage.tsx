import { useNavigate, useParams } from 'react-router-dom';
import { useMemo, useState } from 'react';
import { t } from 'i18next';
import { format } from 'date-fns';

import { useGameSessionsPage } from './hooks/useGameSessionsPage';
import { useGame } from './hooks/useGame';

import { StringToHsl } from '@/utils/stringUtils';
import { Game, PlayerSession, Session } from '@/models';
import { useToasts } from '@/hooks/useToasts';
import { useSettings } from '@/hooks/useSettings';
import { usePlayerById } from '@/hooks/usePlayerById';
import { BgtDeleteModal } from '@/components/Modals/BgtDeleteModal';
import { BgtDataTable, DataTableProps } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import { BgtPlayerAvatar } from '@/components/BgtAvatar/BgtPlayerAvatar';

export const GameSessionsPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { infoToast } = useToasts();
  const { settings } = useSettings();
  const { game } = useGame({ id });

  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  const onDeleteSuccess = () => {
    infoToast('sessions.notifications.deleted');
    setSessionToDelete(null);
  };

  const { sessions, deleteSession } = useGameSessionsPage({ id, onDeleteSuccess });

  const columns: DataTableProps<Session>['columns'] = useMemo(
    () => [
      {
        accessorKey: '0',
        cell: ({ row }) => format(new Date(row.original.start), `${settings.data?.dateFormat}`),
        header: t('common.date'),
      },
      {
        accessorKey: '1',
        cell: ({ row }) => format(new Date(row.original.start), `${settings.data?.timeFormat}`),
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
                  game={game.data}
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
                  playerSession={player}
                  game={game.data}
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
            onEdit={() => navigate(`/sessions/update/${row.original.id}`)}
          />
        ),
        header: <div className="flex justify-end">{t('common.actions')}</div>,
      },
    ],
    [game.data, navigate, settings.data?.dateFormat, settings.data?.timeFormat]
  );

  if (game.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        backAction={() => navigate(`/games/${id}`)}
        header={`${game.data.title} - ${t('sessions.title')}`}
        actions={[
          { onClick: () => navigate(`/sessions/create/${game.data.id}`), variant: 'solid', content: 'game.add' },
        ]}
      />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={sessions.data ?? []}
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
};
