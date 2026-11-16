import { cx } from "class-variance-authority";
import { type DragEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import { BoxOrientation } from "@/models";
import type { StorageItem } from "@/utils/storage";
import { StringToHsl } from "@/utils/stringUtils";

interface Props {
	item: StorageItem;
	selected: boolean;
	onSelect: (item: StorageItem) => void;
	orientation?: BoxOrientation;
	large?: boolean;
	onDropBefore?: (draggedKey: string, beforeKey: string) => void;
}

const shapes: Record<BoxOrientation, string> = {
	[BoxOrientation.Front]: "size-(--box) items-end",
	[BoxOrientation.Side]: "h-(--box) w-(--spine) items-center justify-center",
	[BoxOrientation.Flat]: "h-(--spine) w-(--box) items-center px-1",
};

export const boxLabel = (item: StorageItem, t: (key: string, values?: Record<string, unknown>) => string) =>
	item.expansion ? t("storage:box.expansion-label", { title: item.title, base: item.game.title }) : item.title;

export const StorageBox = ({
	item,
	selected,
	onSelect,
	orientation = item.orientation,
	large,
	onDropBefore,
}: Props) => {
	const { t } = useTranslation("storage");
	const [over, setOver] = useState(false);
	const label = [boxLabel(item, t), item.isLoaned ? t("box.loaned-short") : null].filter(Boolean).join(" · ");
	const showCover = orientation === BoxOrientation.Front && item.image;

	const drop = (event: DragEvent) => {
		event.preventDefault();
		event.stopPropagation();
		setOver(false);
		const dragged = event.dataTransfer.getData("text/plain");
		if (dragged && dragged !== item.key) onDropBefore?.(dragged, item.key);
	};

	return (
		<button
			type="button"
			aria-pressed={selected}
			data-box={item.key}
			aria-label={label}
			title={label}
			draggable={!!onDropBefore}
			onDragStart={(event) => {
				event.dataTransfer.setData("text/plain", item.key);
				event.dataTransfer.effectAllowed = "move";
			}}
			onDragOver={(event) => {
				if (!onDropBefore) return;
				event.preventDefault();
				event.stopPropagation();
				setOver(true);
			}}
			onDragLeave={() => setOver(false)}
			onDrop={drop}
			onClick={() => onSelect(item)}
			style={{ backgroundColor: StringToHsl(item.title) }}
			className={cx(
				"relative flex flex-none scroll-mt-6 overflow-hidden rounded-[3px] text-left font-bold text-white [text-shadow:0_1px_2px_rgb(0_0_0/0.55)] shadow-[inset_0_0_0_1px_rgb(255_255_255/0.1),0_1px_2px_rgb(0_0_0/0.4)]",
				onDropBefore ? "cursor-grab active:cursor-grabbing" : "cursor-pointer",
				shapes[orientation],
				large && orientation === BoxOrientation.Front && "size-[calc(var(--box)*1.05)]",
				item.isLoaned && "grayscale brightness-50",
				selected && "outline-2 outline-offset-2 outline-white",
				over && "shadow-[-4px_0_0_0_var(--color-card-value)]",
				"focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-text",
			)}
		>
			{showCover ? (
				<img src={item.image ?? undefined} alt="" loading="lazy" className="absolute inset-0 size-full object-cover" />
			) : (
				<span
					className={cx(
						"text-[11px] leading-3",
						orientation === BoxOrientation.Front && "line-clamp-3 px-1 py-[3px]",
						orientation === BoxOrientation.Side &&
							"max-h-[calc(var(--box)-6px)] truncate [transform:rotate(180deg)] [writing-mode:vertical-rl]",
						orientation === BoxOrientation.Flat && "truncate",
					)}
				>
					{item.title}
				</span>
			)}
			{item.expansion && (
				<span
					aria-hidden="true"
					className="absolute right-[3px] top-[3px] size-1.5 rounded-full bg-white shadow-[0_0_0_2px_rgb(0_0_0/0.35)]"
				/>
			)}
		</button>
	);
};
