import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LegacyRef, useMemo, useRef, useState } from 'react';
import i18next from 'i18next';
import * as Tooltip from '@radix-ui/react-tooltip';

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

import { Badge, BadgeType } from '@/models';
import { useToasts } from '@/hooks/useToasts';
import { BgtCard } from '@/components/BgtCard/BgtCard';

interface Props {
  badge: Badge;
}

const BgtBadge = (props: Props) => {
  const { badge } = props;
  const { t } = useTranslation();

  return (
    <BgtCard className="col-span-1 p-3">
      <div className="flex flex-row gap-3">
        <img src={`/images/badges/${badge.image}`} alt="Badge image" className="h-10 aspect-square" />
        <div className="flex flex-col">
          <div className="font-bold">{t(`badges.${badge.titleKey}`)}</div>
          <div className="text-xs line-clamp-1">{t(`badges.${badge.descriptionKey}`)}</div>
        </div>
      </div>
    </BgtCard>
  );
};

const BgtBadgeIcon = (props: Props) => {
  const { badge } = props;
  const { t } = useTranslation();

  return (
    <Tooltip.Provider>
      <Tooltip.Root>
        <Tooltip.Trigger asChild>
          <img src={`/images/badges/${badge.image}`} alt="Badge image" className="h-10 aspect-square" />
        </Tooltip.Trigger>
        <Tooltip.Portal>
          <Tooltip.Content
            className="select-none rounded bg-card-black border-card-border border-2 border-solid px-[15px] py-2.5 text-[15px] leading-none will-change-[transform,opacity] data-[state=delayed-open]:data-[side=bottom]:animate-slideUpAndFade data-[state=delayed-open]:data-[side=left]:animate-slideRightAndFade data-[state=delayed-open]:data-[side=right]:animate-slideLeftAndFade data-[state=delayed-open]:data-[side=top]:animate-slideDownAndFade"
            sideOffset={5}
          >
            <div className="flex flex-col justify-center">
              <div className="font-bold">{t(`badges.${badge.titleKey}`)}</div>
              <div className="text-xs">{t(`badges.${badge.descriptionKey}`)}</div>
            </div>
          </Tooltip.Content>
        </Tooltip.Portal>
      </Tooltip.Root>
    </Tooltip.Provider>
  );
};

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
        <BgtBadgeIcon key={badge.titleKey} badge={badge} />
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

export const PlayerDetailpage = () => {
  const { id } = useParams<string>();
  const { t } = useTranslation();
  const { infoToast, errorToast } = useToasts();
  const [openUpdateModal, setOpenUpdateModal] = useState(false);
  const ref = useRef(null);

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

              <BgtBadgeContainer badges={player.data.badges} achievementsRef={ref} />

              <BgtMostWinnerCard
                name={statistics.data.mostWinsGame?.title}
                image={statistics.data.mostWinsGame?.image}
                value={statistics.data.mostWinsGame?.totalWins}
                onClick={() => navigate(`/games/${statistics.data.mostWinsGame?.id}`)}
                nameHeader={t('statistics.best-game')}
                valueHeader={t('statistics.win-count')}
              />

              {statistics.data.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton size="3" variant="outline" onClick={() => navigate(`/players/${id}/sessions`)}>
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
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
            <div ref={ref}>
              <BgtHeading className="pt-8" size="7">
                {t('player.titles.achievements')}
              </BgtHeading>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1 md:gap-3">
              {player.data.badges && player.data.badges.map((badge) => <BgtBadge key={badge.titleKey} badge={badge} />)}
            </div>

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
