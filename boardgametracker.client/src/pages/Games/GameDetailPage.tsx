import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import i18next from 'i18next';
import { formatDistanceToNowStrict } from 'date-fns';
import clsx from 'clsx';

import { RoundDecimal } from '../../utils/numberUtils';
import { getColorFromGameState, getItemStateTranslationKey } from '../../utils/ItemStateUtils';
import { useSettings } from '../../hooks/useSettings';
import { BgtDeleteModal } from '../../components/Modals/BgtDeleteModal';
import { BgtText } from '../../components/BgtText/BgtText';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtHeading } from '../../components/BgtHeading/BgtHeading';
import { BgtMostWinnerCard } from '../../components/BgtCard/BgtMostWinnerCard';
import { BgtEditDeleteButtons } from '../../components/BgtButton/BgtEditDeleteButtons';
import BgtButton from '../../components/BgtButton/BgtButton';
import { BgtBadge } from '../../components/BgtBadge/BgtBadge';

import { useGameDetailPage } from './hooks/useGameDetailPage';
import { TopPlayerCard } from './components/GameTopPlayers';
import { GameCharts } from './components/GameCharts';
import { BgtPoster } from './components/BgtPoster';
import { BgtNoSessions } from './components/BgtNoSessions';

export const GameDetailPage = () => {
  const { id } = useParams();
  const { game, deleteGame, statistics, topPlayers } = useGameDetailPage(id);
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
    settings === undefined
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
                  title={game.data.model.title}
                  image={game.data.model.image}
                />
                <div className="col-span-8 md:col-span-10 xl:col-span-12 flex flex-col gap-2">
                  <div className="flex flex-row justify-between">
                    <div className="flex flex-col gap-1">
                      <BgtText
                        size="2"
                        className="line-clamp-1 uppercase w-full"
                        weight="medium"
                        color={getColorFromGameState(game.data.model.state)}
                      >
                        {t(getItemStateTranslationKey(game.data.model.state))}
                      </BgtText>
                      <BgtHeading className="uppercase">{game.data.model.title}</BgtHeading>
                    </div>
                    <BgtEditDeleteButtons onDelete={() => alert('deleting')} onEdit={() => alert('editing')} />
                  </div>
                  <div className="flex-row justify-start gap-2 hidden md:flex">
                    {game.data.model.categories.map((cat) => (
                      <BgtBadge key={cat.id} color="green" variant="soft">
                        {cat.name}
                      </BgtBadge>
                    ))}
                  </div>
                  <BgtButton
                    size="3"
                    className="md:hidden"
                    onClick={() => navigate(`/play/create/${game.data.model.id}`)}
                  >
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                </div>
              </div>
              <div>
                <BgtText
                  className={clsx(
                    'whitespace-pre-line transition-all duration-500 ease-in-out md:max-h-none overflow-hidden',
                    !isExpanded && 'xl:line-clamp-none transition-max-height max-h-11',
                    isExpanded && 'transition-max-height max-h-[1000px]'
                  )}
                >
                  {game.data.model.description}
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
              {statistics.data.model.playCount !== 0 && (
                <div className="flex-row justify-start gap-2 hidden md:flex">
                  <BgtButton size="3" onClick={() => navigate(`/play/create/${game.data.model.id}`)}>
                    {i18next.format(t('game.add'))}
                  </BgtButton>
                  <BgtButton size="3" variant="outline" onClick={() => alert('Sessions not implemented')}>
                    {i18next.format(t('game.sessions'))}
                  </BgtButton>
                </div>
              )}
            </div>
            {statistics.data.model.playCount === 0 && <BgtNoSessions gameId={game.data.model.id} />}
            <BgtMostWinnerCard
              image={statistics.data.model.mostWinsPlayer?.image}
              name={statistics.data.model.mostWinsPlayer?.name}
              value={statistics.data.model.mostWinsPlayer?.totalWins}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <BgtPoster
            className="hidden xl:flex xl:col-span-4 2xl:col-span-3"
            title={game.data.model.title}
            image={game.data.model.image}
          />
        </div>
        {statistics.data.model.playCount !== 0 && (
          <>
            <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
              <BgtTextStatistic content={statistics.data.model.playCount} title={t('statistics.play-count')} />
              <BgtTextStatistic
                content={statistics.data.model.totalPlayedTime}
                title={t('statistics.total-play-time')}
                suffix={t('common.minutes_abbreviation')}
              />
              <BgtTextStatistic
                content={statistics.data.model.pricePerPlay}
                title={t('statistics.price-per-play')}
                prefix={settings.currency}
              />
              <BgtTextStatistic
                content={RoundDecimal(statistics.data.model.highScore)}
                title={t('statistics.high-score')}
              />
              <BgtTextStatistic
                content={RoundDecimal(statistics.data.model.averageScore)}
                title={t('statistics.average-score')}
              />
              <BgtTextStatistic
                content={RoundDecimal(statistics.data.model.averagePlayTime)}
                title={t('statistics.average-playtime')}
                suffix={t('common.minutes_abbreviation')}
              />
              <BgtTextStatistic
                content={
                  statistics.data.model.lastPlayed != null
                    ? formatDistanceToNowStrict(new Date(statistics.data.model.lastPlayed), { unit: 'day' }).split(
                        ' '
                      )[0]
                    : null
                }
                suffix={t('common.days-ago')}
                title={t('statistics.last-played')}
              />
              <BgtTextStatistic
                content={game.data.model.buyingPrice}
                title={t('statistics.buy-price')}
                prefix={settings.currency}
              />
            </div>
            <BgtHeading className="pt-8 uppercase" size="7">
              {t('game.titles.top-players')}
            </BgtHeading>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
              {topPlayers.data.model.map((player, i) => (
                <TopPlayerCard key={player.playerId} index={i} player={player} />
              ))}
            </div>
            <BgtHeading className="pt-8 uppercase" size="7">
              {t('game.titles.analytics')}
            </BgtHeading>
            <GameCharts />
            <BgtDeleteModal
              title={game.data.model.title}
              open={openDeleteModal}
              setOpen={setOpenDeleteModal}
              onDelete={deleteGameInternal}
            />
          </>
        )}

        {/* <BgtCard transparant noPadding>
          <div className="flex flex-row justify-between">
            <div className="flex flex-col gap-3">
              <Heading as="h4" weight="medium">
                {game.title}
              </Heading>
              <div className="flex flex-row gap-2">
                {game.categories.map((cat) => (
                  <Badge color="green" key={cat.id} variant="solid">
                    {cat.name}
                  </Badge>
                ))}
              </div>
            </div>
            <div className="flex flex-row justify-end gap-2">
              <Button variant="solid" onClick={() => navigate(`/play/create/${game.id}`)} size="3"></Button>
              <DropdownMenu.Root>
                <DropdownMenu.Trigger>
                  <Button variant="solid" size="3">
                    Options
                    <DropdownMenu.TriggerIcon />
                  </Button>
                </DropdownMenu.Trigger>
                <DropdownMenu.Content size="2">
                  <DropdownMenu.Item onClick={() => setOpenDetailsModal(true)}>
                    {i18next.format(t('common.details'), 'capitalize')}
                  </DropdownMenu.Item>
                  <DropdownMenu.Item disabled>{i18next.format(t('common.edit'), 'capitalize')}</DropdownMenu.Item>
                  <DropdownMenu.Separator />
                  <DropdownMenu.Item color="red" onClick={() => setOpenDeleteModal(true)}>
                    {i18next.format(t('common.delete.button'), 'capitalize')}
                  </DropdownMenu.Item>
                </DropdownMenu.Content>
              </DropdownMenu.Root>
            </div>
          </div>
          <div className="flex flex-col gap-3">
            <Heading as="h6" weight="medium" size="5">
              {t('games.cards.top-players')}
            </Heading>
            <div className="grid grid-cols-5 gap-3">
              {topPlayers.map((player, i) => (
                <BgtCard key={i}>
                  <div className="flex flex-col gap-5">
                    <div className="flex flex-col gap-2">
                      <BgtAvatar
                        image={byId(player.playerId)?.image}
                        title={byId(player.playerId)?.name}
                        color={StringToHsl(byId(player.playerId)?.name)}
                        size="large"
                      />
                      <Heading as="h4" weight="medium">
                        {byId(player.playerId)?.name}
                      </Heading>
                    </div>
                    <div className="flex flex-row justify-between">
                      <div className="flex-col">
                        <Text as="div">Ranking</Text>
                        <Text as="div" weight="bold">
                          {player.wins}/{player.playCount}
                        </Text>
                      </div>
                      <div className="flex-col">
                        <Text as="div" align="right">
                          Trend
                        </Text>
                        <div
                          className={clsx(
                            'flex flex-row gap-1',
                            player.trend === Trend.Up && 'text-green-400',
                            player.trend === Trend.Down && 'text-red-500',
                            player.trend === Trend.Equal && 'text-orange-400'
                          )}
                        >
                          {player.trend === Trend.Up && (
                            <BgtIcon icon={<ArrowTrendingUpIcon />} className="mt-1" size={17} />
                          )}
                          {player.trend === Trend.Down && (
                            <BgtIcon icon={<ArrowTrendingDownIcon />} className="mt-1" size={17} />
                          )}
                          {RoundDecimal(player.winPercentage * 100, 0.1)}%
                        </div>
                      </div>
                    </div>
                  </div>
                </BgtCard>
              ))}
            </div>
          </div>
        </BgtCard>
        <div
          className={clsx(
            'grid gap-4',
            count <= 4 && 'grid-cols-4',
            count === 5 && 'grid-cols-5',
            count === 6 && 'grid-cols-6'
          )}
        >
          <BgtTextStatistic content={RoundDecimal(statistics.averageScore)} title={t('statistics.average-score')} />
          <BgtTextStatistic
            content={RoundDecimal(statistics.averagePlayTime)}
            title={t('statistics.average-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
          <BgtTextStatistic content={statistics.playCount} title={t('statistics.play-count')} />
          <BgtTextStatistic
            content={
              statistics.lastPlayed != null
                ? formatDistanceToNowStrict(new Date(statistics.lastPlayed), { addSuffix: true })
                : null
            }
            title={t('statistics.last-played')}
          />
          <BgtTextStatistic
            content={statistics.totalPlayedTime}
            title={t('statistics.total-play-time')}
            suffix={t('common.minutes_abbreviation')}
          />
          <BgtTextStatistic
            content={statistics.pricePerPlay}
            title={t('statistics.price-per-play')}
            suffix={settings.currency}
          />
        </div>
        <GameDetailsPopup open={openDetailsModal} setOpen={setOpenDetailsModal} id={id} />
        <BgtDeleteModal
          title={game.title}
          open={openDeleteModal}
          setOpen={setOpenDeleteModal}
          onDelete={deleteGameInternal}
        /> */}
      </BgtPageContent>
    </BgtPage>
  );
};
