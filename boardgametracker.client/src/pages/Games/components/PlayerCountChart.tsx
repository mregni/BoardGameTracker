import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';

import { useGame } from '../hooks/useGame';
import { BgtPieChart } from '../../../components/BgtCharts/BgtPieChart';
import { BgtChartCard } from '../../../components/BgtCard/BgtChartCard';

export const PlayerCountChart = () => {
  const { id } = useParams();
  const { chartPlayerCount } = useGame({ id });
  const { t } = useTranslation();

  const pieData = useMemo(() => {
    return chartPlayerCount
      .map((rank) => ({
        id: rank.players,
        label: rank.players,
        value: rank.playCount,
      }))
      .reverse();
  }, [chartPlayerCount]);

  return (
    <div className="col-span-1">
      <BgtChartCard title={t('game.charts.player-count')} className="h-96">
        <BgtPieChart data={pieData} labelPrinter={(value) => t('common.player', { count: +(value as number) })} />
      </BgtChartCard>
    </div>
  );
};
