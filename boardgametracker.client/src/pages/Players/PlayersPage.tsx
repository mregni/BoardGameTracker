import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';

import { usePage } from '../../hooks/usePage';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtImageCard } from '../../components/BgtImageCard/BgtImageCard';

import { CreatePlayerModal } from './modals/CreatePlayerModal';
import { usePlayers } from './hooks/usePlayers';

import { BgtSimpleInputField } from '@/components/BgtForm/BgtSimpleInputField';

export const PlayersPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const [filterValue, setFilterValue] = useState<string | undefined>(undefined);
  const { players } = usePlayers({});

  const filteredPlayers = useMemo(() => {
    if (players === undefined) return [];

    if (filterValue === undefined || filterValue.length === 0) {
      return players;
    }

    return players.filter((player) => player.name.toLowerCase().includes(filterValue.toLowerCase()));
  }, [players, filterValue]);

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ content: 'player.new.button', variant: 'solid', onClick: () => setOpenModal(true) }]}
      >
        <BgtSimpleInputField
          value={filterValue}
          onChange={(event) => setFilterValue(event.target.value)}
          className="bg-slate-800 w-full  md:w-[160px] xl:w-[300px]"
          placeholder={t('common.filter-name')}
        />
      </BgtPageHeader>
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-12">
          {filteredPlayers.map((x) => (
            <BgtImageCard key={x.id} title={x.name} image={x.image} link={`/players/${x.id}`} />
          ))}
        </div>
        {openModal && <CreatePlayerModal open={openModal} setOpen={setOpenModal} />}
      </BgtPageContent>
    </BgtPage>
  );
};
