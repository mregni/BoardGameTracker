import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';
import i18next from 'i18next';
import { formatDistanceToNowStrict } from 'date-fns';
import clsx from 'clsx';
import { Badge, Text, Heading, Button, DropdownMenu } from '@radix-ui/themes';
import { ArrowTrendingUpIcon, ArrowTrendingDownIcon } from '@heroicons/react/24/outline';

import { StringToHsl } from '../../utils/stringUtils';
import { RoundDecimal } from '../../utils/numberUtils';
import { Trend } from '../../models/Games/TopPlayer';
import { Player } from '../../models';
import { useSettings } from '../../hooks/useSettings';
import { usePlayers } from '../../hooks/usePlayers';
import { useGame } from '../../hooks/useGame';
import { BgtDeleteModal } from '../../components/Modals/BgtDeleteModal';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import { BgtStatistic } from '../../components/BgtStatistic/BgtStatistic';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtIcon } from '../../components/BgtIcon/BgtIcon';
import { BgtDetailHeader } from '../../components/BgtDetailHeader/BgtDetailHeader';
import { BgtHeaderCard } from '../../components/BgtCard/BgtHeaderCard';
import { BgtCard } from '../../components/BgtCard/BgtCard';
import { BgtPlayerAvatar } from '../../components/BgtAvatar/BgtPlayerAvatar';
import { BgtAvatar } from '../../components/BgtAvatar/BgtAvatar';

import { GameTopPlayers } from './components/GameTopPlayers';
import { GameStatistics } from './components/GameStatistics';
import { GamePlays } from './components/GamePlays';
import { GameDetailsPopup } from './components/GameDetailsPopup';
import { ScoringRankChart } from './components/charts/ScoringRankChart';
import { PlaysByWeekDayChart } from './components/charts/PlaysByWeekDayChart';
import { PlayerScoringChart } from './components/charts/PlayerScoringChart';
import { PlayerCountChart } from './components/charts/PlayerCountChart';

interface Props {
  player: Player | null;
}

const BgtMostWinsCard = (props: Props) => {
  const { player } = props;
  const { t } = useTranslation();

  if (player === undefined) return null;

  return (
    <div className="flex flex-row pt-3 gap-2">
      <BgtAvatar image={player?.image} title={player?.name} color={StringToHsl(player?.name)} size="large" />
      <div className="flex flex-col justify-center">
        <Text as="p" weight="medium" size="4">
          {player?.name}
        </Text>
        <Text as="p" size="2">
          {t('statistics.most-wint')}
        </Text>
      </div>
    </div>
  );
};

export const GameDetailPage = () => {
  const { id } = useParams();
  const { game, deleteGame, statistics, topPlayers } = useGame(id);
  const { t } = useTranslation();
  const { byId } = usePlayers();
  const { settings } = useSettings();

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
      });
  };

  const count = useMemo(() => {
    let count = 2;
    if (statistics == null) {
      return count;
    }

    if (statistics.averageScore != undefined) {
      count = count + 1;
    }
    if (statistics.averagePlayTime != undefined) {
      count = count + 1;
    }
    if (statistics.lastPlayed != undefined) {
      count = count + 1;
    }
    if (statistics.pricePerPlay != undefined) {
      count = count + 1;
    }

    return count;
  }, [statistics]);

  if (game === undefined || statistics === undefined || topPlayers === undefined || settings === undefined) return null;

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtHeaderCard image={game.backgroundImage}>
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
              <BgtMostWinsCard player={statistics.mostWinsPlayer} />
            </div>
            <div className="flex flex-row justify-end gap-2">
              <Button variant="solid" onClick={() => navigate(`/play/create/${game.id}`)} size="3">
                {i18next.format(t('game.add'), 'capitalize')}
              </Button>
              <DropdownMenu.Root>
                <DropdownMenu.Trigger>
                  <Button variant="solid" size="3">
                    Options
                    <DropdownMenu.TriggerIcon />
                  </Button>
                </DropdownMenu.Trigger>
                <DropdownMenu.Content size="2">
                  <DropdownMenu.Item onClick={() => setOpenDetailsModal(true)}>{i18next.format(t('common.details'), 'capitalize')}</DropdownMenu.Item>
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
                          {player.trend === Trend.Up && <BgtIcon icon={<ArrowTrendingUpIcon />} className="mt-1" size={17} />}
                          {player.trend === Trend.Down && <BgtIcon icon={<ArrowTrendingDownIcon />} className="mt-1" size={17} />}
                          {RoundDecimal(player.winPercentage * 100, 0.1)}%
                        </div>
                      </div>
                    </div>
                  </div>
                </BgtCard>
              ))}
            </div>
          </div>
        </BgtHeaderCard>
        <div className={clsx('grid gap-4', count <= 4 && 'grid-cols-4', count === 5 && 'grid-cols-5', count === 6 && 'grid-cols-6')}>
          <BgtTextStatistic content={RoundDecimal(statistics.averageScore)} title={t('statistics.average-score')} />
          <BgtTextStatistic
            content={RoundDecimal(statistics.averagePlayTime)}
            title={t('statistics.average-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
          <BgtTextStatistic content={statistics.playCount} title={t('statistics.play-count')} />
          <BgtTextStatistic
            content={statistics.lastPlayed != null ? formatDistanceToNowStrict(new Date(statistics.lastPlayed), { addSuffix: true }) : null}
            title={t('statistics.last-played')}
          />
          <BgtTextStatistic content={statistics.totalPlayedTime} title={t('statistics.total-play-time')} suffix={t('common.minutes_abbreviation')} />
          <BgtTextStatistic content={statistics.pricePerPlay} title={t('statistics.price-per-play')} suffix={settings.currency} />
        </div>
        <GameDetailsPopup open={openDetailsModal} setOpen={setOpenDetailsModal} id={id} />
        <BgtDeleteModal title={game.title} open={openDeleteModal} setOpen={setOpenDeleteModal} onDelete={deleteGameInternal} />
      </BgtPageContent>
    </BgtPage>
  );
};
