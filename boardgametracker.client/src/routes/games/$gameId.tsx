import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef, Dispatch, SetStateAction, useState } from 'react';
import i18next from 'i18next';
import { formatDistanceToNowStrict } from 'date-fns';
import { cx } from 'class-variance-authority';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';
import { BgtPoster } from '../-components/BgtPoster';

import { ExpansionSelectorModal } from './-modals/ExpansionSelectorModal';
import { useGameData } from './-hooks/useGameData';
import { ScoringRankChart } from './-components/ScoringRankChart';
import { PlayerCountChart } from './-components/PlayerCountChart';
import { TopPlayerCard } from './-components/GameTopPlayers';
import { BgtNoSessions } from './-components/BgtNoSessions';

import { RoundDecimal } from '@/utils/numberUtils';
import { getColorFromGameState, getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { getSettings } from '@/services/queries/settings';
import { getGame, getGameStatistics } from '@/services/queries/games';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtMostWinnerCard } from '@/components/BgtCard/BgtMostWinnerCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtBadge } from '@/components/BgtBadge/BgtBadge';

export const Route = createFileRoute('/games/$gameId')({
  component: RouteComponent,
  loader: async ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getGame(params.gameId));
    queryClient.prefetchQuery(getGameStatistics(params.gameId));
    queryClient.prefetchQuery(getSettings());
  },
});

interface Props extends ComponentPropsWithoutRef<'div'> {
  id: string;
  size?: '1' | '2' | '3' | undefined;
  showSessionListButton?: boolean;
  setOpenExpansionModal: Dispatch<SetStateAction<boolean>>;
}

const ExtraButtons = (props: Props) => {
  const { id, size, className, setOpenExpansionModal } = props;
  const navigate = useNavigate();
  const { t } = useTranslation();

  return (
    <div className={cx('flex flex-col gap-2', className)}>
      <div className={cx('flex-row justify-start gap-2 flex')}>
        <BgtButton size={size} onClick={() => navigate({ to: `/sessions/new/${id}` })} className="flex-1">
          {i18next.format(t('game.add'))}
        </BgtButton>
        <BgtButton
          size={size}
          variant="outline"
          onClick={() => navigate({ to: `/games/${id}/sessions` })}
          className="flex-1 hidden md:flex"
        >
          {i18next.format(t('game.sessions'))}
        </BgtButton>
      </div>
      <BgtButton onClick={() => setOpenExpansionModal(true)} size={size}>
        {t('game.expansions.update')}
      </BgtButton>
    </div>
  );
};

