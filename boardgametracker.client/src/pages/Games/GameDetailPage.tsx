import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import i18next from 'i18next';
import { formatDistanceToNowStrict } from 'date-fns';
import { cx } from 'class-variance-authority';

import { useGame } from './hooks/useGame';
import { ScoringRankChart } from './components/ScoringRankChart';
import { PlayerCountChart } from './components/PlayerCountChart';
import { TopPlayerCard } from './components/GameTopPlayers';
import { BgtPoster } from './components/BgtPoster';
import { BgtNoSessions } from './components/BgtNoSessions';

import { RoundDecimal } from '@/utils/numberUtils';
import { getColorFromGameState, getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { useToast } from '@/providers/BgtToastProvider';
import { useSettings } from '@/hooks/useSettings';
import { BgtDeleteModal } from '@/components/Modals/BgtDeleteModal';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtMostWinnerCard } from '@/components/BgtCard/BgtMostWinnerCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtBadge } from '@/components/BgtBadge/BgtBadge';

export const GameDetailPage = () => {
  const { id } = useParams();
  const { showInfoToast, showErrorToast } = useToast();

  const onDeleteError = () => {
    showErrorToast('game.delete.failed');
  };

  const onDeleteSuccess = () => {
    showInfoToast('game.delete.successfull');
  };

  const { game, deleteGame, statistics, topPlayers } = useGame({ id, onDeleteError, onDeleteSuccess });
  const { t } = useTranslation();
  const { settings } = useSettings();
  const [isExpanded, setIsExpanded] = useState(false);

  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const navigate = useNavigate();

  const deleteGameInternal = () => {
    void deleteGame()
      .then(() => {
        navigate('/games');
      })
      .finally(() => {
        setOpenDeleteModal(false);
      });
  };

  if (
    game.data === undefined ||
    statistics.data === undefined ||
    topPlayers.data === undefined ||
    settings.data === undefined
  )
    return null;

  return (
    <BgtPage>
      <BgtPageContent>
        <div className="grid grid-cols-12 gap-3">
          <div className="col-span-12 xl:col-span-8 2xl:col-span-9 flex flex-col justify-between gap-3">
            <div className="flex flex-col gap-6 w-full">
              <div className="grid grid-cols-12 gap-3">
                <BgtPoster
                  className="col-span-4 md:col-span-2 flex xl:hidden aspect-square"
                  title={game.data.title}
                  image={game.data.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-col gap-2">
                  <div className="flex flex-row justify-between">
                    <div className="flex flex-col gap-1">
                      <BgtText
                        size="2"
                        className="line-clamp-1 uppercase w-full"
                        weight="medium"
                        color={getColorFromGameState(game.data.state)}
                      >
                        {t(getItemStateTranslationKey(game.data.state))}
                      </BgtText>
                      <BgtHeading>{game.data.title}</BgtHeading>
                    </div>
                    <BgtEditDeleteButtons onDelete={() => setOpenDeleteModal(true)} onEdit={() => alert('editing')} />
                  </div>
                  <div className="flex-row justify-start gap-2 hidden md:flex">
                    {game.data.categories.map((cat) => (
                      <BgtBadge key={cat.id} color="green" variant="soft">
                        {cat.name}
                      </BgtBadge>
                    ))}
                  </div>
                  <BgtButton size="3" className="md:hidden" onClick={() => navigate(`/play/create/${game.data.id}`)}>
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                </div>
              </div>
              <div>
                <BgtText
                  className={cx(
                    'whitespace-pre-line transition-all duration-500 ease-in-out md:max-h-none overflow-hidden',
                    !isExpanded && 'xl:line-clamp-none transition-max-height max-h-11',
                    isExpanded && 'transition-max-height max-h-[1000px]'
                  )}
                >
                  {game.data.description}
                </BgtText>
                <div className="flex flex-row justify-end w-full md:hidden pt-2">
                  {!isExpanded && (
                    <BgtButton variant="inline" onClick={() => setIsExpanded((old) => !old)}>
                      {t('common.read-more')}
                    </BgtButton>
                  )}
                  {isExpanded && (
                    <BgtButton variant="inline" onClick={() => setIsExpanded((old) => !old)}>
                      {t('common.read-less')}
                    </BgtButton>
                  )}
                </div>
              </div>
              {statistics.data.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton size="3" onClick={() => navigate(`/play/create/${game.data.id}`)}>
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                  <BgtButton size="3" variant="outline" onClick={() => alert('Sessions not implemented')}>
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
            {statistics.data.playCount === 0 && <BgtNoSessions gameId={game.data.id} />}
            <BgtMostWinnerCard
              image={statistics.data.mostWinsPlayer?.image}
              name={statistics.data.mostWinsPlayer?.name}
              value={statistics.data.mostWinsPlayer?.totalWins}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <BgtPoster
            className="hidden xl:flex xl:col-span-4 2xl:col-span-3"
            title={game.data.title}
            image={game.data.image}
          />
        </div>
        {statistics.data.playCount !== 0 && (
          <>
            <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
              <BgtTextStatistic content={statistics.data.playCount} title={t('statistics.play-count')} />
              <BgtTextStatistic
                content={statistics.data.totalPlayedTime}
                title={t('statistics.total-play-time')}
                suffix={t('common.minutes_abbreviation')}
              />
              <BgtTextStatistic
                content={statistics.data.pricePerPlay}
                title={t('statistics.price-per-play')}
                prefix={settings.data.currency}
              />
              <BgtTextStatistic content={RoundDecimal(statistics.data.highScore)} title={t('statistics.high-score')} />
              <BgtTextStatistic
                content={RoundDecimal(statistics.data.averageScore)}
                title={t('statistics.average-score')}
              />
              <BgtTextStatistic
                content={RoundDecimal(statistics.data.averagePlayTime)}
                title={t('statistics.average-playtime')}
                suffix={t('common.minutes_abbreviation')}
              />
              <BgtTextStatistic
                content={
                  statistics.data.lastPlayed != null
                    ? formatDistanceToNowStrict(new Date(statistics.data.lastPlayed), { unit: 'day' }).split(' ')[0]
                    : null
                }
                suffix={t('common.days-ago')}
                title={t('statistics.last-played')}
              />
              <BgtTextStatistic
                content={game.data.buyingPrice}
                title={t('statistics.buy-price')}
                prefix={settings.data.currency}
              />
            </div>
            <BgtHeading className="pt-8" size="7">
              {t('game.titles.top-players')}
            </BgtHeading>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
              {topPlayers.data.map((player, i) => (
                <TopPlayerCard key={player.playerId} index={i} player={player} />
              ))}
            </div>
            <BgtHeading className="pt-8" size="7">
              {t('game.titles.analytics')}
            </BgtHeading>
            <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-3 gap-3">
              <ScoringRankChart />
              <PlayerCountChart />
            </div>
          </>
        )}
        <BgtDeleteModal
          title={game.data.title}
          open={openDeleteModal}
          setOpen={setOpenDeleteModal}
          onDelete={deleteGameInternal}
          description={t('common.delete.description', { title: game.data.title })}
        />
      </BgtPageContent>
    </BgtPage>
  );
};
