import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';

import { BgtPieChart } from '../../../components/BgtCharts/BgtPieChart';
import { BgtChartCard } from '../../../components/BgtCard/BgtChartCard';

import { getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { GameStateChart } from '@/models';
import GamePad from '@/assets/icons/gamepad.svg?react';

interface Props extends React.HTMLAttributes<HTMLDivElement> {
  data: GameStateChart[];
}

export const GameStateChartCard = (props: Props) => {
  const { data, className } = props;
  const { t } = useTranslation();

  const pieData = useMemo(() => {
    if (data !== undefined) {
      return data
        .map((rank) => ({
          id: rank.type,
          label: t(getItemStateTranslationKey(rank.type, false)),
          value: rank.gameCount,
        }))
        .reverse();
    }
    return [];
  }, [data, t]);

  if (data === undefined) return null;

  return (
    <BgtChartCard title={t('dashboard.charts.collection')} icon={GamePad} className={className}>
      <BgtPieChart data={pieData} showLegend tooltipPrefix="" />
    </BgtChartCard>
  );
};
