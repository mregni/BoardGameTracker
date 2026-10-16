import { BgtSimpleCheckbox } from "./BgtSimpleCheckbox";

interface ListItem {
	id: number;
	value: string;
}

interface CheckboxListProps<T extends ListItem> {
	items: T[];
	selectedIds?: number[];
	onSelectionChange?: (selectedIds: number[]) => void;
	disabled?: boolean;
	renderLabel?: (item: T) => string;
}

export const BgtCheckboxList = <T extends ListItem>(props: CheckboxListProps<T>) => {
	const { items, selectedIds = [], onSelectionChange, disabled = false, renderLabel = (item) => item.value } = props;

	const handleCheckedChange = (id: number, checked: boolean) => {
		const remaining = selectedIds.filter((selectedId) => selectedId !== id);
		onSelectionChange?.(checked ? [...remaining, id] : remaining);
	};

	return (
		<div className="space-y-3">
			{items.map((item) => (
				<BgtSimpleCheckbox
					key={item.id}
					id={`item-${item.id}`}
					label={renderLabel(item)}
					checked={selectedIds.includes(item.id)}
					onCheckedChange={(checked) => handleCheckedChange(item.id, checked)}
					disabled={disabled}
				/>
			))}
		</div>
	);
};
