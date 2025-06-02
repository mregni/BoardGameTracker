import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef, useState } from 'react';
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
import { useToasts } from '@/hooks/useToasts';
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

interface Props extends ComponentPropsWithoutRef<'div'> {
  id: string;
  size?: '1' | '2' | '3' | undefined;
  showSessionListButton?: boolean;
}

const SessionButtons = ({ id, size, showSessionListButton = true, className }: Props) => {
  const navigate = useNavigate();
  const { t } = useTranslation();

  return (
    <div className={cx('flex-row justify-start gap-2 flex', className)}>
      <BgtButton size={size} onClick={() => navigate(`/sessions/create/${id}`)}>
        {i18next.format(t('game.add'))}
      </BgtButton>
      {showSessionListButton && (
        <BgtButton size={size} variant="outline" onClick={() => navigate(`/games/${id}/sessions`)}>
          {i18next.format(t('game.sessions'))}
        </BgtButton>
      )}
    </div>
  );
};

export const GameDetailPage = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const { settings } = useSettings();
  const navigate = useNavigate();
  const { errorToast, successToast } = useToasts();
  const [openDeleteModal, setOpenDeleteModal] = useState(false);

  const onDeleteError = () => {
    errorToast('game.delete.failed');
  };

  const onDeleteSuccess = () => {
    successToast('game.delete.successfull');
  };

  const { game, deleteGame, statistics, topPlayers } = useGame({ id, onDeleteError, onDeleteSuccess });

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
                    <BgtEditDeleteButtons
                      onDelete={() => setOpenDeleteModal(true)}
                      onEdit={() => navigate(`/games/${id}/update`)}
                    />
                  </div>
                  <div className="flex-row justify-start gap-2 hidden md:flex">
                    {game.data.categories.map((cat) => (
                      <BgtBadge key={cat.id} color="green" variant="soft">
                        {cat.name}
                      </BgtBadge>
                    ))}
                  </div>
                  <SessionButtons id={game.data.id} className="md:hidden" size="3" showSessionListButton={false} />
                  <SessionButtons id={game.data.id} className="hidden md:flex xl:hidden" size="1" />
                </div>
              </div>
              <div>
                <BgtText className={cx('xl:line-clamp-2 line-clamp-3')}>{game.data.description}</BgtText>
              </div>

              {statistics.data.playCount !== 0 && (
                <>
                  <BgtMostWinnerCard
                    image={statistics.data.mostWinsPlayer?.image}
                    name={statistics.data.mostWinsPlayer?.name}
                    value={statistics.data.mostWinsPlayer?.totalWins}
                    nameHeader={t('statistics.most-wins')}
                    valueHeader={t('statistics.win-count')}
                  />
                  <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
                    <BgtTextStatistic content={statistics.data.playCount} title={t('statistics.play-count')} />
                    <BgtTextStatistic
                      content={statistics.data.totalPlayedTime}
                      title={t('statistics.total-play-time')}
                      suffix={t('common.minutes-abbreviation')}
                    />
                    <BgtTextStatistic
                      content={statistics.data.pricePerPlay}
                      title={t('statistics.price-per-play')}
                      prefix={settings.data.currency}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.data.highScore)}
                      title={t('statistics.high-score')}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.data.averageScore)}
                      title={t('statistics.average-score')}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.data.averagePlayTime)}
                      title={t('statistics.average-playtime')}
                      suffix={t('common.minutes-abbreviation')}
                    />
                    <BgtTextStatistic
                      content={
                        statistics.data.lastPlayed != null
                          ? formatDistanceToNowStrict(new Date(statistics.data.lastPlayed), { unit: 'day' }).split(
                              ' '
                            )[0]
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
                </>
              )}
            </div>
            {statistics.data.playCount === 0 && <BgtNoSessions gameId={game.data.id} />}
            {statistics.data.playCount !== 0 && (
              <>
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
          </div>
          <div className="hidden xl:flex xl:col-span-4 2xl:col-span-3 xl:flex-col xl:gap-2">
            <BgtPoster title={game.data.title} image={game.data.image} />
            {statistics.data.playCount !== 0 && (
              <SessionButtons id={game.data.id} size="1" className="hidden xl:flex" />
            )}
          </div>
        </div>

        <BgtDeleteModal
          title={game.data.title}
          open={openDeleteModal}
          close={() => setOpenDeleteModal(false)}
          onDelete={deleteGameInternal}
          description={t('common.delete.description', { title: game.data.title })}
        />
      </BgtPageContent>
    </BgtPage>
  );
};
