export interface ProgressSegmentValue {
	label: string;
	value: number;
}

export interface ProgressSummary {
	segments: ProgressSegmentValue[];
	total: number;
	max: number;
	overflow: boolean;
}

export interface ComputedSegment<T extends ProgressSegmentValue> {
	segment: T;
	index: number;
	value: number;
	shown: number;
	percent: number;
	visible: boolean;
}

export interface ComputedSegments<T extends ProgressSegmentValue> {
	items: ComputedSegment<T>[];
	total: number;
	max: number;
	overflow: boolean;
	rest: number;
}

export const sanitize = (value: number): number => (Number.isFinite(value) && value > 0 ? value : 0);

export const clampValue = (value: number, max: number): number => Math.min(sanitize(value), sanitize(max));

export const toPercent = (value: number, max: number): number => {
	const safeMax = sanitize(max);
	if (safeMax === 0) return 0;
	return Math.round((clampValue(value, safeMax) / safeMax) * 10000) / 100;
};

export const computeSegments = <T extends ProgressSegmentValue>(segments: T[], max?: number): ComputedSegments<T> => {
	const values = segments.map((segment) => sanitize(segment.value));
	const total = values.reduce((sum, value) => sum + value, 0);
	const requestedMax = max === undefined ? 0 : sanitize(max);
	const effectiveMax = requestedMax > 0 ? requestedMax : total;
	const overflow = requestedMax > 0 && total > requestedMax;

	let remaining = effectiveMax;
	const items: ComputedSegment<T>[] = [];
	segments.forEach((segment, index) => {
		const value = values[index];
		const shown = Math.min(value, remaining);
		remaining -= shown;
		items.push({ segment, index, value, shown, percent: toPercent(shown, effectiveMax), visible: shown > 0 });
	});

	return {
		items,
		total,
		max: effectiveMax,
		overflow,
		rest: toPercent(remaining, effectiveMax),
	};
};

export const defaultFormatValue = (value: number): string => String(Math.round(value * 100) / 100);

export const createDefaultFormatLabel =
	(formatValue: (value: number) => string = defaultFormatValue) =>
	(summary: ProgressSummary): string => {
		const parts = summary.segments
			.filter((segment) => segment.value > 0)
			.map((segment) => `${segment.label} ${formatValue(segment.value)}`);
		if (parts.length === 0) return "";
		return `${parts.join(", ")} of ${formatValue(summary.max)}`;
	};
