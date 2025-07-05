import { useTranslation } from 'react-i18next';
import { useMemo, useState, useRef, LegacyRef } from 'react';
import i18next from 'i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';
import { BgtPoster } from '../-components/BgtPoster';

import { usePlayerData } from './-hooks/usePlayerData';

import { GetPercentage } from '@/utils/numberUtils';
import { getPlayer, getPlayerStatistics } from '@/services/queries/players';
import { EditPlayerModal } from '@/routes/players/-modals/EditPlayerModal';
import { Badge, BadgeType } from '@/models';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtMostWinnerCard } from '@/components/BgtCard/BgtMostWinnerCard';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtAchievement, BgtAchievementIcon } from '@/components/BgtAchievement/BgtAchievement';

export const Route = createFileRoute('/players/$playerId')({
  component: RouteComponent,
  loader: ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getPlayer(params.playerId));
    queryClient.prefetchQuery(getPlayerStatistics(params.playerId));
  },
});

interface BadgeContainerProps {
  badges: Badge[];
  achievementsRef: LegacyRef<HTMLDivElement> | undefined;
}

export const BgtBadgeContainer = (props: BadgeContainerProps) => {
  const { badges, achievementsRef } = props;

  const processedBadges = useMemo(() => {
    if (!badges?.length) {
      return { displayBadges: [], hiddenCount: 0 };
    }

    const badgesByType = badges.reduce(
      (acc, badge) => {
        if (!acc[badge.type]) {
          acc[badge.type] = [];
        }
        acc[badge.type].push(badge);
        return acc;
      },
      {} as Record<BadgeType, Badge[]>
    );

    const highestLevelBadges: Badge[] = [];
    let totalHiddenCount = 0;

    Object.values(badgesByType).forEach((typeBadges) => {
      const sortedBadges = typeBadges.sort((a, b) => {
        if (a.level === null) return 1;
        if (b.level === null) return -1;
        return b.level - a.level;
      });

      highestLevelBadges.push(sortedBadges[0]);
      totalHiddenCount += sortedBadges.length - 1;
    });

    return {
      displayBadges: highestLevelBadges,
      hiddenCount: totalHiddenCount,
    };
  }, [badges]);

  const handleClick = () => {
    achievementsRef?.current?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <div className="flex flex-row flex-wrap gap-2">
      {processedBadges.displayBadges.map((badge) => (
        <BgtAchievementIcon key={badge.titleKey} badge={badge} />
      ))}
      {processedBadges.hiddenCount > 0 && (
        <div
          onClick={() => handleClick()}
          className="text-gray-500 text-xl flex items-center hover:cursor-pointer hover:underline"
        >
          +{processedBadges.hiddenCount}
        </div>
      )}
    </div>
  );
};

function RouteComponent() {
  const { playerId } = Route.useParams();
  const { t } = useTranslation();
  const { infoToast, errorToast } = useToasts();
  const [openUpdateModal, setOpenUpdateModal] = useState(false);
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const ref = useRef(null);
  const navigate = useNavigate();

  const onDeleteError = () => {
    errorToast('player.delete.failed');
  };

  const onDeleteSuccess = () => {
    infoToast('player.delete.successfull');
  };

  const { player, statistics, deletePlayer } = usePlayerData({ playerId, onDeleteError, onDeleteSuccess });

  const deletePlayerInternal = async () => {
    try {
      await deletePlayer(playerId);
      navigate({ to: '/players' });
    } finally {
      setOpenDeleteModal(false);
    }
  };

  if (player === undefined || statistics === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent>
        <div className="grid grid-cols-12 gap-3">
          <div className="col-span-12 xl:col-span-8 2xl:col-span-9 flex flex-col justify-between gap-3">
            <div className="flex flex-col gap-6 w-full">
              <div className="grid grid-cols-12 gap-3">
                <BgtPoster
                  className="col-span-4 md:col-span-2 flex xl:hidden aspect-square"
                  title={player.name}
                  image={player.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-row justify-between">
                  <BgtHeading>{player.name}</BgtHeading>
                  <BgtEditDeleteButtons
                    onDelete={() => setOpenDeleteModal(true)}
                    onEdit={() => setOpenUpdateModal(true)}
                  />
                </div>
              </div>

              <BgtBadgeContainer badges={player.badges} achievementsRef={ref} />

              <BgtMostWinnerCard
                name={statistics.mostWinsGame?.title}
                image={statistics.mostWinsGame?.image}
                value={statistics.mostWinsGame?.totalWins}
                onClick={() => navigate({ to: `/games/${statistics.mostWinsGame?.id}` })}
                nameHeader={t('statistics.best-game')}
                valueHeader={t('statistics.win-count')}
              />

              {statistics.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton
                    size="3"
                    variant="outline"
                    onClick={() => navigate({ to: `/players/${playerId}/sessions` })}
                  >
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
          </div>
          <BgtPoster className="hidden xl:flex xl:col-span-4 2xl:col-span-3" title={player.name} image={player.image} />
        </div>
        {statistics.playCount !== 0 && (
          <>
            <div className="grid grid-cols-3 lg:grid-cols-4 2xl:grid-cols-6 gap-1 md:gap-3">
              <BgtTextStatistic content={statistics.playCount} title={t('statistics.play-count')} />
              <BgtTextStatistic
                content={statistics.totalPlayedTime}
                title={t('statistics.total-play-time')}
                suffix={t('common.minutes-abbreviation')}
              />
              <BgtTextStatistic content={statistics.winCount} title={t('statistics.win-count')} />
              <BgtTextStatistic
                content={GetPercentage(statistics.winCount, statistics.playCount)}
                title={t('statistics.win-percentage')}
                suffix={'%'}
              />
              <BgtTextStatistic content={statistics.distinctGameCount} title={t('statistics.distinct-game-count')} />
            </div>
            <div ref={ref}>
              <BgtHeading className="pt-8" size="7">
                {t('player.titles.achievements')}
              </BgtHeading>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1 md:gap-3">
              {player.badges && player.badges.map((badge) => <BgtAchievement key={badge.titleKey} badge={badge} />)}
            </div>
          </>
        )}
        {openDeleteModal && (
          <BgtDeleteModal
            title={player.name}
            open={openDeleteModal}
            close={() => setOpenDeleteModal(false)}
            onDelete={deletePlayerInternal}
            description={t('common.delete.description', { title: player.name })}
          />
        )}
        {openUpdateModal && <EditPlayerModal open={openUpdateModal} setOpen={setOpenUpdateModal} player={player} />}
      </BgtPageContent>
    </BgtPage>
  );
}
