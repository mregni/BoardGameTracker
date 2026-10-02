export const RoundDecimal = (value: number | null, increment: number = 1): number | null => {
	if (value === null) return null;
	const precision = Math.round(-Math.log10(increment));
	const result = Math.round(value / increment) * increment;
	return precision > 0 ? Number(result.toFixed(precision)) : result;
};

export const GetPercentage = (value: number, total: number): number => {
	if (total === 0) return 0;
	return Math.round((value / total) * 100);
};

export const formatFileSize = (bytes: number): string => {
	if (bytes < 1024) {
		return `${bytes} B`;
	}

	const kilobytes = bytes / 1024;
	if (kilobytes < 1024) {
		return `${kilobytes.toFixed(1)} KB`;
	}

	return `${(kilobytes / 1024).toFixed(1)} MB`;
};
