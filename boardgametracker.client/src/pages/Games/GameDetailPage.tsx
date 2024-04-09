import i18next from 'i18next';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {ArrowLeftCircleIcon} from '@heroicons/react/24/outline';
import {Button, Text} from '@radix-ui/themes';

import {BgtIcon} from '../../components/BgtIcon/BgtIcon';
import {BgtCard} from '../../components/BgtLayout/BgtCard';
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
        <div className='fixed z-50 top-18 md:top-4'>
          <Button size="2" variant="soft" className='hover:cursor-pointer' onClick={() => navigate('/games')}>
            <BgtIcon icon={<ArrowLeftCircleIcon />} size={22} />
          </Button>
        </div>
        <div className='flex items-center justify-start gap-3 flex-col'>
          <div className='max-w-24 md:max-w-56'>
            <img src={`/${game.image}`} className='rounded-md object-fill md:border-orange-600 md:border-2 w-60 mt-4' />
          </div>
          <div className='flex flex-col gap-3 items-center'>
            <Text size="8" weight="bold">{game.title}</Text>
          </div>
          <div className='flex flex-row justify-center md:justify-end items-end gap-3 flex-grow h-full'>
            <Button variant='outline'>{i18next.format(t('game.add'), 'capitalize')}</Button>
            <Button variant='outline' onClick={() => setOpenDetailsModal(true)}>{i18next.format(t('common.details'), 'capitalize')}</Button>
            <Button variant='outline'>{i18next.format(t('common.edit'), 'capitalize')}</Button>
            <Button variant='outline' color='red' onClick={() => setOpenDeleteModal(true)}>{i18next.format(t('common.delete'), 'capitalize')}</Button>
          </div>
        </div>

        <GameStatistics />

        <div className='flex flex-col 2xl:flex-row gap-3'>
          <GameTopPlayers />
          <GamePlays />
        </div>
        <GameDetailsPopup open={openDetailsModal} setOpen={setOpenDetailsModal} id={id} />
        <BgtDeleteModal
          title={t('game.delete.title', { title: game.title })}
          content={t('game.delete.description', { title: game.title })}
          open={openDeleteModal}
          setOpen={setOpenDeleteModal}
          onDelete={deleteGameInternal}
        />
      </BgtPageContent>
    </BgtPage>
  )
}
