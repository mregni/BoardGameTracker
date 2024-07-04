/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/dot-notation */
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AgChartsReact } from 'ag-charts-react';

import { useGame } from '../../../../hooks/useGame';
import { BgtChartCard } from '../../../../components/BgtCard/BgtChartCard';

export const PlayerCountChart = () => {
  const { id } = useParams();
  const { playerCountChart } = useGame(id);
  const { t } = useTranslation();

  return (
    <BgtChartCard title={t('game.charts.player-count')}>
      <AgChartsReact
        options={{
          data: playerCountChart,
          series: [
            {
              type: 'pie',
              angleKey: 'playCount',
              legendItemKey: 'players',
              showInLegend: false,
              tooltip: {
                renderer: (params) => {
                  return `<div class="ag-chart-tooltip-title" style="background-color: ${params.color}">
                  ${t('common.game', { count: +params.datum['playCount'] })} with ${t('common.player', { count: +params.datum['players'] })}
                </div>`;
                },
              },
              calloutLabelKey: 'playCount',
              calloutLabel: {
                color: 'white',
                formatter: (params) => t('common.player', { count: +params.datum['players'] }),
              },
            },
          ],
          background: { visible: false },
          height: 300,
        }}
      />
    </BgtChartCard>
  );
};
