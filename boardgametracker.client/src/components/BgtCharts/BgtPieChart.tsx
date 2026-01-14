import { useTranslation } from 'react-i18next';
import { ResponsivePie } from '@nivo/pie';

import { BgtText } from '../BgtText/BgtText';

import { PieChartDatum, pieColors } from '@/models/Charts/PieChartDatum';

const theme = {
  text: {
    fontFamily: 'Chakra Petch',
    fontSize: 15,
  },
  tooltip: {
    container: {
      background: 'transparent',
      padding: 0,
    },
  },
};

interface Props {
  data: PieChartDatum[];
  showLegend?: boolean;
  tooltipPrefix: string;
}

export const BgtPieChart = (props: Props) => {
  const { data, showLegend = false, tooltipPrefix } = props;
  const { t } = useTranslation();

  if (!data || data.length === 0) return null;

  return (
    <>
      <div className="flex items-center justify-center mb-4">
        <div className="relative w-48 h-48">
          <ResponsivePie
            data={data}
            margin={{ top: 10, right: 10, bottom: 10, left: 10 }}
            innerRadius={0.6}
            padAngle={2}
            cornerRadius={3}
            activeOuterRadiusOffset={8}
            colors={pieColors}
            enableArcLinkLabels={false}
            enableArcLabels={false}
            borderWidth={0}
            borderColor={{
              from: 'color',
              modifiers: [['darker', 0.9]],
            }}
            theme={theme}
            tooltip={({ datum }) => {
              const total = data.reduce((sum, item) => sum + item.value, 0);
              const percentage = ((datum.value / total) * 100).toFixed(1);
              return (
                <div className="bg-background border border-primary/30 rounded-lg p-3 shadow-lg min-w-40">
                  <BgtText color="white">{datum.label}</BgtText>
                  <BgtText color="cyan">
                    {t(tooltipPrefix, { count: datum.value })} ({percentage}%)
                  </BgtText>
                </div>
              );
            }}
          />
          <div className="absolute inset-0 flex items-center justify-center flex-col pointer-events-none">
            <BgtText color="cyan" size="8">
              {data.reduce((sum, item) => sum + item.value, 0)}
            </BgtText>
            <div className="text-white/50 text-xs uppercase">{t('common.total')}</div>
          </div>
        </div>
      </div>
      {showLegend && (
        <div className="space-y-2">
          {data.map((item, index) => (
            <div key={index} className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-3 h-3 rounded-full" style={{ backgroundColor: pieColors[index] }} />
                <span className="text-white/70 text-sm">{item.label}</span>
              </div>
              <BgtText color="white">{item.value}</BgtText>
            </div>
          ))}
        </div>
      )}
    </>
  );
};
