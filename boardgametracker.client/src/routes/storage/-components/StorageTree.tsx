import { cx } from "class-variance-authority";
import type { ComponentType, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import CaretRightIcon from "@/assets/icons/caret-right.svg?react";
import GridIcon from "@/assets/icons/grid.svg?react";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import PackageIcon from "@/assets/icons/package.svg?react";
import ShelvesIcon from "@/assets/icons/shelves.svg?react";
import { type Location, StorageLayout, type StorageLocation } from "@/models";
import {
	childrenOf,
	isCabinet,
	itemsIn,
	nodeName,
	placeKey,
	rootsOf,
	type StorageItem,
	type StoragePlace,
} from "@/utils/storage";

export const layoutIcons: Record<StorageLayout, ComponentType<SVGProps<SVGSVGElement>>> = {
	[StorageLayout.Free]: PackageIcon,
	[StorageLayout.Shelves]: ShelvesIcon,
	[StorageLayout.Grid]: GridIcon,
	[StorageLayout.Slot]: ShelvesIcon,
};

interface Props {
	locations: Location[];
	nodes: StorageLocation[];
	items: StorageItem[];
	current: StoragePlace | null;
	open: Set<string>;
	onToggle: (key: string) => void;
	onGo: (place: StoragePlace) => void;
}

interface Row {
	place: StoragePlace;
	name: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	children: Row[];
	top: boolean;
}

export const StorageTree = ({ locations, nodes, items, current, open, onToggle, onGo }: Props) => {
	const { t } = useTranslation("storage");

	const nodeRow = (node: StorageLocation): Row => ({
		place: { locationId: node.locationId, storageLocationId: node.id },
		name: nodeName(node, nodes, t),
		icon: layoutIcons[node.layout],
		children: isCabinet(node) ? [] : childrenOf(nodes, node.id).map(nodeRow),
		top: false,
	});

	const rows: Row[] = [...locations]
		.sort((a, b) => a.name.localeCompare(b.name))
		.map((location) => ({
			place: { locationId: location.id, storageLocationId: null },
			name: location.name,
			icon: MapPinIcon,
			children: rootsOf(nodes, location.id).map(nodeRow),
			top: true,
		}));

	const render = (row: Row) => {
		const key = placeKey(row.place);
		const isOpen = open.has(key);
		const isCurrent = current !== null && placeKey(current) === key;
		const count = itemsIn(items, row.place, nodes).length;
		const Icon = row.icon;
		return (
			<li key={key}>
				<div
					className={cx(
						"flex items-center gap-1 rounded-md pr-2",
						isCurrent ? "bg-primary/60" : "hover:bg-primary/20",
						row.top && "font-bold",
					)}
				>
					{row.children.length > 0 ? (
						<button
							type="button"
							aria-expanded={isOpen}
							aria-label={t(isOpen ? "page.collapse" : "page.expand", { name: row.name })}
							onClick={() => onToggle(key)}
							className="flex size-6 flex-none items-center justify-center text-white/50 pointer-coarse:size-11"
						>
							<CaretRightIcon className={cx("size-3 transition-transform", isOpen && "rotate-90")} />
						</button>
					) : (
						<span className="w-6 flex-none" />
					)}
					<button
						type="button"
						aria-current={isCurrent ? "true" : undefined}
						onClick={() => onGo(row.place)}
						className="flex min-w-0 flex-1 items-center gap-1.5 py-1.5 text-left font-[inherit] pointer-coarse:min-h-11"
					>
						<Icon
							className={cx("size-4 flex-none", isCurrent ? "text-white" : "text-primary-text")}
							aria-hidden="true"
						/>
						<span className="min-w-0 flex-1 truncate">{row.name}</span>
						<span className="text-xs font-normal tabular-nums text-white/60">{count || ""}</span>
					</button>
				</div>
				{row.children.length > 0 && isOpen && (
					<ul className="ml-[9px] border-l border-card-border pl-3.5">{row.children.map(render)}</ul>
				)}
			</li>
		);
	};

	return <ul aria-label={t("page.tree")}>{rows.map(render)}</ul>;
};
