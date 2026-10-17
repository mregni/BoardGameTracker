export const formatRange = (min: number | null | undefined, max: number | null | undefined): string | null => {
	const low = min ?? max;
	const high = max ?? min;
	if (low == null || high == null) return null;
	return low === high ? `${low}` : `${low}–${high}`;
};
