import i18next from 'i18next';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {Button} from '@radix-ui/themes';

import {BgtDetailHeader} from '../../components/BgtDetailHeader/BgtDetailHeader';
import {BgtPage} from '../../components/BgtLayout/BgtPage';
import {BgtPageContent} from '../../components/BgtLayout/BgtPageContent';
import {BgtDeleteModal} from '../../components/Modals/BgtDeleteModal';
import {useGame} from '../../hooks/useGame';
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
      <BgtPageContent className='flex flex-col gap-3'>
        <BgtDetailHeader
          title={game.title}
          imageSrc={game.image}
          navigateBackUrl={'/games'}
          actions={
            <>
              <Button variant='outline'>{i18next.format(t('game.add'), 'capitalize')}</Button>
              <Button variant='outline' onClick={() => setOpenDetailsModal(true)}>{i18next.format(t('common.details'), 'capitalize')}</Button>
              <Button variant='outline'>{i18next.format(t('common.edit'), 'capitalize')}</Button>
              <Button variant='outline' color='red' onClick={() => setOpenDeleteModal(true)}>{i18next.format(t('common.delete.button'), 'capitalize')}</Button>
            </>
          }
        />

        <GameStatistics />

        <div className='flex flex-col 2xl:flex-row gap-3'>
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
    </BgtPage>
  )
}
