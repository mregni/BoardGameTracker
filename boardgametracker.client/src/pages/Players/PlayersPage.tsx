import {useState} from 'react';
import {useTranslation} from 'react-i18next';

import {Button} from '@radix-ui/themes';

import {BgtImageCard} from '../../components/BgtImageCard/BgtImageCard';
import {BgtCard} from '../../components/BgtLayout/BgtCard';
import {BgtPage} from '../../components/BgtLayout/BgtPage';
import {BgtPageContent} from '../../components/BgtLayout/BgtPageContent';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import {BgtPlayerModal} from '../../components/Modals/BgtPlayerModal';
import {useCounts} from '../../hooks/useCounts';
import {usePage} from '../../hooks/usePage';
import {usePlayers} from '../../hooks/usePlayers';

export const PlayersPage = () => {
  const { t } = useTranslation();
  const { pageTitle, activePage } = usePage();
  const [openModal, setOpenModal] = useState<boolean>(false);
  const { counts } = useCounts();
  const { players } = usePlayers();

  if (!counts || !players) return null;

  const itemCount = counts.find(x => x.key == activePage)?.value;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        subHeader={t('common.items', { count: itemCount })}
      >
        <Button variant='solid' onClick={() => setOpenModal(true)}>New</Button>
        <Button variant='classic' disabled color='red' onClick={() => prompt("boe")} className='radix-disabled:cursor-not-allowed'>Edit</Button>
      </BgtPageHeader>
      <BgtPageContent>
        <BgtCard transparant>
          <div className='grid gap-3 grid-cols-3 sm:grid-cols-4 md:grid-cols-4 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-12'>
            {
              players.map(x => <BgtImageCard key={x.id} title={x.name} image={x.image} />)
            }
          </div>
        </BgtCard>
        <BgtPlayerModal open={openModal} setOpen={setOpenModal} />
      </BgtPageContent>
    </BgtPage>
  )
}
