import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';

import CreateGameModal from './modals/CreateGameModal';
import { BggGameModal } from './modals/BggGameModal';
import { useGames } from './hooks/useGames';

import { usePage } from '@/hooks/usePage';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtImageCard } from '@/components/BgtImageCard/BgtImageCard';
import { BgtSimpleInputField } from '@/components/BgtForm/BgtSimpleInputField';

export const GamesPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const navigate = useNavigate();
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);
  const [filterValue, setFilterValue] = useState<string>('');

  const { games } = useGames({});

  const filteredGames = useMemo(() => {
    if (games === undefined) return [];

    if (filterValue === undefined || filterValue.length === 0) {
      return games;
    }

    return games.filter((game) => game.title.toLowerCase().includes(filterValue.toLowerCase()));
  }, [games, filterValue]);

  const openManual = () => {
    setOpenModal(false);
    navigate('/games/new');
  };

  const openBgg = () => {
    setOpenModal(false);
    setOpenBggModal(true);
  };

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ onClick: () => setOpenModal(true), variant: 'solid', content: 'games.new' }]}
      >
        <BgtSimpleInputField
          value={filterValue}
          onChange={(event) => setFilterValue(event.target.value)}
          className="bg-slate-800 w-full  md:w-[160px] xl:w-[300px]"
          placeholder={t('common.filter-name')}
        />
      </BgtPageHeader>
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-2 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-5 xl:grid-cols-8 2xl:grid-cols-10">
          {filteredGames.map((x) => (
            <BgtImageCard key={x.id} title={x.title} image={x.image} state={x.state} link={`/games/${x.id}`} />
          ))}
        </div>
        {openBggModal && <BggGameModal open={openBggModal} setOpen={setOpenBggModal} />}
        <CreateGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
};
