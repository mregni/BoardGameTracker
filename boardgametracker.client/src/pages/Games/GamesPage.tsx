import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Button } from '@radix-ui/themes';

import { BgtCard } from '../../components/BgtCard/BgtCard';
import { BgtImageCard } from '../../components/BgtImageCard/BgtImageCard';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtBggGameModal } from '../../components/Modals/BgtBggGameModal';
import BgtNewGameModal from '../../components/Modals/BgtNewGameModal';
import { useCounts } from '../../hooks/useCounts';
import { useGames } from '../../hooks/useGames';
import { usePage } from '../../hooks/usePage';

export const GamesPage = () => {
  const { t } = useTranslation();
  const { pageTitle, activePage } = usePage();
  const [openModal, setOpenModal] = useState(false);
  const [openBggModal, setOpenBggModal] = useState(false);
  const { counts } = useCounts();
  const { games } = useGames();

  const openManual = () => {
    //TODO: implement
    console.log('open manual => TODO');
  };

  const openBgg = () => {
    setOpenModal(false);
    setOpenBggModal(true);
  };

  if (!counts || !games) return null;

  const itemCount = counts.find((x) => x.key == activePage)?.value;
  return (
    <BgtPage>
      <BgtPageHeader header={t(pageTitle)} subHeader={t('common.items', { count: itemCount })}>
        <Button variant="solid" onClick={() => setOpenModal(true)}>
          New
        </Button>
      </BgtPageHeader>
      <BgtPageContent>
        <BgtCard transparant>
          <div className="grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-10 2xl:grid-cols-12">
            {games.map((x) => (
              <Link key={x.id} to={`/games/${x.id}`}>
                <BgtImageCard title={x.title} image={x.image} state={x.state} />
              </Link>
            ))}
          </div>
        </BgtCard>
        {openBggModal && <BgtBggGameModal open={openBggModal} setOpen={setOpenBggModal} />}
        <BgtNewGameModal open={openModal} setOpen={setOpenModal} openBgg={openBgg} openManual={openManual} />
      </BgtPageContent>
    </BgtPage>
  );
};
