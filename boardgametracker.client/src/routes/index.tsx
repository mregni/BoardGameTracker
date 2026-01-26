import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { SessionCountChartCard } from './games/-components/SessionCountChartCard';
import { useDashboardData } from './-hooks/useDashboardData';
import { TopPlayersCard } from './-components/dashboard/TopPlayers';
import { RecentAddedGamesCard } from './-components/dashboard/RecentAddedGames';
import { RecentActivityCard } from './-components/dashboard/RecentActivity';
import { MostPlayedDashboardGamesCard } from './-components/dashboard/MostPlayedDashboardGames';
import { GameStateChartCard } from './-components/dashboard/GameStateChart';

import { formatMinutesToDuration } from '@/utils/dateUtils';
import { getDashboardStatistics } from '@/services/queries/dashboard';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtEmptyState } from '@/components/BgtLayout/BgtEmptyState';
import Players from '@/assets/icons/users.svg?react';
import Game from '@/assets/icons/gamepad.svg?react';
import Coins from '@/assets/icons/coins.svg?react';
import Calendar from '@/assets/icons/calendar.svg?react';

export const Route = createFileRoute('/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getDashboardStatistics());
  },
});

function RouteComponent() {
  const { statistics, settings } = useDashboardData();
  const navigate = useNavigate();
  const { t } = useTranslation();

  if (statistics === undefined || settings === undefined) return null;

  if (statistics.totalGames === 0) {
    return (
      <BgtPage>
        <BgtPageHeader header={t('common.dashboard')} />
        <BgtPageContent centered>
          <BgtEmptyState
            icon={Game}
            description={t('dashboard.empty.description')}
            title={t('dashboard.empty.title')}
            action={{
              label: t('dashboard.empty.button'),
              onClick: () => navigate({ to: '/games' }),
            }}
          />
        </BgtPageContent>
      </BgtPage>
    );
  }

  const totalPlayedTime = formatMinutesToDuration(
    statistics.totalPlayedTime,
    ['weeks', 'days', 'hours', 'minutes'],
    settings?.uiLanguage
  );

  const avgSessionTime = formatMinutesToDuration(statistics.avgSessionTime, ['hours', 'minutes'], settings?.uiLanguage);

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.dashboard')} />
      <BgtPageContent>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <BgtTextStatistic
            content={statistics.totalGames}
            title={t('statistics.game-count')}
            icon={<Game />}
            textSize="8"
            iconClassName="size-9"
          />
          <BgtTextStatistic
            content={statistics.activePlayers}
            title={t('statistics.player-count')}
            icon={<Players />}
            textSize="8"
            iconClassName="size-9"
          />
          <BgtTextStatistic
            content={statistics.sessionsPlayed}
            title={t('statistics.session-count')}
            icon={<Calendar />}
            textSize="8"
            iconClassName="size-9"
          />
          <BgtTextStatistic
            content={statistics.totalCollectionValue}
            title={t('statistics.collection-value')}
            prefix={settings.currency}
            icon={<Coins />}
            textSize="8"
            iconClassName="size-9"
          />
        </div>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <BgtTextStatistic content={totalPlayedTime} title={t('statistics.total-playtime')} />
          <BgtTextStatistic
            content={Math.round(statistics.avgGamePrice ?? 0)}
            title={t('statistics.average-cost')}
            prefix={settings.currency}
          />
          <BgtTextStatistic content={statistics.expansionsOwned} title={t('statistics.expansion-count')} />
          <BgtTextStatistic content={avgSessionTime} title={t('statistics.average-playtime')} />
        </div>
        <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-6 gap-4">
          <RecentActivityCard activities={statistics.recentActivities} className="col-span-1 2xl:col-span-2" />
          <MostPlayedDashboardGamesCard games={statistics.mostPlayedGames} className="col-span-1 2xl:col-span-2" />
          <GameStateChartCard data={statistics.collection} className="col-span-1 2xl:col-span-2" />
          <TopPlayersCard topPlayers={statistics.topPlayers} className="col-span-1 2xl:col-span-3" />
          <RecentAddedGamesCard games={statistics.recentAddedGames} className="col-span-1 2xl:col-span-3" />
          <SessionCountChartCard
            playByDayChart={statistics.sessionsByDayOfWeek}
            className="col-span-1 2xl:col-span-6"
          />
        </div>
        <div className="grid grid-cols-1 "></div>
      </BgtPageContent>
    </BgtPage>
  );
}
