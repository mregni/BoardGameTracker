import { type BarDatum, ResponsiveBar } from "@nivo/bar";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { getIntegerTicks } from "@/utils/numberUtils";

import { BgtText } from "../BgtText/BgtText";

const theme = {
	text: {
		fontFamily: "Chakra Petch",
		fontSize: 12,
	},
	tooltip: {
		container: {
			background: "transparent",
			padding: 0,
		},
	},
	axis: {
		ticks: {
			text: {
				fill: "#ffffff70",
			},
		},
	},
	grid: {
		line: {
			stroke: "#24304430",
			strokeWidth: 1,
		},
	},
};

interface Props {
	data: BarDatum[];
	index: string;
	keys: string[];
	tickFormat?: (value: string | number) => string;
}

export const BgtBarChart = (props: Props) => {
	const { data, index, keys, tickFormat } = props;
	const { t } = useTranslation();
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

	const ticks = useMemo(
		() => getIntegerTicks(Math.max(0, ...(data ?? []).flatMap((datum) => keys.map((key) => Number(datum[key]) || 0)))),
		[data, keys],
	);

	if (!data || data.length === 0) return null;

	const rotateTicks = isSmall && !tickFormat;

	return (
		<div ref={containerRef} className="h-full min-h-64">
			<ResponsiveBar
				data={data}
				keys={keys}
				indexBy={index}
				margin={{ top: 20, right: 20, bottom: rotateTicks ? 70 : 50, left: 50 }}
				padding={0.3}
				colors="#22d3ee"
				borderRadius={4}
				axisTop={null}
				axisRight={null}
				axisBottom={{
					tickSize: 5,
					tickPadding: 5,
					tickRotation: rotateTicks ? -45 : 0,
					format: tickFormat,
				}}
				axisLeft={{
					tickSize: 5,
					tickPadding: 5,
					tickRotation: 0,
					tickValues: ticks,
				}}
				valueScale={{ type: "linear", min: 0, max: ticks[ticks.length - 1] }}
				gridYValues={ticks}
				enableLabel={false}
				enableGridY={true}
				theme={theme}
				tooltip={({ value, indexValue }) => (
					<div className="bg-background border border-primary/30 rounded-lg p-3 shadow-lg min-w-32">
						<BgtText color="white">{indexValue}</BgtText>
						<BgtText color="cyan">{t("common:sessions-count", { count: Number(value) })}</BgtText>
					</div>
				)}
			/>
		</div>
	);
};
