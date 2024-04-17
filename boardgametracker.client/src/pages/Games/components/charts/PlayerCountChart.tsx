import {AgChartsReact} from 'ag-charts-react';
import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {BgtCard} from '../../../../components/BgtCard/BgtCard';
import {useGame} from '../../../../hooks/useGame';

export const PlayerCountChart = () => {
  const { id } = useParams();
  const { playerCountChart } = useGame(id);
  const { t } = useTranslation();

  return (
    <BgtCard noTitleSpacing>
      <AgChartsReact options={{
        data: playerCountChart,
        series: [{
          type: 'pie',
          angleKey: 'playCount',
          legendItemKey: 'players',
          showInLegend: false,
          tooltip: {
            renderer: (params) => {
              return (
                `<div class="ag-chart-tooltip-title" style="background-color: ${params.color}">
                  ${t('common.game', { count: +params.datum['playCount']})} with ${t('common.player', { count: +params.datum['players']})}
                </div>`
              )
            },
          },
          calloutLabelKey: 'playCount',
          calloutLabel: {
            color: 'white',
            formatter: (params) => t('common.game', { count: +params.value }),
          }
        }],
        background: { visible: false },
        title: { text: t('game.charts.player-count') },
        height: 300
      }} />
    </BgtCard>
  )
}