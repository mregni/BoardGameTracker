export const formatPrice = (
	amount: number | null | undefined,
	currency: string | null | undefined,
	locale?: string | null,
): string => {
	if (amount === null || amount === undefined || Number.isNaN(amount)) {
		return "-";
	}

	const fractionDigits = Number.isInteger(Math.round(amount * 100) / 100) ? 0 : 2;
	let formatted: string;
	try {
		formatted = new Intl.NumberFormat(locale ?? undefined, {
			minimumFractionDigits: fractionDigits,
			maximumFractionDigits: fractionDigits,
		}).format(amount);
	} catch {
		formatted = amount.toFixed(fractionDigits);
	}

	const prefix = currency?.trim() ?? "";
	if (prefix.length === 0) {
		return formatted;
	}

	return prefix.length > 1 ? `${prefix} ${formatted}` : `${prefix}${formatted}`;
};
