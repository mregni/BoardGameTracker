import { cx } from "class-variance-authority";
import { BgtSimpleSelect } from "@/components/BgtForm";
import type { BgtSelectItem } from "@/models";

interface Props {
	value: string;
	items: BgtSelectItem[];
	onChange: (value: string) => void;
	hasSearch?: boolean;
	className?: string;
}

export const EditableSelectCell = ({ value, items, onChange, hasSearch = false, className }: Props) => {
	return (
		<BgtSimpleSelect
			value={value}
			items={items}
			hasSearch={hasSearch}
			onValueChange={(next) => onChange(String(next))}
			className={cx("w-36", className)}
		/>
	);
};
