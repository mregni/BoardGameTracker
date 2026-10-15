import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import {
	formatDecimal,
	formatDecimalInput,
	formatFileSize,
	formatInteger,
	formatNumber,
	formatPercent,
	formatPrice,
	formatRatio,
	type NumberValue,
} from "@/utils/numberUtils";

export const useNumberFormat = (culture?: string | null) => {
	const { i18n } = useTranslation();
	const locale = culture?.trim() || i18n?.language;

	return useMemo(
		() => ({
			locale,
			number: (value: NumberValue, options?: Intl.NumberFormatOptions) => formatNumber(value, locale, options),
			decimal: (value: NumberValue, maximumFractionDigits?: number, minimumFractionDigits?: number) =>
				formatDecimal(value, locale, maximumFractionDigits, minimumFractionDigits),
			integer: (value: NumberValue) => formatInteger(value, locale),
			percent: (fraction: NumberValue, maximumFractionDigits?: number) =>
				formatPercent(fraction, locale, maximumFractionDigits),
			ratio: (value: number, total: number, maximumFractionDigits?: number) =>
				formatRatio(value, total, locale, maximumFractionDigits),
			price: (amount: NumberValue, currency: string | null | undefined) => formatPrice(amount, currency, locale),
			fileSize: (bytes: number) => formatFileSize(bytes, locale),
			input: (value: NumberValue) => formatDecimalInput(value, locale),
		}),
		[locale],
	);
};
