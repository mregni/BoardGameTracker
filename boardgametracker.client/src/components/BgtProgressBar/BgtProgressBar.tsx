import { cva, cx, type VariantProps } from "class-variance-authority";
import type { ReactNode } from "react";
import { BgtProgressLegend } from "./BgtProgressLegend";
import {
	clampValue,
	computeSegments,
	createDefaultFormatLabel,
	defaultFormatValue,
	type ProgressSummary,
	sanitize,
	toPercent,
} from "./progressMath";
import { type ProgressPaint, paintClasses, paintStyle } from "./progressTones";

const trackVariants = cva("relative flex w-full overflow-hidden", {
	variants: {
		size: {
			xs: "h-1.5 rounded-full",
			thin: "h-2 rounded",
			default: "h-2.5 rounded",
			lg: "h-3 rounded-[5px]",
			xl: "h-3.5 rounded-[5px]",
		},
		track: {
			default: "bg-white/10",
			primary: "bg-primary/10",
			card: "bg-card-light",
			none: "bg-transparent",
		},
		gapped: {
			true: "gap-0.5",
			false: "",
		},
	},
	defaultVariants: {
		size: "default",
		track: "default",
		gapped: false,
	},
});

type TrackVariants = Omit<VariantProps<typeof trackVariants>, "gapped">;

export interface BgtProgressSegment extends ProgressPaint {
	key?: string;
	label: string;
	value: number;
	title?: string;
}

interface BaseProps extends TrackVariants {
	max?: number;
	className?: string;
	id?: string;
	formatValue?: (value: number) => string;
	ticks?: ReactNode[];
}

export interface BgtProgressBarSingleProps extends BaseProps, ProgressPaint {
	value: number;
	title?: string;
	"aria-label"?: string;
	"aria-labelledby"?: string;
	"aria-valuetext"?: string;
	segments?: never;
}

export interface BgtProgressBarSegmentedProps extends BaseProps {
	segments: BgtProgressSegment[];
	formatLabel?: (summary: ProgressSummary) => string;
	legend?: boolean;
	gapped?: boolean;
	value?: never;
}

export type BgtProgressBarProps = BgtProgressBarSingleProps | BgtProgressBarSegmentedProps;

const motion = "motion-safe:transition-[width,flex-grow] motion-safe:duration-300 motion-safe:ease-out";

const Ticks = (props: { ticks: ReactNode[] }) => (
	<div data-testid="progress-ticks" className="flex justify-between text-[11px] text-white/50 tabular-nums">
		{props.ticks.map((tick, index) => (
			<span key={typeof tick === "string" || typeof tick === "number" ? `${tick}-${index}` : index}>{tick}</span>
		))}
	</div>
);

const SingleBar = (props: BgtProgressBarSingleProps) => {
	const {
		value,
		max = 100,
		size,
		track,
		tone,
		color,
		pattern,
		title,
		id,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledBy,
		"aria-valuetext": ariaValueText,
	} = props;
	const safeMax = sanitize(max);
	const percent = toPercent(value, safeMax);

	return (
		<div
			id={id}
			role="progressbar"
			aria-label={ariaLabel}
			aria-labelledby={ariaLabelledBy}
			aria-valuemin={0}
			aria-valuemax={safeMax}
			aria-valuenow={clampValue(value, safeMax)}
			aria-valuetext={ariaValueText}
			title={title}
			className={trackVariants({ size, track })}
		>
			<span
				data-testid="progress-fill"
				className={paintClasses(
					{ tone, color, pattern },
					"value",
					cx("block h-full rounded-[inherit]", percent > 0 && "min-w-1", motion),
				)}
				style={{ ...paintStyle({ color }), width: `${percent}%` }}
			/>
		</div>
	);
};

const SegmentedBar = (props: BgtProgressBarSegmentedProps) => {
	const { segments, max, size, track, gapped = false, id, formatLabel, formatValue = defaultFormatValue } = props;
	const computed = computeSegments(segments, max);
	const summary: ProgressSummary = {
		segments: segments.map((segment) => ({ label: segment.label, value: sanitize(segment.value) })),
		total: computed.total,
		max: computed.max,
		overflow: computed.overflow,
	};
	const label = (formatLabel ?? createDefaultFormatLabel(formatValue))(summary);

	return (
		<div
			id={id}
			role="img"
			aria-label={label}
			data-overflow={computed.overflow ? "true" : undefined}
			className={trackVariants({ size, track, gapped })}
		>
			{computed.items.map((item) => (
				<span
					key={item.segment.key ?? item.index}
					data-testid="progress-segment"
					data-tone={item.segment.color ? "custom" : (item.segment.tone ?? "primary")}
					data-pattern={item.segment.pattern ?? "solid"}
					title={item.segment.title ?? `${item.segment.label} · ${formatValue(item.value)}`}
					className={paintClasses(item.segment, "primary", cx("block h-full min-w-1 basis-0", motion))}
					style={{ ...paintStyle(item.segment), flexGrow: item.percent }}
				/>
			))}
			{computed.rest > 0 && (
				<span
					aria-hidden="true"
					data-testid="progress-rest"
					className={cx("block h-full basis-0", motion)}
					style={{ flexGrow: computed.rest }}
				/>
			)}
			{computed.overflow && (
				<span
					aria-hidden="true"
					data-testid="progress-overflow"
					className="pointer-events-none absolute inset-y-0 right-0 w-2 border-l-2 border-dashed border-warn bg-overflow-stripes"
				/>
			)}
		</div>
	);
};

const legendItems = (segments: BgtProgressSegment[], formatValue: (value: number) => string) =>
	segments.map((segment, index) => ({
		key: segment.key ?? String(index),
		label: segment.label,
		value: formatValue(sanitize(segment.value)),
		tone: segment.tone,
		color: segment.color,
		pattern: segment.pattern,
	}));

export const BgtProgressBar = (props: BgtProgressBarProps) => {
	const { className, ticks } = props;

	return (
		<div className={cx("flex min-w-0 flex-col gap-1.5", className)}>
			{props.segments === undefined ? <SingleBar {...props} /> : <SegmentedBar {...props} />}
			{ticks && ticks.length > 0 && <Ticks ticks={ticks} />}
			{props.segments !== undefined && props.legend && (
				<BgtProgressLegend items={legendItems(props.segments, props.formatValue ?? defaultFormatValue)} />
			)}
		</div>
	);
};

export type { ProgressPattern, ProgressTone } from "./progressTones";
export type { ProgressSummary };
