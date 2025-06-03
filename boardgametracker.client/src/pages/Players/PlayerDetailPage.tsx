import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import i18next from 'i18next';

import { BgtPoster } from '../Games/components/BgtPoster';
import { GetPercentage } from '../../utils/numberUtils';
import { BgtDeleteModal } from '../../components/Modals/BgtDeleteModal';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtHeading } from '../../components/BgtHeading/BgtHeading';
import { BgtMostWinnerCard } from '../../components/BgtCard/BgtMostWinnerCard';
import { BgtEditDeleteButtons } from '../../components/BgtButton/BgtEditDeleteButtons';
import BgtButton from '../../components/BgtButton/BgtButton';

import { EditPlayerModal } from './modals/EditPlayerModal';
import { usePlayers } from './hooks/usePlayers';
import { usePlayer } from './hooks/usePlayer';

import { useToasts } from '@/hooks/useToasts';

export const PlayerDetailpage = () => {
  const { id } = useParams<string>();
  const { t } = useTranslation();
  const { infoToast, errorToast } = useToasts();
  const [openUpdateModal, setOpenUpdateModal] = useState(false);

  if (!id) {
    throw Error('No player id found in path');
  }

  const onDeleteError = () => {
    errorToast('player.delete.failed');
  };

  const onDeleteSuccess = () => {
    infoToast('player.delete.successfull');
  };

  const { player, statistics } = usePlayer({ id });
  const { deletePlayer } = usePlayers({ onDeleteError, onDeleteSuccess });
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const navigate = useNavigate();

  const deletePlayerInternal = async () => {
    try {
      await deletePlayer(id as string);
      navigate('/players');
    } finally {
      setOpenDeleteModal(false);
    }
  };

  if (player.data === undefined || statistics.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent>
        <div className="grid grid-cols-12 gap-3">
          <div className="col-span-12 xl:col-span-8 2xl:col-span-9 flex flex-col justify-between gap-3">
            <div className="flex flex-col gap-6 w-full">
              <div className="grid grid-cols-12 gap-3">
                <BgtPoster
                  className="col-span-4 md:col-span-2 flex xl:hidden aspect-square"
                  title={player.data.name}
                  image={player.data.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-col gap-2">
                  <div className="flex flex-row justify-between">
                    <BgtHeading>{player.data.name}</BgtHeading>
                    <BgtEditDeleteButtons onDelete={() => alert('deleting')} onEdit={() => setOpenUpdateModal(true)} />
                  </div>
                  <BgtButton size="3" className="md:hidden">
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                </div>
              </div>
              {statistics.data.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton size="3" variant="outline" onClick={() => navigate(`/players/${id}/sessions`)}>
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
            <BgtMostWinnerCard
              name={statistics.data.mostWinsGame?.title}
              image={statistics.data.mostWinsGame?.image}
              value={statistics.data.mostWinsGame?.totalWins}
              nameHeader={t('statistics.best-game')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <BgtPoster
            className="hidden xl:flex xl:col-span-4 2xl:col-span-3"
            title={player.data.name}
            image={player.data.image}
          />
        </div>
        {statistics.data.playCount !== 0 && (
          <>
            <div className="grid grid-cols-3 lg:grid-cols-4 2xl:grid-cols-6 gap-1 md:gap-3">
              <BgtTextStatistic content={statistics.data.playCount} title={t('statistics.play-count')} />
              <BgtTextStatistic
                content={statistics.data.totalPlayedTime}
                title={t('statistics.total-play-time')}
                suffix={t('common.minutes-abbreviation')}
              />
              <BgtTextStatistic content={statistics.data.winCount} title={t('statistics.win-count')} />
              <BgtTextStatistic
                content={GetPercentage(statistics.data.winCount, statistics.data.playCount)}
                title={t('statistics.win-percentage')}
                suffix={'%'}
              />
              <BgtTextStatistic
                content={statistics.data.distinctGameCount}
                title={t('statistics.distinct-game-count')}
              />
            </div>
            <BgtHeading className="pt-8" size="7">
              {t('game.titles.analytics')}
            </BgtHeading>

            <BgtDeleteModal
              title={player.data.name}
              open={openDeleteModal}
              close={() => setOpenDeleteModal(false)}
              onDelete={deletePlayerInternal}
              description={t('common.delete.description', { title: player.data.name })}
            />
          </>
        )}
        {openUpdateModal && (
          <EditPlayerModal open={openUpdateModal} setOpen={setOpenUpdateModal} player={player.data} />
        )}
      </BgtPageContent>
    </BgtPage>
  );
};
