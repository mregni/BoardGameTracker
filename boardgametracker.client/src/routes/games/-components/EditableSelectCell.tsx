import { cx } from "class-variance-authority";
import { BgtSimpleSelect } from "@/components/BgtForm";
import type { BgtSelectItem } from "@/models";
import { EditableCellButton } from "./EditableCellButton";

interface Props {
	value: string;
	items: BgtSelectItem[];
	editing: boolean;
	onStartEdit: () => void;
	onStopEdit: () => void;
	onChange: (value: string) => void;
	hasSearch?: boolean;
	className?: string;
	align?: "left" | "right";
	readOnly?: boolean;
}

export const EditableSelectCell = (props: Props) => {
	const {
		value,
		items,
		editing,
		onStartEdit,
		onStopEdit,
		onChange,
		hasSearch = false,
		className,
		align,
		readOnly,
	} = props;

	if (!editing || readOnly) {
		return (
			<EditableCellButton onClick={onStartEdit} className={className} align={align} readOnly={readOnly}>
				{items.find((item) => String(item.value) === value)?.label ?? "-"}
			</EditableCellButton>
		);
	}

	return (
		<BgtSimpleSelect
			value={value}
			items={items}
			hasSearch={hasSearch}
			defaultOpen
			onOpenChange={(open) => {
				if (!open) {
					onStopEdit();
				}
			}}
			onValueChange={(next) => {
				onChange(String(next));
				onStopEdit();
			}}
			className={cx("w-full", className)}
		/>
	);
};
