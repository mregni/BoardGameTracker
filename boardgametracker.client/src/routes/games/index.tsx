import { useTranslation } from 'react-i18next';
import { useState, useMemo } from 'react';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useGamesData } from './-hooks/useGamesData';

import { getGames } from '@/services/queries/games';
import CreateGameModal from '@/routes/games/-modals/CreateGameModal';
import { BggGameModal } from '@/routes/games/-modals/BggGameModal';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtImageCard } from '@/components/BgtImageCard/BgtImageCard';
import { BgtSimpleInputField } from '@/components/BgtForm/BgtSimpleInputField';
import { BgtBadge } from '@/components/BgtBadge/BgtBadge';

type GamesFilterSearch = {
  category?: string;
};

export const Route = createFileRoute('/games/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getGames());
  },
  validateSearch: (search: Record<string, unknown>): GamesFilterSearch => {
    return {
      category: search.category ? (search.category as string) : undefined,
    };
  },
});

function RouteComponent() {
  const { category } = Route.useSearch();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { games, isLoading } = useGamesData();
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);
  const [filterValue, setFilterValue] = useState<string>('');
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>(category);

  const filteredGames = useMemo(() => {
    let filteredGames = games;
    if (categoryFilter !== undefined) {
      filteredGames = games.filter((game) => game.categories.map((x) => x.name).includes(categoryFilter));
    }

    if (filterValue === undefined || filterValue.length === 0) {
      return filteredGames;
    }

    return filteredGames.filter((game) => game.title.toLowerCase().includes(filterValue.toLowerCase()));
  }, [games, categoryFilter, filterValue]);

  const openManual = () => {
    setOpenModal(false);
    navigate({ to: '/games/new' });
  };

  const openBgg = () => {
    setOpenModal(false);
    setOpenBggModal(true);
  };

  if (isLoading) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t('games.title')}
        actions={[{ onClick: () => setOpenModal(true), variant: 'solid', content: 'games.new' }]}
      >
        <BgtSimpleInputField
          value={filterValue}
          onChange={(event) => setFilterValue(event.target.value)}
          className="bg-slate-800 w-full md:w-[160px] xl:w-[300px]"
          placeholder={t('common.filter-name')}
        />
      </BgtPageHeader>
      <BgtPageContent>
        {categoryFilter !== undefined ? (
          <div className="flex flex-row gap-2 items-center text-sm text-gray-400">
            <div>{t('common.filter')}:</div>
            <BgtBadge color="green" variant="soft" onClose={() => setCategoryFilter(undefined)}>
              {categoryFilter}
            </BgtBadge>
          </div>
        ) : (
          <></>
        )}
        <div className="grid gap-3 grid-cols-2 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-5 xl:grid-cols-8 2xl:grid-cols-10">
          {filteredGames.map((x) => (
            <BgtImageCard key={x.id} title={x.title} image={x.image} state={x.state} link={`/games/${x.id}`} />
          ))}
        </div>
        <BggGameModal open={openBggModal} setOpen={setOpenBggModal} />
        <CreateGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
}
