import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import { usePage } from '../../hooks/usePage';
import { useGames } from '../../hooks/useGames';
import { useCounts } from '../../hooks/useCounts';
import BgtNewGameModal from '../../components/Modals/BgtNewGameModal';
import { BgtBggGameModal } from '../../components/Modals/BgtBggGameModal';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtImageCard } from '../../components/BgtImageCard/BgtImageCard';

import { useGamesPage } from './hooks/useGamesPage';

export const GamesPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);
  const { games } = useGamesPage();

  const openManual = () => {
    //TODO: implement
    console.log('open manual => TODO');
  };

  const openBgg = () => {
    setOpenModal(false);
    setOpenBggModal(true);
  };

  if (games.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ onClick: () => setOpenModal(true), variant: 'solid', content: 'games.new' }]}
      />
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-2 sm:grid-cols-3 md:grid-cols-3 lg:grid-cols-5 xl:grid-cols-8 2xl:grid-cols-10">
          {games.data.list.map((x) => (
            <BgtImageCard key={x.id} title={x.title} image={x.image} state={x.state} link={`/games/${x.id}`} />
          ))}
        </div>
        {openBggModal && <BgtBggGameModal open={openBggModal} setOpen={setOpenBggModal} />}
        <BgtNewGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
};
