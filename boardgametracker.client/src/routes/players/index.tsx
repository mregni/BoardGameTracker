import { useTranslation } from 'react-i18next';
import { useState, useMemo } from 'react';
import { createFileRoute } from '@tanstack/react-router';

import { CreatePlayerModal } from './-modals/CreatePlayerModal';
import { usePlayersData } from './-hooks/usePlayersData';

import { getPlayers } from '@/services/queries/players';
import { useDebounce } from '@/hooks/useDebounce';
import { BgtText } from '@/components/BgtText/BgtText';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtEmptyState } from '@/components/BgtLayout/BgtEmptyState';
import { BgtCardList } from '@/components/BgtLayout/BgtCardList';
import { BgtImageCard } from '@/components/BgtImageCard/BgtImageCard';
import { SearchInputField } from '@/components/BgtForm';
import Users from '@/assets/icons/users.svg?react';

export const Route = createFileRoute('/players/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getPlayers());
  },
});

function RouteComponent() {
  const { t } = useTranslation();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const [filterValue, setFilterValue] = useState<string>('');
  const { players, isLoading } = usePlayersData();

  const debouncedFilterValue = useDebounce(filterValue, 300);

  const filteredPlayers = useMemo(() => {
    if (!debouncedFilterValue) {
      return players;
    }

    return players.filter((player) => player.name.toLowerCase().includes(debouncedFilterValue.toLowerCase()));
  }, [players, debouncedFilterValue]);

  if (isLoading) return null;

  if (players.length === 0) {
    return (
      <BgtPage>
        <BgtPageHeader
          header={t('common.players')}
          actions={[{ content: 'player.new.button', variant: 'primary', onClick: () => setOpenModal(true) }]}
        />
        <BgtPageContent centered>
          <BgtEmptyState icon={Users} description={t('player.empty.description')} title={t('player.empty.title')} />
        </BgtPageContent>
      </BgtPage>
    );
  }

  return (
    <BgtPage>
      <BgtPageHeader
        header={t('common.players')}
        actions={[{ content: 'player.new.button', variant: 'primary', onClick: () => setOpenModal(true) }]}
      ></BgtPageHeader>
      <BgtPageContent>
        <SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
        <BgtText size="3" className="pb-6 text-primary" weight="medium">
          {t('player.count', { count: filteredPlayers.length })}
        </BgtText>
        <BgtCardList>
          {filteredPlayers.map((x) => (
            <BgtImageCard key={x.id} title={x.name} image={x.image} link={`/players/${x.id}`} />
          ))}
        </BgtCardList>
        {openModal && <CreatePlayerModal open={openModal} setOpen={setOpenModal} />}
      </BgtPageContent>
    </BgtPage>
  );
}
