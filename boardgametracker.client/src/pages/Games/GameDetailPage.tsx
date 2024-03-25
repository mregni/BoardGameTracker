import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {Button} from '@radix-ui/themes';

import {BgtCard} from '../../components/BgtLayout/BgtCard';
import {BgtPage} from '../../components/BgtLayout/BgtPage';
import {BgtPageContent} from '../../components/BgtLayout/BgtPageContent';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import {useGame} from '../../hooks/useGame';
import {GameCategories} from './components/GameCategories';
import {GameDetails} from './components/GameDetails';
import {GameMechanics} from './components/GameMechanics';
import {GamePlays} from './components/GamePlays';
import {GameTopPlayers} from './components/GameTopPlayers';

export const GameDetailPage = () => {
  const { id } = useParams();
  const { game } = useGame(id);
  const { t } = useTranslation();

  if (game === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={game.title}
        hasBackButton
        backUrl='/games'
      >
        <Button variant='classic' disabled color='red' onClick={() => prompt("boe")}>Edit</Button>
      </BgtPageHeader>
      <BgtPageContent className='grid gap-3 grid-cols-1 md:grid-cols-3 xl:grid-cols-5'>
        <BgtCard transparant cardStyle='md:col-span-1'>
          <img src={`/${game.image}`} className='rounded-md object-fill w-full' />
        </BgtCard>
        <GameDetails />
        <div className='flex flex-col gap-3 md:col-span-3 xl:col-span-2'>
          <BgtCard title={t('games.cards.categories')}>
            <GameCategories />
          </BgtCard>
          <BgtCard title={t('games.cards.mechanics')}>
            <GameMechanics />
          </BgtCard>
        </div>
        <BgtCard
          title={t('games.cards.games')}
          transparant
          cardStyle='md:col-span-3 xl:col-span-3'>
          <GamePlays />
        </BgtCard>
        <BgtCard
          title={t('games.cards.top-players')}
          cardStyle='md:col-span-3 lg:col-span-2 3xl:col-span-1'
          contentStyle='bg-gradient-to-b from-sky-600 to-sky-900'>
          <GameTopPlayers />
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  )
}
