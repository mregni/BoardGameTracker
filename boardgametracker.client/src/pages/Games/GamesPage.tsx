import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import CreateGameModal from './modals/CreateGameModal';
import { BggGameModal } from './modals/BggGameModal';
import { useGames } from './hooks/useGames';

import { usePage } from '@/hooks/usePage';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtGameImageCard } from '@/components/BgtImageCard/BgtImageCard';

export const GamesPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const navigate = useNavigate();
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);

  const { games } = useGames({});

  const openManual = () => {
    setOpenModal(false);
    navigate('/games/new');
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
          {games.data.map((x) => (
            <BgtGameImageCard
              key={x.id}
              id={x.id}
              title={x.title}
              image={x.image}
              state={x.state}
              link={`/games/${x.id}`}
            />
          ))}
        </div>
        {openBggModal && <BggGameModal open={openBggModal} setOpen={setOpenBggModal} />}
        <CreateGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
};
