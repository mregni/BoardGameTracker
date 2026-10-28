import { useState } from "react";
import { type NumberValue, parseDecimal } from "@/utils/numberUtils";
import { useNumberFormat } from "./useNumberFormat";

const DECIMAL_DRAFT = /^[+-]?[\d.,\s]*$/;

export const useDecimalInput = (value: NumberValue, onValueChange: (value: number | null) => void) => {
	const { input, locale } = useNumberFormat();
	const [draft, setDraft] = useState<string | null>(null);

	return {
		value: draft ?? input(value),
		onChange: (raw: string) => {
			if (!DECIMAL_DRAFT.test(raw)) return;
			setDraft(raw);
			onValueChange(parseDecimal(raw, locale));
		},
		onBlur: () => setDraft(null),
	};
};
