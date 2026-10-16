import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import Cross from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtSelect } from "@/components/BgtForm/BgtSelect";

interface Option {
	value: number;
	label: string;
	image?: string | null;
}

interface Props {
	label: string;
	options: Option[];
	selected: number[];
	disabled: boolean;
	onChange: (selected: number[]) => void;
	placeholder?: string;
}

export const MultiSelectField = (props: Props) => {
	const { label, options, selected, onChange, placeholder, disabled } = props;
	const { t } = useTranslation();

	const available = useMemo(
		() =>
			options
				.filter((option) => !selected.includes(option.value))
				.map((option) => ({ ...option, image: option.image ?? null })),
		[options, selected],
	);
	const selectedItems = useMemo(() => options.filter((option) => selected.includes(option.value)), [options, selected]);

	return (
		<div className="flex w-full flex-col gap-2">
			<BgtSelect
				label={label}
				value={null}
				onValueChange={(value) => onChange([...selected, Number(value)])}
				items={available}
				placeholder={placeholder}
				disabled={disabled}
				hasSearch
				showAvatars
			/>
			{selectedItems.length > 0 && (
				<div className="flex flex-wrap gap-2">
					{selectedItems.map((item) => (
						<div
							key={item.value}
							className="pl-1 pr-2 py-1 bg-primary/20 border border-primary/30 rounded text-sm flex items-center gap-2"
						>
							{item.image !== undefined && <BgtAvatar title={item.label} image={item.image} size="medium" />}
							<span className="text-primary">{item.label}</span>
							<button
								type="button"
								onClick={() => onChange(selected.filter((value) => value !== item.value))}
								disabled={disabled}
								aria-label={t("common:remove-item", { name: item.label })}
								className="text-primary cursor-pointer hover:text-white transition-colors py-2"
							>
								<Cross className="size-3" />
							</button>
						</div>
					))}
				</div>
			)}
		</div>
	);
};
