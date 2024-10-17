import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import { usePage } from '../../hooks/usePage';
import { BgtPlayerModal } from '../../components/Modals/BgtPlayerModal';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtImageCard } from '../../components/BgtImageCard/BgtImageCard';

import { usePlayersPage } from './hooks/usePlayersPage';

export const PlayersPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const { players } = usePlayersPage();

  if (players.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ content: 'player.new.button', variant: 'solid', onClick: () => setOpenModal(true) }]}
      />
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-12">
          {players.data.list.map((x) => (
            <BgtImageCard key={x.id} title={x.name} image={x.image} link={`/players/${x.id}`} />
          ))}
        </div>
        <BgtPlayerModal open={openModal} setOpen={setOpenModal} />
      </BgtPageContent>
    </BgtPage>
  );
};
