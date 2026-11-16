import { cx } from "class-variance-authority";
import { type DragEvent, type ReactNode, useState } from "react";
import { useTranslation } from "react-i18next";
import { BoxOrientation, StorageLayout, type StorageLocation } from "@/models";
import { childrenOf, itemsAt, nodeName, type StorageItem, type StoragePlace } from "@/utils/storage";
import { StorageBox } from "./StorageBox";

interface Props {
	cabinet: StorageLocation;
	nodes: StorageLocation[];
	items: StorageItem[];
	selectedKey: string | null;
	onSelect: (item: StorageItem) => void;
	onMove?: (draggedKey: string, slot: StorageLocation, beforeKey: string | null) => void;
}

interface SlotProps extends Omit<Props, "cabinet"> {
	slot: StorageLocation;
	grid: boolean;
}

const Slot = ({ slot, nodes, items, selectedKey, onSelect, onMove, grid }: SlotProps) => {
	const { t } = useTranslation("storage");
	const [over, setOver] = useState(false);
	const place: StoragePlace = { locationId: slot.locationId, storageLocationId: slot.id };
	const here = itemsAt(items, place);
	const name = nodeName(slot, nodes, t);

	const drop = (event: DragEvent) => {
		event.preventDefault();
		setOver(false);
		const dragged = event.dataTransfer.getData("text/plain");
		if (dragged) onMove?.(dragged, slot, null);
	};

	const content: ReactNode[] = [];
	let pile: StorageItem[] = [];
	const flush = () => {
		if (pile.length === 0) return;
		const stack = pile;
		content.push(
			<div key={`pile-${stack[0].key}`} className="flex flex-col-reverse gap-0.5 pointer-coarse:gap-1.5">
				{stack.map((item) => (
					<StorageBox
						key={item.key}
						item={item}
						selected={item.key === selectedKey}
						onSelect={onSelect}
						onDropBefore={onMove ? (dragged, before) => onMove(dragged, slot, before) : undefined}
					/>
				))}
			</div>,
		);
		pile = [];
	};
	for (const item of here) {
		if (item.orientation === BoxOrientation.Flat) {
			pile.push(item);
			continue;
		}
		flush();
		content.push(
			<StorageBox
				key={item.key}
				item={item}
				large={grid}
				selected={item.key === selectedKey}
				onSelect={onSelect}
				onDropBefore={onMove ? (dragged, before) => onMove(dragged, slot, before) : undefined}
			/>,
		);
	}
	flush();

	return (
		<fieldset
			onDragOver={(event) => {
				if (!onMove) return;
				event.preventDefault();
				setOver(true);
			}}
			onDragLeave={(event) => {
				if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setOver(false);
			}}
			onDrop={drop}
			className={cx(
				"relative m-0 flex items-end gap-[3px] rounded-[3px] border-0 bg-page-black pointer-coarse:gap-1.5",
				grid
					? "min-h-[calc(var(--box)*1.45)] min-w-[calc(var(--box)*1.45)] max-w-[calc(var(--box)*3)] flex-wrap content-end p-1.5 pt-4"
					: "min-h-[calc(var(--box)+22px)] min-w-[calc(var(--box)*4.4)] px-2 pt-4 shadow-[inset_0_-4px_0_rgb(255_255_255/0.07)]",
				over && "bg-card-value/10 shadow-[inset_0_0_0_1px_var(--color-card-value)]",
			)}
		>
			<legend className="sr-only">{name}</legend>
			<span
				aria-hidden="true"
				className="pointer-events-none absolute right-1.5 top-1 text-[11px] font-medium tracking-wide text-white/60"
			>
				{name}
			</span>
			{content.length > 0 ? (
				content
			) : (
				<span className="m-auto self-center text-xs text-white/50">{t("page.empty-slot")}</span>
			)}
		</fieldset>
	);
};

export const StorageCabinet = (props: Props) => {
	const { cabinet, nodes } = props;
	const { t } = useTranslation("storage");
	const slots = childrenOf(nodes, cabinet.id);
	const grid = cabinet.layout === StorageLayout.Grid;

	return (
		<fieldset className="m-0 inline-flex min-w-0 flex-col gap-2 rounded-md border-0 bg-wood p-2 shadow-[0_10px_24px_rgb(0_0_0/0.35)]">
			<legend className="sr-only">{nodeName(cabinet, nodes, t)}</legend>
			{grid ? (
				<div className="grid gap-1.5" style={{ gridTemplateColumns: `repeat(${cabinet.columns ?? 1}, auto)` }}>
					{slots.map((slot) => (
						<Slot key={slot.id} slot={slot} grid {...props} />
					))}
				</div>
			) : (
				slots.map((slot) => <Slot key={slot.id} slot={slot} grid={false} {...props} />)
			)}
		</fieldset>
	);
};
