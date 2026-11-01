import { cx } from "class-variance-authority";
import type { ChangeEventHandler } from "react";
import { useTranslation } from "react-i18next";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtSimpleInputField } from "./BgtSimpleInputField";

interface Props {
	value: string | number | Date | undefined;
	onChange: ChangeEventHandler<HTMLInputElement> | undefined;
	onClear?: () => void;
	placeholder?: string;
	className?: string;
	inputClassName?: string;
}

export const SearchInputField = (props: Props) => {
	const { value, onChange, onClear, placeholder, className, inputClassName } = props;
	const { t } = useTranslation();
	const label = placeholder || t("filter-name");

	return (
		<BgtSimpleInputField
			value={value}
			onChange={onChange}
			className={cx(
				"border-primary/30 border bg-primary/10 w-full transition-colors focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/40",
				className,
			)}
			inputClassName={cx("placeholder:text-primary-text", inputClassName)}
			placeholder={label}
			ariaLabel={label}
			type="text"
			prefixLabel={<SearchIcon aria-hidden="true" className="text-primary size-5" />}
			suffixLabel={
				onClear && value ? (
					<button
						type="button"
						onClick={onClear}
						aria-label={t("clear-search")}
						className="-mr-2 flex size-7 shrink-0 cursor-pointer items-center justify-center rounded-md text-white/60 transition-colors hover:text-white focus-visible:outline-2 focus-visible:outline-primary"
					>
						<XIcon aria-hidden="true" className="size-4" />
					</button>
				) : undefined
			}
		/>
	);
};
