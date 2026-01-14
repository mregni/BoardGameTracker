import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { ResponsiveBar } from '@nivo/bar';

import { useElementSize } from '../../-hooks/useElementSize';

import { RoundDecimal } from '@/utils/numberUtils';
import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import { ScoreRankChartData } from '@/models';
import { BgtChartCard } from '@/components/BgtCard/BgtChartCard';

const fillColors: Record<string, string> = {
  'top-score': '#008000',
  'highest-losing': '#2B5600',
  average: '#562B00',
  'lowest-winning': '#802B00',
  lowest: '#800000',
};

const theme = {
  grid: {
    line: {
      stroke: '#333333',
    },
  },
  axis: {
    ticks: {
      text: {
        fontSize: 15,
        fill: '#ffffff',
        fontFamily: 'Chakra Petch',
      },
    },
  },
};

interface Props {
  hasScoring: boolean;
  data: ScoreRankChartData[];
}

export const ScoringRankChart = ({ hasScoring, data }: Props) => {
  const { playerById } = usePlayerById();
  const { t } = useTranslation();
  const [divRef, { width }] = useElementSize();

  const barData = useMemo(() => {
    return data
      .map((rank) => ({
        key: rank.key,
        score: rank.score,
        playerId: rank.playerId,
      }))
      .reverse();
  }, [data]);

  if (!hasScoring) return null;

  const getTickValues = () => {
    if (width < 350) return 3;
    if (width < 550) return 5;
    if (width < 600) return 8;
    return 10;
  };

  return (
    <>
      <div className="col-span-1" ref={divRef}>
        <BgtChartCard title={t('game.charts.top-scoring.title')} className="h-96">
          <ResponsiveBar
            data={barData}
            theme={theme}
            keys={['score']}
            indexBy="key"
            margin={{ top: 0, right: 20, bottom: 50, left: 120 }}
            padding={0.3}
            layout="horizontal"
            colors={(id) => fillColors[id.data.key]}
            borderRadius={5}
            enableGridY={false}
            enableGridX
            enableLabel={false}
            labelSkipWidth={100}
            tooltip={() => <></>}
            axisLeft={{
              format: (value) => t(`game.charts.top-scoring.${value}`),
            }}
            axisBottom={{
              tickSize: 1,
              tickPadding: 5,
              tickRotation: 0,
              tickValues: getTickValues(),
            }}
            layers={[
              'grid',
              'axes',
              'bars',
              'markers',
              'legends',
              'annotations',
              ({ bars, labelSkipWidth }) => {
                return (
                  <g>
                    {bars.map(({ width, height, y, data }) => {
                      // only show this custom outer label on bars that are too small
                      return width < labelSkipWidth ? (
                        <text
                          fill="#ffffff"
                          transform={`translate(${width + 10}, ${y + height / 2})`}
                          dominantBaseline="central"
                          className="uppercase"
                          key={data.data.playerId + y}
                        >
                          {playerById(data.data.playerId)
                            ? `${playerById(data.data.playerId)?.name} ${data.data.score}`
                            : RoundDecimal(data.data.score)}
                        </text>
                      ) : (
                        <text
                          fill="#ffffff"
                          transform={`translate(${width - 10}, ${y + height / 2})`}
                          textAnchor="end"
                          dominantBaseline="central"
                          className="uppercase"
                          key={data.data.playerId + y}
                        >
                          {' '}
                          {playerById(data.data.playerId)
                            ? `${playerById(data.data.playerId)?.name} ${data.data.score}`
                            : RoundDecimal(data.data.score)}
                        </text>
                      );
                    })}
                  </g>
                );
              },
            ]}
          />
        </BgtChartCard>
      </div>
    </>
  );
};
