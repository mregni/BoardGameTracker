import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { type ProgressPaint, paintClasses, paintStyle } from "./progressTones";

export interface BgtProgressLegendItem extends ProgressPaint {
	key?: string;
	label: ReactNode;
	value?: ReactNode;
}

interface Props {
	items: BgtProgressLegendItem[];
	className?: string;
	"aria-label"?: string;
}

export const BgtProgressLegend = (props: Props) => {
	const { items, className, "aria-label": ariaLabel } = props;

	if (items.length === 0) return null;

	return (
		<ul aria-label={ariaLabel} className={cx("flex flex-wrap gap-x-3 gap-y-1 text-xs text-white/60", className)}>
			{items.map((item, index) => (
				<li key={item.key ?? index} className="inline-flex items-center gap-1">
					<span
						aria-hidden="true"
						data-testid="progress-legend-swatch"
						data-pattern={item.pattern ?? "solid"}
						className={paintClasses(item, "primary", "inline-block size-2.5 shrink-0 rounded-[3px]")}
						style={paintStyle(item)}
					/>
					<span>{item.label}</span>
					{item.value !== undefined && <span className="tabular-nums text-white/80">{item.value}</span>}
				</li>
			))}
		</ul>
	);
};
