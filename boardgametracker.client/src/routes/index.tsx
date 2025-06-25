import { t } from 'i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useDashboardData } from './-hooks/useDashboardData';
import { GameStateChart } from './-components/GameStateChart';

import { RoundDecimal } from '@/utils/numberUtils';
import { getDashboardCharts, getDashboardStatistics } from '@/services/queries/dashboard';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtMostWinnerCard } from '@/components/BgtCard/BgtMostWinnerCard';

export const Route = createFileRoute('/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getDashboardStatistics());
    queryClient.prefetchQuery(getDashboardCharts());
  },
});

function RouteComponent() {
  const { statistics, charts, settings } = useDashboardData();
  const navigate = useNavigate();

  if (statistics === undefined || charts === undefined || settings === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.dashboard')} actions={[]} />
      <BgtPageContent>
        <h1>TODO</h1>
        <ul>
          <li>✅ Filter games on name (input field)</li>
          <li>✅ Make tags clickable in game detail and navigate to game overview with filter</li>
          <li>✅ Support for Expansions</li>
          <li>Move badges for achievemnts out of $playerId.tsx</li>
          <li>
            User achievements:
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Played 5, 10, 50, 100 sessions
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- DONE - Played 3, 10, 20, 50 different games
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Won 3, 10, 25, 50 sessions
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Played for 5, 10, 50, 100 hours
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Have a win percentage of 30%, 50%, 70%, 90% (with minimum of 5 sessions
            played)
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Solo Specialist: Win 5, 10, 25, 50 solo games
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Win 5, 10, 15, 25 games in a row
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Social Player: Play with 5, 10, 25, 50 different people
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Won with 2 or less points difference from second place
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Almost there!: Lost with 2 or less points difference from first place
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Marathon Runner: Play a single game session lasting 4+ hours
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- First Try: Win a game on your first play
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Learning Curve: Beat your personal best score 3 times in the same game
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Monthly Goal: Play 20+ games in a single month
            <br /> &nbsp;&nbsp;&nbsp;&nbsp;- Consistent Schedule: Play every Saturday for 10 weeks straight
          </li>
          <li>REMOVE ME WHEN ALL IS DONE</li>
        </ul>
        <h1>Bugs</h1>
        <ul></ul>
        <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
          <BgtTextStatistic content={statistics.gameCount} title={t('statistics.game-count')} />
          <BgtTextStatistic content={statistics.expansionCount} title={t('statistics.expansion-count')} />
          <BgtTextStatistic content={statistics.playerCount} title={t('statistics.player-count')} />
          <BgtTextStatistic content={statistics.sessionCount} title={t('statistics.session-count')} />
          <BgtTextStatistic content={statistics.locationCount} title={t('statistics.location-count')} />
          <BgtTextStatistic
            content={statistics.totalCost}
            prefix={settings.currency}
            title={t('statistics.total-cost')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.meanPayed, 0.5)}
            prefix={settings.currency}
            title={t('statistics.mean-cost')}
          />

          <BgtTextStatistic
            content={statistics.totalPlayTime}
            title={t('statistics.total-playtime')}
            suffix={t('common.minutes-abbreviation')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.meanPlayTime, 1)}
            title={t('statistics.mean-playtime')}
            suffix={t('common.minutes-abbreviation')}
          />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-1 md:gap-3">
          <div>
            <BgtMostWinnerCard
              image={statistics.mostWinningPlayer?.image}
              name={statistics.mostWinningPlayer?.name}
              value={statistics.mostWinningPlayer?.totalWins}
              onClick={() => navigate({ to: `/players/${statistics.mostWinningPlayer?.id}` })}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <div>
            <GameStateChart charts={charts} />
          </div>
        </div>
      </BgtPageContent>
    </BgtPage>
  );
}
