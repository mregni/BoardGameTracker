import { t } from 'i18next';

import { RoundDecimal } from '../../utils/numberUtils';
import { useSettings } from '../../hooks/useSettings';
import { BgtTextStatistic } from '../../components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '../../components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtMostWinnerCard } from '../../components/BgtCard/BgtMostWinnerCard';

import { useDashboardPage } from './hooks/useDashboardPage';
import { GameStateChart } from './components/GameStateChart';

export const DashboardPage = () => {
  const { statistics, charts } = useDashboardPage();
  const { settings } = useSettings();

  if (statistics.data === undefined || charts.data === undefined || settings === undefined) return null;

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.dashboard')} actions={[]} />
      <BgtPageContent>
        <div className="grid grid-cols-3 lg:grid-cols-4 gap-1 md:gap-3">
          <BgtTextStatistic content={statistics.data.gameCount} title={t('statistics.game-count')} />
          <BgtTextStatistic content={statistics.data.playerCount} title={t('statistics.player-count')} />
          <BgtTextStatistic content={statistics.data.sessionCount} title={t('statistics.session-count')} />
          <BgtTextStatistic content={statistics.data.locationCount} title={t('statistics.location-count')} />
          <BgtTextStatistic
            content={statistics.data.totalCost}
            prefix={settings.data?.currency}
            title={t('statistics.total-cost')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.data.meanPayed, 0.5)}
            prefix={settings.data?.currency}
            title={t('statistics.mean-cost')}
          />

          <BgtTextStatistic
            content={statistics.data.totalPlayTime}
            title={t('statistics.total-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
          <BgtTextStatistic
            content={RoundDecimal(statistics.data.meanPlayTime, 1)}
            title={t('statistics.mean-playtime')}
            suffix={t('common.minutes_abbreviation')}
          />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-1 md:gap-3">
          <div>
            <BgtMostWinnerCard
              image={statistics.data.mostWinningPlayer?.image}
              name={statistics.data.mostWinningPlayer?.name}
              value={statistics.data.mostWinningPlayer?.totalWins}
              nameHeader={t('statistics.most-wins')}
              valueHeader={t('statistics.win-count')}
            />
          </div>
          <div>
            <GameStateChart charts={charts.data} />
          </div>
        </div>
      </BgtPageContent>
    </BgtPage>
  );
};
