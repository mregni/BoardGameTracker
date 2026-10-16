import { cva, cx } from "class-variance-authority";
import { type ProgressTone, progressToneVariants } from "./progressTones";

export type BgtProgressDotState = "on" | "off" | "bad";

export interface BgtProgressDot {
	key?: string;
	state: BgtProgressDotState;
	label: string;
}

const dotVariants = cva("block shrink-0 rounded-[3px] border", {
	variants: {
		size: {
			sm: "size-2",
			default: "size-2.5",
		},
		state: {
			on: "border-transparent",
			off: "border-card-border bg-card",
			bad: "border-bad/60 bg-bad/35",
		},
	},
	defaultVariants: {
		size: "default",
	},
});

interface Props {
	items: BgtProgressDot[];
	tone?: ProgressTone;
	size?: "sm" | "default";
	label?: string;
	formatLabel?: (items: BgtProgressDot[]) => string;
	className?: string;
}

const defaultFormatLabel = (label: string | undefined) => (items: BgtProgressDot[]) => {
	const states = items.map((item) => item.label).join(", ");
	return label ? `${label}: ${states}` : states;
};

export const BgtProgressDots = (props: Props) => {
	const { items, tone = "ok", size, label, formatLabel, className } = props;
	const ariaLabel = (formatLabel ?? defaultFormatLabel(label))(items);

	return (
		<div role="img" aria-label={ariaLabel} className={cx("flex flex-wrap gap-[3px]", className)}>
			{items.map((item, index) => (
				<span
					key={item.key ?? index}
					data-testid="progress-dot"
					data-state={item.state}
					title={item.label}
					className={dotVariants({
						size,
						state: item.state,
						className: item.state === "on" ? progressToneVariants({ tone }) : undefined,
					})}
				/>
			))}
		</div>
	);
};
