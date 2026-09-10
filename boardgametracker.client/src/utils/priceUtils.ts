export const formatPrice = (amount: number, currency: string | null | undefined, locale?: string | null): string => {
	let formatted: string;
	try {
		formatted = new Intl.NumberFormat(locale ?? undefined, {
			minimumFractionDigits: 2,
			maximumFractionDigits: 2,
		}).format(amount);
	} catch {
		formatted = amount.toFixed(2);
	}

	const prefix = currency?.trim() ?? "";
	if (prefix.length === 0) {
		return formatted;
	}

	return prefix.length > 1 ? `${prefix} ${formatted}` : `${prefix}${formatted}`;
};
