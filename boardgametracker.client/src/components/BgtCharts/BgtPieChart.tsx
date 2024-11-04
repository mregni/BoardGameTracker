import { ReactNode } from 'react';
import { MayHaveLabel, ResponsivePie } from '@nivo/pie';

const theme = {
  text: {
    fontFamily: 'Chakra Petch',
    fontSize: 15,
    stroke: '#ffffff',
  },
};

interface Props<T> {
  data: MayHaveLabel[];
  labelPrinter: (label: T) => string;
  tooltip?: ReactNode;
}

export const BgtPieChart = <T,>(props: Props<T>) => {
  const { data, labelPrinter, tooltip = <></> } = props;
  return (
    <ResponsivePie
      data={data}
      innerRadius={0.5}
      padAngle={3}
      cornerRadius={3}
      activeOuterRadiusOffset={8}
      borderWidth={1}
      borderColor={{
        from: 'color',
        modifiers: [['darker', 0.9]],
      }}
      arcLinkLabel={(e) => labelPrinter(e.label as T)}
      arcLinkLabelsSkipAngle={10}
      arcLinkLabelsTextColor="#ffffff"
      arcLinkLabelsThickness={2}
      arcLinkLabelsColor={{ from: 'color' }}
      arcLabelsSkipAngle={10}
      arcLabelsTextColor={{
        from: 'color',
        modifiers: [['darker', 2]],
      }}
      theme={theme}
      margin={{ top: 40, right: 80, bottom: 40, left: 80 }}
      tooltip={() => tooltip}
      colors={{ scheme: 'category10' }}
    />
  );
};
