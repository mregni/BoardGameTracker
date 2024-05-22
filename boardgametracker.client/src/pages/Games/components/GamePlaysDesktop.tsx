import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { createColumnHelper } from '@tanstack/react-table';
import { Badge } from '@radix-ui/themes';
import { InformationCircleIcon, PencilIcon } from '@heroicons/react/24/outline';

import { PlayFlagToString } from '../../../utils/stringUtils';
import { Play } from '../../../models';
import { useGamePlays } from '../../../hooks/usePlays';
import { useLocations } from '../../../hooks/useLocations';
import { BgtDateTimeCell } from '../../../components/BgtTable/BgtDateTimeCell';
import { BgtIconButton } from '../../../components/BgtIconButton/BgtIconButton';
import { BgtTableCard } from '../../../components/BgtCard/BgtTableCard';
import { BgtPlayerAvatar } from '../../../components/BgtAvatar/BgtPlayerAvatar';

const columnHelper = createColumnHelper<Play>();

const editPlay = (id: number) => {
  //TODO: Implement
  console.log('edit: ' + id);
};

const pageCount = 10;

export const DesktopDetails = () => {
  const { id } = useParams();
  const [page, setPage] = useState(0);
  const { plays, totalCount } = useGamePlays(id, page, pageCount);
  const { t } = useTranslation();
  const { byId: locationById } = useLocations();

  const columns = [
    columnHelper.accessor((row) => row.start, {
      minSize: 200,
      id: 'start',
      header: () => <>{t('common.start', {})}</>,
      cell: (info) => <BgtDateTimeCell dateTime={info.getValue()} />,
    }),
    columnHelper.accessor((row) => row.players.filter((x) => x.won).sort((x) => x.score ?? 0), {
      id: 'winners',
      header: () => <>{t('common.winners', {})}</>,
      cell: (info) => (
        <div className="flex gap-1 justify-center flex-wrap">
          {info.getValue().map((player) => (
            <BgtPlayerAvatar key={player.playerId} player={player} />
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
            <BgtPlayerAvatar key={player.playerId} player={player} />
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
    columnHelper.accessor((row) => row.playFlags, {
      id: 'flags',
      header: () => <>{t('common.flags.header')}</>,
      cell: (info) => (
        <div className="flex flex-row justify-center gap-1 flex-wrap">
          {info.getValue().map((flag) => (
            <Badge key={flag} variant="solid">
              {t(PlayFlagToString(flag))}
            </Badge>
          ))}
        </div>
      ),
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
      className="w-full"
      columns={columns}
      plays={plays}
      title={t('player.cards.games')}
      setPage={setPage}
      hasMore={(page + 1) * pageCount < totalCount}
      currentPage={page}
      totalPages={Math.ceil(totalCount / pageCount)}
    />
  );
};
