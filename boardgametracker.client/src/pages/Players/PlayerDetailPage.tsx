import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import i18next from 'i18next';
import { Button } from '@radix-ui/themes';

import { usePlayer } from '../../hooks/usePlayer';
import { BgtDeleteModal } from '../../components/Modals/BgtDeleteModal';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtDetailHeader } from '../../components/BgtDetailHeader/BgtDetailHeader';

import { PlayerStatistics } from './components/PlayerStatistics';
import { PlayerPlays } from './components/PlayerPlays';

export const PlayerDetailpage = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { player, deletePlayer } = usePlayer(id);
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const navigate = useNavigate();

  const deletePlayerInternal = () => {
    void deletePlayer()
      .then(() => {
        navigate('/players');
      })
      .finally(() => {
        setOpenDeleteModal(false);
      });
  };

  if (player === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent className="flex flex-col gap-3">
        <BgtDetailHeader
          title={player?.name}
          imageSrc={player.image}
          navigateBackUrl={'/players'}
          actions={
            <>
              <Button variant="outline" color="red" onClick={() => setOpenDeleteModal(true)}>
                {i18next.format(t('common.delete.button'), 'capitalize')}
              </Button>
            </>
          }
        />

        <PlayerStatistics />

        <PlayerPlays />

        <BgtDeleteModal title={player.name} open={openDeleteModal} setOpen={setOpenDeleteModal} onDelete={deletePlayerInternal} />
      </BgtPageContent>
    </BgtPage>
  );
};
