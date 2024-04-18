import i18next from 'i18next';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {Button} from '@radix-ui/themes';

import {BgtCard} from '../../components/BgtCard/BgtCard';
import {BgtDetailHeader} from '../../components/BgtDetailHeader/BgtDetailHeader';
import {BgtPage} from '../../components/BgtLayout/BgtPage';
import {BgtPageContent} from '../../components/BgtLayout/BgtPageContent';
import {BgtDeleteModal} from '../../components/Modals/BgtDeleteModal';
import {useGame} from '../../hooks/useGame';
import {PlayerCountChart} from './components/charts/PlayerCountChart';
import {PlayerScoringChart} from './components/charts/PlayerScoringChart';
import {PlaysByWeekDayChart} from './components/charts/PlaysByWeekDayChart';
import {ScoringRankChart} from './components/charts/ScoringRankChart';
import {GameCharts} from './components/GameCharts';
import {GameDetailsPopup} from './components/GameDetailsPopup';
import {GamePlays} from './components/GamePlays';
import {GameStatistics} from './components/GameStatistics';
import {GameTopPlayers} from './components/GameTopPlayers';

export const GameDetailPage = () => {
  const { id } = useParams();
  const { game, deleteGame } = useGame(id);
  const { t } = useTranslation();
  const [openDetailsModal, setOpenDetailsModal] = useState(false);
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const navigate = useNavigate();

  const deleteGameInternal = () => {
    void deleteGame()
      .then(() => {
        navigate('/games');
      })
      .finally(() => {
        setOpenDeleteModal(false);
      })
  }

  if (game === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent className='flex flex-col gap-8'>
        <BgtDetailHeader
          title={game.title}
          imageSrc={game.image}
          navigateBackUrl={'/games'}
          actions={
            <>
              <Button disabled variant='outline'>{i18next.format(t('game.add'), 'capitalize')}</Button>
              <Button variant='outline' onClick={() => setOpenDetailsModal(true)}>{i18next.format(t('common.details'), 'capitalize')}</Button>
              <Button disabled variant='outline'>{i18next.format(t('common.edit'), 'capitalize')}</Button>
              <Button variant='outline' color='red' onClick={() => setOpenDeleteModal(true)}>{i18next.format(t('common.delete.button'), 'capitalize')}</Button>
            </>
          }
        />

        <GameStatistics />

        <div className='grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-8'>
          <PlaysByWeekDayChart />
          <PlayerCountChart />
          <PlayerScoringChart />
          <ScoringRankChart />
        </div>
        <div className='grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-8'>
          <GameTopPlayers />
          <GamePlays />
        </div>
        <GameDetailsPopup open={openDetailsModal} setOpen={setOpenDetailsModal} id={id} />
        <BgtDeleteModal
          title={game.title}
          open={openDeleteModal}
          setOpen={setOpenDeleteModal}
          onDelete={deleteGameInternal}
        />
      </BgtPageContent>
    </BgtPage >
  )
}
