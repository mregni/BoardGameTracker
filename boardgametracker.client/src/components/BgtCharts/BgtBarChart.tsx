import { useState, useEffect, useRef, useCallback } from 'react';
import { BarDatum, ResponsiveBar } from '@nivo/bar';

import { BgtText } from '../BgtText/BgtText';

const theme = {
  text: {
    fontFamily: 'Chakra Petch',
    fontSize: 12,
  },
  tooltip: {
    container: {
      background: 'transparent',
      padding: 0,
    },
  },
  axis: {
    ticks: {
      text: {
        fill: '#ffffff70',
      },
    },
  },
  grid: {
    line: {
      stroke: '#a855f730',
      strokeWidth: 1,
    },
  },
};

interface Props {
  data: BarDatum[];
  index: string;
  keys: string[];
}

export const BgtBarChart = (props: Props) => {
  const { data, index, keys } = props;
  const [isSmall, setIsSmall] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  const handleResize = useCallback((width: number) => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      setIsSmall(width < 700);
    }, 150);
  }, []);

  useEffect(() => {
    if (!containerRef.current) return;

    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        handleResize(entry.contentRect.width);
      }
    });

    resizeObserver.observe(containerRef.current);

    return () => {
      resizeObserver.disconnect();
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [handleResize]);

  if (!data || data.length === 0) return null;

  return (
    <div ref={containerRef} className="h-64">
      <ResponsiveBar
        data={data}
        keys={keys}
        indexBy={index}
        margin={{ top: 20, right: 20, bottom: isSmall ? 70 : 50, left: 50 }}
        padding={0.3}
        colors="#22d3ee"
        borderRadius={4}
        axisTop={null}
        axisRight={null}
        axisBottom={{
          tickSize: 5,
          tickPadding: 5,
          tickRotation: isSmall ? -45 : 0,
        }}
        axisLeft={{
          tickSize: 5,
          tickPadding: 5,
          tickRotation: 0,
        }}
        enableLabel={false}
        enableGridY={true}
        theme={theme}
        tooltip={({ value, indexValue }) => (
          <div className="bg-background border border-primary/30 rounded-lg p-3 shadow-lg min-w-32">
            <BgtText color="white">{indexValue}</BgtText>
            <BgtText color="cyan">{value} sessions</BgtText>
          </div>
        )}
      />
    </div>
  );
};
