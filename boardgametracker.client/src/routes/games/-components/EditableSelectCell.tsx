import { cx } from "class-variance-authority";
import { BgtSimpleSelect } from "@/components/BgtForm";
import type { BgtSelectItem } from "@/models";
import { EditableCellButton } from "./EditableCellButton";

interface Props {
	label: string;
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
	emptyValue?: string;
	size?: "xs" | "sm";
	display?: string;
	flash?: boolean;
	onFlashEnd?: () => void;
}

export const EditableSelectCell = (props: Props) => {
	const {
		label,
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
		emptyValue,
		size,
		display,
		flash,
		onFlashEnd,
	} = props;

	if (!editing || readOnly) {
		return (
			<EditableCellButton
				onClick={onStartEdit}
				className={cx(className, flash && "animate-value-flash")}
				align={align}
				readOnly={readOnly}
				size={size}
				onAnimationEnd={flash ? onFlashEnd : undefined}
			>
				{display ?? (value === emptyValue ? "-" : (items.find((item) => String(item.value) === value)?.label ?? "-"))}
			</EditableCellButton>
		);
	}

	return (
		<BgtSimpleSelect
			aria-label={label}
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
