import { useTranslation } from 'react-i18next';
import { useState, useMemo, useCallback } from 'react';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useGamesData } from './-hooks/useGamesData';

import { getGames } from '@/services/queries/games';
import CreateGameModal from '@/routes/games/-modals/CreateGameModal';
import { BggGameModal } from '@/routes/games/-modals/BggGameModal';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';
import { useDebounce } from '@/hooks/useDebounce';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtLoadingSpinner } from '@/components/BgtLoadingSpinner/BgtLoadingSpinner';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtImageCard } from '@/components/BgtImageCard/BgtImageCard';
import { SearchInputField } from '@/components/BgtForm/SearchInputField';
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
  const { games, isLoading, hasNextPage, fetchNextPage, isFetchingNextPage } = useGamesData(true, 15);
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);
  const [filterValue, setFilterValue] = useState<string>('');
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>(category);

  const debouncedFilterValue = useDebounce(filterValue, 300);

  const handleLoadMore = useCallback(() => {
    if (fetchNextPage) {
      fetchNextPage();
    }
  }, [fetchNextPage]);

  const sentinelRef = useInfiniteScroll({
    onLoadMore: handleLoadMore,
    hasMore: hasNextPage ?? false,
    isLoading: isFetchingNextPage ?? false,
    threshold: 500,
  });

  const filteredGames = useMemo(() => {
    let filteredGames = games;
    if (categoryFilter !== undefined) {
      filteredGames = games.filter((game) => game.categories.some((category) => category.name === categoryFilter));
    }

    if (!debouncedFilterValue) {
      return filteredGames;
    }

    return filteredGames.filter((game) => game.title.toLowerCase().includes(debouncedFilterValue.toLowerCase()));
  }, [games, categoryFilter, debouncedFilterValue]);

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
        actions={[{ onClick: () => setOpenModal(true), variant: 'primary', content: 'games.new' }]}
      ></BgtPageHeader>
      <BgtPageContent>
        <div className="flex flex-row gap-3">
          <SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
        </div>
        <BgtText size="3" className="pb-6 text-primary" weight="medium">
          {t('games.count', { count: filteredGames.length })}
        </BgtText>
        {categoryFilter !== undefined && (
          <div className="flex flex-row gap-2 items-center text-sm text-gray-400">
            <div>{t('common.filter')}:</div>
            <BgtBadge color="green" variant="soft" onClose={() => setCategoryFilter(undefined)}>
              {categoryFilter}
            </BgtBadge>
          </div>
        )}
        <div className="grid gap-3 grid-cols-2 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-5 xl:grid-cols-8 2xl:grid-cols-10">
          {filteredGames.map((x) => (
            <BgtImageCard
              key={x.id}
              title={x.title}
              image={x.image}
              state={x.state}
              isLoaned={x.isLoaned}
              link={`/games/${x.id}`}
            />
          ))}
        </div>
        {/* Infinite scroll sentinel */}
        <div ref={sentinelRef} className="h-4" />
        {/* Loading indicator */}
        {isFetchingNextPage && (
          <div className="flex justify-center py-8">
            <BgtLoadingSpinner />
          </div>
        )}
        <BggGameModal open={openBggModal} setOpen={setOpenBggModal} />
        <CreateGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
}
