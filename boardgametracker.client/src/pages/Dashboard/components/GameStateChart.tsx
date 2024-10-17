import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';

import { getItemStateTranslationKey } from '../../../utils/ItemStateUtils';
import { BgtPieChart } from '../../../components/BgtCharts/BgtPieChart';
import { BgtChartCard } from '../../../components/BgtCard/BgtChartCard';

import { DashboardCharts, GameState } from '@/models';

interface Props {
  charts: DashboardCharts;
}

export const GameStateChart = (props: Props) => {
  const { charts } = props;
  const { t } = useTranslation();

  const pieData = useMemo(() => {
    if (charts?.gameState !== undefined) {
      return charts?.gameState
        .map((rank) => ({
          id: rank.type,
          label: rank.type,
          value: rank.gameCount,
        }))
        .reverse();
    }
    return [];
  }, [charts?.gameState]);

  if (charts === undefined) return null;

  return (
    <div className="col-span-1">
      <BgtChartCard title={t('dashboard.charts.games')} className="h-96">
        <BgtPieChart data={pieData} labelPrinter={(value) => t(getItemStateTranslationKey(value as GameState))} />
      </BgtChartCard>
    </div>
  );
};
