/* eslint-disable @typescript-eslint/no-unsafe-member-access */
import { AgChartsReact } from 'ag-charts-react';
import { addDays, format } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';

import { BgtCard } from '../../../../components/BgtCard/BgtCard';
import { useGame } from '../../../../hooks/useGame';

export const PlaysByWeekDayChart = () => {
  const { id } = useParams();
  const { playsByDayChart } = useGame(id);
  const { t } = useTranslation();

  if (playsByDayChart === undefined) return null;

  return (
    <BgtCard contentStyle="h-[320px] flex flex-col">
      {/* <Text size='4' align="center">{t('game.charts.games-week-day')}</Text> */}
      {/* <ResponsiveBar
        data={playsByDayChart}
        keys={["playCount"]}
        indexBy="dayOfWeek"
        theme={customTheme}
        margin={{ top: 30, right: 10, bottom: 50, left: 50 }}
        padding={0.3}
        colors={{ scheme: 'category10' }}
        axisLeft={{
          tickSize: 5,
          tickPadding: 5,
          tickRotation: 0,
          legend: "Count",
          legendPosition: "middle",
          legendOffset: -40
        }}
        axisBottom={{
          tickSize: 10,
          format: value => format(addDays(new Date(2024, 3, 1), +value), 'EE')
        }}
      /> */}

      <AgChartsReact
        options={{
          data: playsByDayChart,
          series: [
            {
              type: 'bar',
              xKey: 'dayOfWeek',
              yKey: 'playCount',
              tooltip: {
                renderer: (params) =>
                  `<div class="ag-chart-tooltip-title flex flex-row justify-start gap-2 items-center" style="background-color:${params.color}">
              ${format(addDays(new Date(2024, 3, 1), +params.datum[params.xKey]), 'EEEE')}: ${t('common.game', { count: +params.datum[params.yKey] })}
            </div>`,
              },
            },
          ],
          background: { visible: false },
          title: {
            color: 'white',
            spacing: 15,
            text: t('game.charts.games-week-day'),
          },
          height: 300,
          axes: [
            {
              type: 'number',
              position: 'left',
              gridLine: {
                style: [
                  {
                    stroke: 'lightgray',
                  },
                ],
              },
            },
            {
              type: 'category',
              position: 'bottom',
              label: {
                formatter: (params) => format(addDays(new Date(2024, 3, 1), +params.value), 'EE'),
              },
            },
          ],
        }}
      />
    </BgtCard>
  );
};
