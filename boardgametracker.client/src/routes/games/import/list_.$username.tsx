import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';
import { cx } from 'class-variance-authority';
import { createFileRoute } from '@tanstack/react-router';

import { useList } from './-hooks/useList';
import { ImportLoader } from './-components/ImportLoader';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { getSettings } from '@/services/queries/settings';
import { getBggCollection, getGames } from '@/services/queries/games';
import { useToasts } from '@/routes/-hooks/useToasts';
import { GameState, ImportGame } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtPaging } from '@/components/BgtTable/BgtPaging';
import { BgtDataTable, DataTableProps } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtSimpleSwitch } from '@/components/BgtForm/BgtSimpleSwitch';
import { BgtSimpleSelect } from '@/components/BgtForm/BgtSimpleSelect';
import { BgtSimpleInputField } from '@/components/BgtForm/BgtSimpleInputField';
import { BgtSimpleCheckbox } from '@/components/BgtForm/BgtSimpleCheckbox';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import LinkIcon from '@/assets/icons/arrow-square-out.svg?react';

export const Route = createFileRoute('/games/import/list_/$username')({
  component: RouteComponent,
  loader: async ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getBggCollection(params.username));
    queryClient.prefetchQuery(getGames());
    queryClient.prefetchQuery(getSettings());
  },
});

const countPerPage = 50;
const maxImport = 5;

function RouteComponent() {
  const { username } = Route.useParams();
  const { t } = useTranslation();
  const { errorToast, successToast } = useToasts();
  const [loadText, setLoadText] = useState('games.import.loading');

  const onSuccessImport = () => {
    successToast(t('games.import.success'));
    setLoadText('games.import.loading');
  };

  const onFailedImport = () => {
    errorToast(t('games.import.failed'));
    setLoadText('games.import.loading');
  };

  const {
    statusCode,
    settings,
    games,
    updateGame,
    filterCollected,
    setFilterCollected,
    inCollectionCount,
    processingGames,
    totalCount,
    startImport,
    importing,
  } = useList({
    username,
    onSuccessImport,
    onFailedImport,
  });

  const [page, setPage] = useState<number>(0);

  const columns: DataTableProps<ImportGame>['columns'] = useMemo(
    () => [
      {
        accessorKey: '0',
        cell: ({ row }) => (
          <BgtSimpleCheckbox
            id={''}
            disabled={row.original.inCollection}
            label={''}
            checked={row.original.checked}
            onCheckedChange={(state) => {
              updateGame(row.original.bggId, { checked: state });
            }}
          />
        ),
        header: '',
      },
      {
        accessorKey: '1',
        cell: ({ row }) => (
          <BgtAvatar
            title={row.original.title}
            image={row.original.imageUrl}
            size="large"
            disabled={row.original.inCollection}
          />
        ),
        header: '',
      },
      {
        accessorKey: '2',
        cell: ({ row }) => <div className={cx(row.original.inCollection && 'text-gray-500')}>{row.original.title}</div>,
        header: t('common.name'),
      },
      {
        accessorKey: '3',
        cell: ({ row }) => (
          <div>
            <span
              className="underline text-blue-700 cursor-pointer flex flex-row gap-1"
              onClick={() =>
                window.open(
                  `https://boardgamegeek.com/boardgame/${row.original.bggId}`,
                  '_blank',
                  'noopener noreferrer'
                )
              }
            >
              {row.original.bggId} <LinkIcon className="size-4" />
            </span>
          </div>
        ),
        header: t('common.name'),
      },
      {
        accessorKey: '4',
        cell: ({ row }) => (
          <BgtSimpleInputField
            type="number"
            value={row.original.price}
            onChange={(event) => updateGame(row.original.bggId, { price: Number(event.target.value) })}
            placeholder={t('game.price.placeholder')}
            disabled={row.original.inCollection}
            className="w-[130px]"
            prefixLabel={settings?.currency}
          />
        ),
        header: t('game.price.label'),
      },
      {
        accessorKey: '5',
        cell: ({ row }) => (
          <BgtSimpleSelect
            value={row.original.state.toString()}
            onChange={(value) => updateGame(row.original.bggId, { state: Number(value) })}
            disabled={row.original.inCollection}
            items={Object.keys(GameState)
              .filter((value) => !Number.isNaN(Number(value)))
              .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: Number(value) }))}
          />
        ),
        header: t('common.state'),
      },
      {
        accessorKey: '6',
        cell: ({ row }) => (
          <BgtSimpleSwitch
            label={t('game.scoring.label')}
            value={row.original.hasScoring}
            disabled={row.original.inCollection}
            onChange={(value) => updateGame(row.original.bggId, { hasScoring: value })}
          />
        ),
        header: t('common.scoring'),
      },
      {
        accessorKey: '7',
        cell: ({ row }) => (
          <BgtSimpleInputField
            value={row.original.addedDate}
            onChange={(event) => updateGame(row.original.bggId, { addedDate: new Date(event.target.value) })}
            type="date"
            disabled={row.original.inCollection}
          />
        ),
        header: t('game.added-date.label'),
      },
    ],
    [settings?.currency, t, updateGame]
  );

  const checkedCount = useMemo(() => {
    return games.filter((game) => game.checked).length;
  }, [games]);

  const triggerImport = () => {
    setLoadText('games.import.importing');
    startImport(games.filter((game) => game.checked));
  };

  return (
    <BgtPage>
      <BgtPageHeader header={t('bgg-import.title')} actions={[]} />
      <BgtPageContent>
        <ImportLoader show={() => statusCode !== 200 || processingGames || importing} text={t(loadText)}>
          <div className="flex flex-row justify-between gap-4 mb-16">
            <div className="flex flex-col gap-2 flex-1">
              <BgtText>
                {t('games.import.intro', { count: totalCount, collectionCount: inCollectionCount, maxImport })}
              </BgtText>
              <BgtSimpleSwitch
                label={'Hide games that are in your collection already'}
                value={filterCollected}
                onChange={(value) => setFilterCollected(value)}
              />
            </div>
            <div>
              <BgtButton onClick={() => triggerImport()} disabled={checkedCount > maxImport} variant="primary">
                {t('games.import.start-import', { count: checkedCount, totalCount: maxImport })}
              </BgtButton>
            </div>
          </div>

          <BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />

          <BgtDataTable
            columns={columns}
            data={games.slice(page * countPerPage, (page + 1) * countPerPage)}
            noDataMessage={t('common.no-data')}
            widths={['w-[48px]', 'w-[70px]', null, 'w-[100px]']}
          />

          <BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />
        </ImportLoader>
      </BgtPageContent>
    </BgtPage>
  );
}
