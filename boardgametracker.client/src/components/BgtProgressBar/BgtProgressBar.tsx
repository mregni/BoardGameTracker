import { cva, cx, type VariantProps } from "class-variance-authority";
import { type ReactNode, useEffect, useState } from "react";
import { useMediaQuery } from "@/hooks/useMediaQuery";
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
	},
	defaultVariants: {
		size: "default",
		track: "default",
	},
});

type TrackVariants = VariantProps<typeof trackVariants>;

export interface BgtProgressSegment extends ProgressPaint {
	key?: string;
	label: string;
	value: number;
	title?: string;
}

interface SingleBarProps extends TrackVariants, ProgressPaint {
	value: number;
	max?: number;
	id?: string;
	title?: string;
	animateOnMount?: boolean;
	"aria-label"?: string;
	"aria-labelledby"?: string;
	"aria-valuetext"?: string;
}

interface SegmentedBarProps extends TrackVariants {
	segments: BgtProgressSegment[];
	max?: number;
	id?: string;
	gapped?: boolean;
	formatLabel?: (summary: ProgressSummary) => string;
	formatValue?: (value: number) => string;
	animateOnMount?: boolean;
	growFromEnds?: boolean;
}

interface FrameProps {
	className?: string;
	ticks?: ReactNode[];
}

export interface BgtProgressBarSingleProps extends SingleBarProps, FrameProps {}

export interface BgtProgressBarSegmentedProps extends SegmentedBarProps, FrameProps {
	legend?: boolean;
}

export type BgtProgressBarProps = BgtProgressBarSingleProps | BgtProgressBarSegmentedProps;

const motion =
	"motion-safe:transition-[width,flex-grow,margin,background-color] motion-safe:duration-300 motion-safe:ease-out motion-reduce:transition-none";

const Ticks = (props: { ticks: ReactNode[] }) => (
	<div data-testid="progress-ticks" className="flex justify-between text-[11px] text-white/50 tabular-nums">
		{props.ticks.map((tick, index) => (
			<span key={typeof tick === "string" || typeof tick === "number" ? `${tick}-${index}` : index}>{tick}</span>
		))}
	</div>
);

const SingleBar = (props: SingleBarProps) => {
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
		animateOnMount = false,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledBy,
		"aria-valuetext": ariaValueText,
	} = props;
	const safeMax = sanitize(max);
	const grown = useGrownOnMount(animateOnMount);
	const percent = grown ? toPercent(value, safeMax) : 0;

	return (
		<div data-testid="progress-track" title={title} className={trackVariants({ size, track })}>
			<progress
				id={id}
				className="sr-only"
				value={clampValue(value, safeMax)}
				max={safeMax > 0 ? safeMax : 1}
				aria-label={ariaLabel}
				aria-labelledby={ariaLabelledBy}
				aria-valuetext={ariaValueText}
			/>
			<span
				aria-hidden="true"
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

const useGrownOnMount = (animateOnMount: boolean): boolean => {
	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)");
	const [grown, setGrown] = useState(!animateOnMount);

	useEffect(() => {
		if (grown) return;
		let second = 0;
		const grow = () => setGrown(true);
		const first = requestAnimationFrame(() => {
			second = requestAnimationFrame(grow);
		});
		const fallback = setTimeout(grow, 100);
		return () => {
			cancelAnimationFrame(first);
			cancelAnimationFrame(second);
			clearTimeout(fallback);
		};
	}, [grown]);

	return grown || reduceMotion;
};

const SegmentedBar = (props: SegmentedBarProps) => {
	const {
		segments,
		max,
		size,
		track,
		gapped = false,
		id,
		formatLabel,
		formatValue = defaultFormatValue,
		animateOnMount = false,
		growFromEnds = false,
	} = props;
	const grown = useGrownOnMount(animateOnMount);
	const computed = computeSegments(segments, max);
	const firstVisible = computed.items.findIndex((item) => item.visible);
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
			className={trackVariants({ size, track })}
		>
			{computed.items.map((item, position) => [
				growFromEnds && position === computed.items.length - 1 && (
					<span
						key="grow-gap"
						aria-hidden="true"
						className={cx("block h-full shrink-0 basis-0", motion)}
						style={{ flexGrow: grown ? 0 : 100 }}
					/>
				),
				<span
					key={item.segment.key ?? item.index}
					aria-hidden={item.visible ? undefined : "true"}
					data-testid="progress-segment"
					data-visible={item.visible ? "true" : "false"}
					data-tone={item.segment.color ? "custom" : (item.segment.tone ?? "primary")}
					data-pattern={item.segment.pattern ?? "solid"}
					title={
						item.visible ? (item.segment.title ?? `${item.segment.label} · ${formatValue(item.value)}`) : undefined
					}
					className={paintClasses(
						item.segment,
						"primary",
						cx(
							"block h-full shrink-0 basis-0",
							item.visible && grown ? "min-w-1" : "min-w-0",
							gapped && item.visible && position > firstVisible ? "ml-0.5" : "ml-0",
							motion,
						),
					)}
					style={{ ...paintStyle(item.segment), flexGrow: grown ? item.percent : 0 }}
				/>,
			])}
			<span
				aria-hidden="true"
				data-testid="progress-rest"
				className={cx("block h-full shrink-0 basis-0", motion)}
				style={{ flexGrow: grown || growFromEnds ? computed.rest : 100 }}
			/>
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
	segments
		.map((segment, index) => ({ segment, index, value: sanitize(segment.value) }))
		.filter((entry) => entry.value > 0)
		.map(({ segment, index, value }) => ({
			key: segment.key ?? String(index),
			label: segment.label,
			value: formatValue(value),
			tone: segment.tone,
			color: segment.color,
			pattern: segment.pattern,
		}));

export const BgtProgressBar = (props: BgtProgressBarProps) => {
	const { className, ticks } = props;

	return (
		<div className={cx("flex min-w-0 flex-col gap-1.5", className)}>
			{"segments" in props ? <SegmentedBar {...props} /> : <SingleBar {...props} />}
			{ticks && ticks.length > 0 && <Ticks ticks={ticks} />}
			{"segments" in props && props.legend && (
				<BgtProgressLegend items={legendItems(props.segments, props.formatValue ?? defaultFormatValue)} />
			)}
		</div>
	);
};

export type { ProgressPattern, ProgressTone } from "./progressTones";
export type { ProgressSummary };
