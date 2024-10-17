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

import { usePlayerDetailPage } from './hooks/usePlayerDetailPage';

export const PlayerDetailpage = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { player, statistics, deletePlayer } = usePlayerDetailPage(id);
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

  if (player.data === undefined || statistics.data === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent className="flex flex-col gap-3">
        <div className="grid grid-cols-12 gap-3">
          <div className="col-span-12 xl:col-span-8 2xl:col-span-9 flex flex-col justify-between gap-3">
            <div className="flex flex-col gap-6 w-full">
              <div className="grid grid-cols-12 gap-3">
                <BgtPoster
                  className="col-span-4 md:col-span-2 flex xl:hidden aspect-square"
                  title={player.data.model.name}
                  image={player.data.model.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-col gap-2">
                  <div className="flex flex-row justify-between">
                    <BgtHeading className="uppercase">{player.data.model.name}</BgtHeading>
                    <BgtEditDeleteButtons onDelete={() => alert('deleting')} onEdit={() => alert('editing')} />
                  </div>
                  <BgtButton size="3" className="md:hidden">
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                </div>
              </div>
              {statistics.data.model.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton size="3" onClick={() => navigate(`/play/create`)}>
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                  <BgtButton size="3" variant="outline" onClick={() => alert('Sessions not implemented')}>
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
            <BgtMostWinnerCard
              name={statistics.data.model.mostWinsGame?.title}
              image={statistics.data.model.mostWinsGame?.image}
              value={statistics.data.model.mostWinsGame?.totalWins}
              nameHeader={t('statistics.best-game')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <BgtPoster
            className="hidden xl:flex xl:col-span-4 2xl:col-span-3"
            title={player.data.model.name}
            image={player.data.model.image}
          />
        </div>
        {statistics.data.model.playCount !== 0 && (
          <>
            <div className="grid grid-cols-3 lg:grid-cols-4 2xl:grid-cols-6 gap-1 md:gap-3">
              <BgtTextStatistic content={statistics.data.model.playCount} title={t('statistics.play-count')} />
              <BgtTextStatistic
                content={statistics.data.model.totalPlayedTime}
                title={t('statistics.total-play-time')}
                suffix={t('common.minutes_abbreviation')}
              />
              <BgtTextStatistic content={statistics.data.model.winCount} title={t('statistics.win-count')} />
              <BgtTextStatistic
                content={GetPercentage(statistics.data.model.winCount, statistics.data.model.playCount)}
                title={t('statistics.win-percentage')}
                suffix={'%'}
              />
              <BgtTextStatistic
                content={statistics.data.model.distinctGameCount}
                title={t('statistics.distinct-game-count')}
              />
            </div>
            <BgtHeading className="pt-8 uppercase" size="7">
              {t('game.titles.analytics')}
            </BgtHeading>

            <BgtDeleteModal
              title={player.data.model.name}
              open={openDeleteModal}
              setOpen={setOpenDeleteModal}
              onDelete={deletePlayerInternal}
            />
          </>
        )}

        {/* <PlayerPlays /> */}
      </BgtPageContent>
    </BgtPage>
  );
};
