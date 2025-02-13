import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import { usePage } from '../../hooks/usePage';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtPlayerImageCard } from '../../components/BgtImageCard/BgtImageCard';

import { CreatePlayerModal } from './modals/CreatePlayerModal';
import { usePlayers } from './hooks/usePlayers';

export const PlayersPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const { players } = usePlayers({});

  if (players.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ content: 'player.new.button', variant: 'solid', onClick: () => setOpenModal(true) }]}
      />
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-12">
          {players.data.map((x) => (
            <BgtPlayerImageCard key={x.id} id={x.id} title={x.name} image={x.image} link={`/players/${x.id}`} />
          ))}
        </div>
        <CreatePlayerModal open={openModal} setOpen={setOpenModal} />
      </BgtPageContent>
    </BgtPage>
  );
};
