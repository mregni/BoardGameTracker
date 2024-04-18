import {AgChartsReact} from 'ag-charts-react';
import {renderToString} from 'react-dom/server';
import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {BgtAvatar} from '../../../../components/BgtAvatar/BgtAvatar';
import {BgtCard} from '../../../../components/BgtCard/BgtCard';
import {useGame} from '../../../../hooks/useGame';
import {usePlayers} from '../../../../hooks/usePlayers';
import {RoundDecimal} from '../../../../utils/numberUtils';

const GetFillColor = (xKey: string, fallback: string | undefined): string | undefined => {
  switch (xKey) {
    case "top-score": return '#008000';
    case "highest-losing": return '#2B5600';
    case "average": return '#562B00';
    case "lowest-winning": return '#802B00';
    case "lowest": return '#800000';
    default: return fallback;
  }
}

export const ScoringRankChart = () => {
  const { id } = useParams();
  const { scoreRankChart, game } = useGame(id);
  const { byId } = usePlayers();
  const { t } = useTranslation();

  if (scoreRankChart === undefined || !game?.hasScoring) return null;

  return (
    <BgtCard>
      <AgChartsReact options={{
        data: scoreRankChart,
        series: [
          {
            type: 'bar',
            direction: "horizontal",
            xKey: 'key',
            yKey: 'score',
            legendItemName: "boe",
            label: {
              enabled: true,
              placement: 'inside',
              fontStyle: 'italic',
              formatter: (params) => `${byId(params.datum['playerId'])?.name ?? ''} ${RoundDecimal(params.datum[params.yKey])}`
            },
            tooltip: {
              renderer: (params) => {
                if (params.datum['playerId'] === undefined) {
                  return (
                    `<div class="ag-chart-tooltip-title flex flex-row justify-start gap-2 items-center" style="background-color:${params.color}">
                      ${RoundDecimal(params.datum[params.yKey])}
                    </div>`
                  )
                }

                return (
                  `<div class="ag-chart-tooltip-title flex flex-row justify-start gap-2 items-center" style="background-color:${params.color}">
                  ${renderToString(<BgtAvatar
                    title={byId(params.datum['playerId'])?.name}
                    image={byId(params.datum['playerId'])?.image}
                    noTooltip
                  />)}
                  ${byId(params.datum['playerId'])?.name}: ${RoundDecimal(params.datum[params.yKey], 0.1)}
                </div>`
                )
              },
            },
            formatter: (params) => ({ fill: GetFillColor(params.datum['key'], params.fill) }),
          }
        ],
        background: { visible: false },
        title: { text: t('game.charts.top-scoring.title') },
        height: 300,
        axes: [
          {
            type: "category",
            position: "left",
            label: {
              formatter: (params) => t(`game.charts.top-scoring.${params.value}`),
            }
          },
          {
            type: "number",
            position: "bottom"
          },
        ],
      }} />
    </BgtCard>
  )
}