import { useTranslation } from 'react-i18next';
import { createFileRoute } from '@tanstack/react-router';

import { useShameData } from './-hooks/useShameData';
import { ShameGame } from './-components/ShameGame';
import { NoShames } from './-components/NoShames';

import { getShames, getShameStatistics } from '@/services/queries/games';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCardList } from '@/components/BgtLayout/BgtCardList';
import Game from '@/assets/icons/gamepad.svg?react';
import Coins from '@/assets/icons/coins.svg?react';

export const Route = createFileRoute('/shames/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getShames());
    queryClient.prefetchQuery(getShameStatistics());
  },
});

function RouteComponent() {
  const { t } = useTranslation();
  const { shames, statistics, settings, isLoading } = useShameData();

  return (
    <BgtPage>
      <BgtPageHeader header={t('common.shame')} icon={Game} />
      <BgtPageContent isLoading={isLoading} data={{ statistics, shames, settings }}>
        {({ statistics, shames, settings }) => (
          <>
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-3 xl:gap-6">
              <BgtTextStatistic content={statistics.count} title={t('shames.total-shames')} icon={<Game />} />
              <BgtTextStatistic
                prefix={settings.currency}
                content={statistics.totalValue}
                title={t('shames.total-value')}
                icon={<Coins />}
              />
              <BgtTextStatistic
                prefix={settings.currency}
                content={Math.round(statistics.averageValue ?? 0)}
                title={t('shames.average-value')}
                icon={<Coins />}
              />
            </div>
            {shames.length === 0 && <NoShames />}
            {shames.length !== 0 && (
              <BgtCardList>
                {shames.map((shame) => (
                  <ShameGame
                    key={shame.id}
                    shame={shame}
                    dateFormat={settings.dateFormat}
                    currency={settings.currency}
                  />
                ))}
              </BgtCardList>
            )}
          </>
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
