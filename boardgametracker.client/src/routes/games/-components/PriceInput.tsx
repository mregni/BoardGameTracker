import { useEffect, useState } from "react";
import { BgtSimpleInputField } from "@/components/BgtForm";
import { useDecimalInput } from "@/hooks/useDecimalInput";

interface PriceInputProps {
	value: number | null;
	onCommit: (value: number | null) => void;
	ariaLabel: string;
	placeholder: string;
	currency?: string;
	prefix?: string;
}

export const PriceInput = ({ value, onCommit, ariaLabel, placeholder, currency, prefix }: PriceInputProps) => {
	const [current, setCurrent] = useState<number | null>(value);
	useEffect(() => setCurrent(value), [value]);
	const decimal = useDecimalInput(current, setCurrent);

	return (
		<BgtSimpleInputField
			type="text"
			inputMode="decimal"
			value={decimal.value}
			onChange={(event) => decimal.onChange(event.target.value)}
			onBlur={() => {
				decimal.onBlur();
				if (current !== value && (current === null || current >= 0)) {
					onCommit(current);
				}
			}}
			placeholder={placeholder}
			prefixLabel={prefix}
			suffixLabel={currency}
			ariaLabel={ariaLabel}
			className="w-full"
		/>
	);
};
