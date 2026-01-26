import { useTranslation } from 'react-i18next';
import { useState, useMemo } from 'react';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useGamesData } from './-hooks/useGamesData';

import { getGames } from '@/services/queries/games';
import CreateGameModal from '@/routes/games/-modals/CreateGameModal';
import { BggGameModal } from '@/routes/games/-modals/BggGameModal';
import { useDebounce } from '@/hooks/useDebounce';
import { BgtText } from '@/components/BgtText/BgtText';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtEmptyState } from '@/components/BgtLayout/BgtEmptyState';
import { BgtCardList } from '@/components/BgtLayout/BgtCardList';
import { BgtImageCard } from '@/components/BgtImageCard/BgtImageCard';
import { SearchInputField } from '@/components/BgtForm';
import { BgtBadge } from '@/components/BgtBadge/BgtBadge';
import Game from '@/assets/icons/gamepad.svg?react';

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

  const debouncedFilterValue = useDebounce(filterValue, 300);

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

  if (games.length === 0) {
    return (
      <BgtPage>
        <BgtPageHeader
          header={t('games.title')}
          actions={[{ onClick: () => setOpenModal(true), variant: 'primary', content: 'games.new' }]}
        />
        <BgtPageContent centered>
          <BgtEmptyState
            icon={Game}
            description={t('dashboard.empty.description')}
            title={t('dashboard.empty.title')}
          />
        </BgtPageContent>
      </BgtPage>
    );
  }

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
        <BgtText size="3" color="primary" className="pb-6" weight="medium">
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
        <BgtCardList>
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
        </BgtCardList>
        <BggGameModal open={openBggModal} setOpen={setOpenBggModal} />
        <CreateGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
}
