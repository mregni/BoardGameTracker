import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { Button } from '@radix-ui/themes';

import { usePlayers } from '../../hooks/usePlayers';
import { usePage } from '../../hooks/usePage';
import { useCounts } from '../../hooks/useCounts';
import { BgtPlayerModal } from '../../components/Modals/BgtPlayerModal';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtImageCard } from '../../components/BgtImageCard/BgtImageCard';
import { BgtCard } from '../../components/BgtCard/BgtCard';

export const PlayersPage = () => {
  const { t } = useTranslation();
  const { pageTitle, activePage } = usePage();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const { counts } = useCounts();
  const { players } = usePlayers();

  if (!counts || !players) return null;

  const itemCount = counts.find((x) => x.key == activePage)?.value;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        subHeader={t('common.items', { count: itemCount })}
        actions={[
          { content: 'player.new.button', variant: 'solid', onClick: () => setOpenModal(true) },
          { content: 'player.edit', variant: 'classic', onClick: () => prompt('boe') },
        ]}
      />
      <BgtPageContent>
        <div className="grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-12">
          {players.map((x) => (
            <Link key={x.id} to={`/players/${x.id}`}>
              <BgtImageCard title={x.name} image={x.image} />
            </Link>
          ))}
        </div>
        <BgtPlayerModal open={openModal} setOpen={setOpenModal} />
      </BgtPageContent>
    </BgtPage>
  );
};
