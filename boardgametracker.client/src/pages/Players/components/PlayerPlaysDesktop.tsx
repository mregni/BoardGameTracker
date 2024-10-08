import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { createColumnHelper } from '@tanstack/react-table';
import { InformationCircleIcon, PencilIcon } from '@heroicons/react/24/outline';

import { Play } from '../../../models';
import { usePlayerSessions } from '../../../hooks/useSessions';
import { useLocations } from '../../../hooks/useLocations';
import { useGames } from '../../../hooks/useGames';
import { BgtDateTimeCell } from '../../../components/BgtTable/BgtDateTimeCell';
import { BgtIconButton } from '../../../components/BgtIconButton/BgtIconButton';
import { BgtTableCard } from '../../../components/BgtCard/BgtTableCard';
import { BgtPlayerAvatar } from '../../../components/BgtAvatar/BgtPlayerAvatar';
import { BgtAvatar } from '../../../components/BgtAvatar/BgtAvatar';

const columnHelper = createColumnHelper<Play>();

const editPlay = (id: number) => {
  //TODO: Implement
  console.log('edit: ' + id);
};

const pageCount = 10;

export const PlayerPlaysDesktop = () => {
  const { id } = useParams();
  const [page, setPage] = useState(0);
  const { playSessions: plays, totalCount } = usePlayerSessions(id, page, pageCount);
  const { t } = useTranslation();
  const { byId: locationById } = useLocations();
  const { byId: gameById } = useGames();
  const navigate = useNavigate();

  const columns = [
    columnHelper.accessor((row) => row.start, {
      minSize: 200,
      id: 'start',
      header: () => <>{t('common.start', {})}</>,
      cell: (info) => <BgtDateTimeCell dateTime={info.getValue()} />,
    }),
    columnHelper.accessor((row) => row.gameId, {
      id: 'game',
      header: () => <>{t('common.game', {})}</>,
      cell: (info) => (
        <div
          className="flex gap-2 justify-left flex-row hover:cursor-pointer"
          onClick={() => navigate(`/games/${gameById(info.getValue())?.id}`)}
        >
          <BgtAvatar
            title={gameById(info.getValue())?.title}
            image={gameById(info.getValue())?.image}
            key={info.getValue()}
            noTooltip
          />
          <div>
            <span className="line-clamp-1 text-left">{gameById(info.getValue())?.title}</span>
          </div>
        </div>
      ),
    }),
    columnHelper.accessor((row) => row.players.filter((x) => x.won).sort((x) => x.score ?? 0), {
      id: 'winners',
      header: () => <>{t('common.winners', {})}</>,
      cell: (info) => (
        <div className="flex gap-1 justify-center flex-wrap">
          {info.getValue().map((player) => (
            <BgtPlayerAvatar key={player.playerId} playerSession={player} />
          ))}
        </div>
      ),
    }),
    columnHelper.accessor((row) => row.players.filter((x) => !x.won).sort((x) => x.score ?? 0), {
      id: 'others',
      header: () => <>{t('common.loosers')}</>,
      cell: (info) => (
        <div className="flex gap-1 justify-center flex-wrap">
          {info.getValue().map((player) => (
            <BgtPlayerAvatar key={player.playerId} playerSession={player} />
          ))}
        </div>
      ),
    }),
    columnHelper.accessor((row) => row.minutes, {
      id: 'duration',
      header: () => <>{t('common.duration')}</>,
      cell: (info) => (
        <i>
          {info.getValue()} {t('common.minutes_abbreviation')}
        </i>
      ),
      footer: (info) => info.column.id,
    }),
    columnHelper.accessor((row) => row.locationId, {
      id: 'location',
      header: () => <>{t('common.location')}</>,
      cell: (info) => <i>{locationById(info.getValue())?.name}</i>,
      footer: (info) => info.column.id,
    }),
    columnHelper.accessor((row) => row.id, {
      id: 'actions',
      size: 80,
      header: () => <>{t('common.actions')}</>,
      cell: (info) => (
        <div className="flex flex-row justify-center gap-1">
          <BgtIconButton size={17} icon={<InformationCircleIcon />} onClick={() => editPlay(info.getValue())} />
          <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlay(info.getValue())} />
        </div>
      ),
      footer: (info) => info.column.id,
    }),
  ];

  return (
    <BgtTableCard
      columns={columns}
      plays={plays}
      title={t('games.cards.games')}
      setPage={setPage}
      hasMore={(page + 1) * pageCount < totalCount}
      currentPage={page}
      totalPages={Math.ceil(totalCount / pageCount)}
    />
  );
};
