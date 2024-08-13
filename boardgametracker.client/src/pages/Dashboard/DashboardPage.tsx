import { t } from 'i18next';

import { useSettings } from '../../hooks/useSettings';
import { useDashboard } from '../../hooks/useDashboard';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtMostWinnerCard } from '../../components/BgtCard/BgtMostWinnerCard';

import { GameStateChart } from './components/GameStateChart';

export const DashboardPage = () => {
  const { statistics } = useDashboard();
  const { settings } = useSettings();

  if (statistics === undefined || settings === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.dashboard')} actions={[]} />
      <BgtPageContent>
        <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
          <BgtTextStatistic content={statistics.gameCount} title={t('statistics.game-count')} />
          <BgtTextStatistic content={statistics.playerCount} title={t('statistics.player-count')} />
          <BgtTextStatistic content={statistics.sessionCount} title={t('statistics.session-count')} />
          <BgtTextStatistic content={statistics.locationCount} title={t('statistics.location-count')} />
          <BgtTextStatistic
            content={statistics.totalCost}
            prefix={settings.currency}
            title={t('statistics.total-cost')}
          />
          <BgtTextStatistic
            content={statistics.meanPayed}
            prefix={settings.currency}
            title={t('statistics.mean-cost')}
          />

          <BgtTextStatistic
            content={statistics.totalPlayTime}
            title={t('statistics.total-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
          <BgtTextStatistic
            content={statistics.meanPlayTime}
            title={t('statistics.mean-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-1 md:gap-3">
          <div>
            <BgtMostWinnerCard
              image={statistics.mostWinningPlayer?.image}
              name={statistics.mostWinningPlayer?.name}
              value={statistics.mostWinningPlayer?.totalWins}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <div>
            <GameStateChart />
          </div>
        </div>
      </BgtPageContent>
    </BgtPage>
  );
};