function RouteComponent() {
  const { gameId } = Route.useParams();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { errorToast, successToast } = useToasts();
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const [openExpansionModal, setOpenExpansionModal] = useState(false);

  const onDeleteError = () => {
    errorToast('game.delete.failed');
  };

  const onDeleteSuccess = () => {
    successToast('game.delete.successfull');
  };

  const { game, deleteGame, settings, statistics } = useGameData({ gameId, onDeleteError, onDeleteSuccess });

  const deleteGameInternal = () => {
    void deleteGame()
      .then(() => {
        navigate({ to: '/games' });
      })
      .finally(() => {
        setOpenDeleteModal(false);
      });
  };

  if (game === undefined || settings === undefined || statistics === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent>
        <div className="grid grid-cols-12 gap-3">
          <div className="col-span-12 xl:col-span-8 2xl:col-span-9 flex flex-col justify-between gap-3">
            <div className="flex flex-col gap-6 w-full">
              <div className="grid grid-cols-12 gap-3">
                <BgtPoster
                  className="col-span-4 md:col-span-2 flex xl:hidden aspect-square"
                  title={game.title}
                  image={game.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-col gap-2">
                  <div className="flex flex-row justify-between">
                    <div className="flex flex-col gap-1">
                      <BgtText
                        size="2"
                        className="line-clamp-1 uppercase w-full"
                        weight="medium"
                        color={getColorFromGameState(game.state)}
                      >
                        {t(getItemStateTranslationKey(game.state))}
                      </BgtText>
                      <BgtHeading>{game.title}</BgtHeading>
                    </div>
                    <BgtEditDeleteButtons
                      onDelete={() => setOpenDeleteModal(true)}
                      onEdit={() => navigate({ to: `/games/${gameId}/update` })}
                    />
                  </div>
                  <div className="flex-row justify-start gap-2 hidden md:flex">
                    {game.categories.map((cat) => (
                      <BgtBadge
                        key={cat.id}
                        color="green"
                        variant="soft"
                        onClick={() => navigate({ to: '/games', search: () => ({ category: cat.name }) })}
                      >
                        {cat.name}
                      </BgtBadge>
                    ))}
                  </div>
                  <ExtraButtons
                    id={game.id}
                    className="md:hidden"
                    size="1"
                    showSessionListButton={false}
                    setOpenExpansionModal={setOpenExpansionModal}
                  />
                  <ExtraButtons
                    id={game.id}
                    className="hidden md:flex xl:hidden"
                    size="1"
                    setOpenExpansionModal={setOpenExpansionModal}
                  />
                </div>
              </div>
              <div className="flex-row justify-start gap-2 flex md:hidden flex-wrap">
                {game.categories.map((cat) => (
                  <BgtBadge
                    key={cat.id}
                    color="green"
                    variant="soft"
                    onClick={() => navigate({ to: '/games', search: () => ({ category: cat.name }) })}
                  >
                    {cat.name}
                  </BgtBadge>
                ))}
              </div>
              <div>
                <BgtText className={cx('xl:line-clamp-2 line-clamp-3')}>{game.description}</BgtText>
              </div>
              {statistics.gameStats.playCount !== 0 && (
                <>
                  <BgtMostWinnerCard
                    image={statistics.gameStats.mostWinsPlayer?.image}
                    name={statistics.gameStats.mostWinsPlayer?.name}
                    value={statistics.gameStats.mostWinsPlayer?.totalWins}
                    onClick={() => navigate({ to: `/players/${statistics.gameStats.mostWinsPlayer?.id}` })}
                    nameHeader={t('statistics.most-wins')}
                    valueHeader={t('statistics.win-count')}
                  />
                  <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
                    <BgtTextStatistic content={statistics.gameStats.playCount} title={t('statistics.play-count')} />
                    <BgtTextStatistic
                      content={statistics.gameStats.totalPlayedTime}
                      title={t('statistics.total-play-time')}
                      suffix={t('common.minutes-abbreviation')}
                    />
                    <BgtTextStatistic
                      content={statistics.gameStats.pricePerPlay}
                      title={t('statistics.price-per-play')}
                      prefix={settings.currency}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.gameStats.highScore)}
                      title={t('statistics.high-score')}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.gameStats.averageScore)}
                      title={t('statistics.average-score')}
                    />
                    <BgtTextStatistic
                      content={RoundDecimal(statistics.gameStats.averagePlayTime)}
                      title={t('statistics.average-playtime')}
                      suffix={t('common.minutes-abbreviation')}
                    />
                    <BgtTextStatistic
                      content={
                        statistics.gameStats.lastPlayed != null
                          ? formatDistanceToNowStrict(new Date(statistics.gameStats.lastPlayed), { unit: 'day' }).split(
                              ' '
                            )[0]
                          : null
                      }
                      suffix={t('common.days-ago')}
                      title={t('statistics.last-played')}
                    />
                    <BgtTextStatistic
                      content={game.buyingPrice}
                      title={t('statistics.buy-price')}
                      prefix={settings.currency}
                    />
                    <BgtTextStatistic content={game.expansions.length} title={t('statistics.expansion-count')} />
                  </div>
                </>
              )}
            </div>
            {statistics.gameStats.playCount === 0 && <BgtNoSessions gameId={game.id} />}
            {statistics.gameStats.playCount !== 0 && (
              <>
                <BgtHeading className="pt-8" size="7">
                  {t('game.titles.top-players')}
                </BgtHeading>
                <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
                  {statistics.topPlayers.map((player, i) => (
                    <TopPlayerCard key={player.playerId} index={i} player={player} />
                  ))}
                </div>
              </>
            )}
          </div>
          <div className="hidden xl:flex xl:col-span-4 2xl:col-span-3 xl:flex-col xl:gap-2">
            <BgtPoster title={game.title} image={game.image} />
            <ExtraButtons
              id={game.id}
              size="1"
              className="hidden xl:flex"
              setOpenExpansionModal={setOpenExpansionModal}
            />
          </div>
        </div>
        {statistics.gameStats.playCount !== 0 && (
          <div>
            <BgtHeading className="pt-8" size="7">
              {t('game.titles.analytics')}
            </BgtHeading>
            <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-3 gap-3">
              <ScoringRankChart data={statistics.scoreRankChart} hasScoring={game.hasScoring} />
              <PlayerCountChart data={statistics.playerCountChart} />
            </div>
          </div>
        )}
        <BgtDeleteModal
          title={game.title}
          open={openDeleteModal}
          close={() => setOpenDeleteModal(false)}
          onDelete={deleteGameInternal}
          description={t('common.delete.description', { title: game.title })}
        />
        {openExpansionModal && (
          <ExpansionSelectorModal
            open={openExpansionModal}
            setOpen={setOpenExpansionModal}
            gameId={gameId}
            selectedExpansions={game.expansions.map((x) => x.bggId)}
          />
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
