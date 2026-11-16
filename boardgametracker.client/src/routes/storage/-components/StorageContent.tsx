import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import InfoIcon from "@/assets/icons/info.svg?react";
import LeftRightArrowIcon from "@/assets/icons/left-right-arrow.svg?react";
import PencilIcon from "@/assets/icons/pencil.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import TrashIcon from "@/assets/icons/trash.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BoxOrientation, type Location, StorageLayout, type StorageLocation } from "@/models";
import {
	ancestorsOf,
	childrenOf,
	isCabinet,
	itemsAt,
	itemsIn,
	nodeName,
	rootsOf,
	type StorageItem,
	type StoragePlace,
} from "@/utils/storage";
import { useRovingBoxes } from "../-hooks/useRovingBoxes";
import { StorageBox } from "./StorageBox";
import { StorageCabinet } from "./StorageCabinet";
import { layoutIcons } from "./StorageTree";

interface Props {
	place: StoragePlace;
	location: Location;
	nodes: StorageLocation[];
	items: StorageItem[];
	selectedKey: string | null;
	canWrite: boolean;
	onGo: (place: StoragePlace) => void;
	onSelect: (item: StorageItem) => void;
	onMoveBox: (draggedKey: string, slot: StorageLocation, beforeKey: string | null) => void;
	onAdd: () => void;
	onEdit: () => void;
	onMove: () => void;
	onDelete: () => void;
}

const layoutTag = (node: StorageLocation, t: (key: string, values?: Record<string, unknown>) => string) => {
	if (node.layout === StorageLayout.Grid) return t("layout.grid-size", { rows: node.rows, columns: node.columns });
	if (node.layout === StorageLayout.Shelves) return t("layout.shelves-count", { count: node.rows ?? 0 });
	return t(`layout.${node.layout}`);
};

const Hint = ({ children }: { children: ReactNode }) => (
	<p className="mt-2.5 flex items-center gap-1.5 text-sm text-white/60">
		<InfoIcon className="size-4 flex-none" aria-hidden="true" />
		{children}
	</p>
);

export const StorageContent = (props: Props) => {
	const { place, location, nodes, items, selectedKey, canWrite, onGo, onSelect, onMoveBox } = props;
	const { t } = useTranslation(["storage", "common"]);
	const node = nodes.find((x) => x.id === place.storageLocationId);
	const name = node ? nodeName(node, nodes, t) : location.name;
	const kids = (node ? childrenOf(nodes, node.id) : rootsOf(nodes, location.id)).filter(
		(child) => child.layout !== StorageLayout.Slot,
	);
	const direct = itemsAt(items, place);
	const roving = useRovingBoxes(
		direct.map((item) => item.key),
		selectedKey,
	);
	const total = itemsIn(items, place, nodes).length;
	const crumbs: { place: StoragePlace; name: string }[] = [
		{ place: { locationId: location.id, storageLocationId: null }, name: location.name },
		...ancestorsOf(nodes, place.storageLocationId).map((x) => ({
			place: { locationId: x.locationId, storageLocationId: x.id },
			name: nodeName(x, nodes, t),
		})),
	];
	const canAddHere = !node || node.layout === StorageLayout.Free;

	const bin = (
		<div
			onKeyDown={roving.onKeyDown}
			className="flex min-h-[84px] flex-wrap items-end gap-1.5 rounded-lg border border-dashed border-card-border bg-card-black/60 p-2.5"
		>
			<p id={roving.hintId} className="sr-only">
				{t("page.keys-hint")}
			</p>
			{direct.length > 0 ? (
				direct.map((item) => (
					<StorageBox
						key={item.key}
						item={item}
						orientation={BoxOrientation.Front}
						selected={item.key === selectedKey}
						onSelect={onSelect}
						tabIndex={roving.tabIndexFor(item.key)}
						describedBy={roving.describedByFor(item.key)}
						onFocusBox={roving.onFocusBox}
					/>
				))
			) : (
				<span className="m-auto self-center text-xs text-white/50">{t("page.nothing-directly")}</span>
			)}
		</div>
	);

	return (
		<section
			aria-labelledby="storage-content-title"
			className="min-w-0 motion-safe:animate-rise-in motion-reduce:animate-fade-in"
		>
			<nav aria-label={t("page.path")} className="flex flex-wrap items-center gap-1.5 text-sm text-white/60">
				{crumbs.map((crumb, index) =>
					index === crumbs.length - 1 ? (
						<span key={crumb.name} className="text-white" aria-current="page">
							{crumb.name}
						</span>
					) : (
						<span key={crumb.name} className="flex items-center gap-1.5">
							<button
								type="button"
								onClick={() => onGo(crumb.place)}
								className="text-primary-text hover:underline pointer-coarse:inline-flex pointer-coarse:min-h-11 pointer-coarse:items-center"
							>
								{crumb.name}
							</button>
							<span aria-hidden="true" className="text-white/40">
								›
							</span>
						</span>
					),
				)}
			</nav>
			<div className="mb-4 flex flex-wrap items-start justify-between gap-3">
				<div>
					<h2 id="storage-content-title" className="mb-1 mt-1.5 text-2xl font-bold">
						{name}
					</h2>
					<p className="flex flex-wrap items-center gap-3 text-sm text-white/60">
						{node && (
							<>
								<span>{layoutTag(node, t)}</span>
								<span aria-hidden="true">·</span>
							</>
						)}
						<span>
							<span className="font-bold tabular-nums text-card-value">{total}</span>{" "}
							{t("page.stored", { count: total })}
						</span>
						{!node && (
							<>
								<span aria-hidden="true">·</span>
								<span>
									<span className="font-bold tabular-nums text-card-value">{location.playCount}</span>{" "}
									{t("page.sessions", { count: location.playCount })}
								</span>
							</>
						)}
					</p>
				</div>
				{canWrite && (
					<div className="flex items-center gap-1">
						{canAddHere && (
							<BgtButton size="1" className="pointer-coarse:min-h-11" onClick={props.onAdd}>
								<PlusIcon className="size-4" aria-hidden="true" />
								{t("page.add-here")}
							</BgtButton>
						)}
						<BgtIconButton
							icon={<PencilIcon className="size-4" />}
							intent="subtile"
							className="pointer-coarse:size-11"
							onClick={props.onEdit}
							aria-label={t("page.edit", { name })}
						/>
						{node && (
							<>
								{node.layout !== StorageLayout.Slot && (
									<BgtIconButton
										icon={<LeftRightArrowIcon className="size-4" />}
										intent="subtile"
										className="pointer-coarse:size-11"
										onClick={props.onMove}
										aria-label={t("page.move", { name })}
									/>
								)}
								{node.layout !== StorageLayout.Slot && (
									<BgtIconButton
										icon={<TrashIcon className="size-4" />}
										intent="subtile-danger"
										className="pointer-coarse:size-11"
										onClick={props.onDelete}
										aria-label={t("page.delete", { name })}
									/>
								)}
							</>
						)}
					</div>
				)}
			</div>
			{node && isCabinet(node) ? (
				<>
					<div className="overflow-x-auto pb-3 pt-1 [--box:64px] [--spine:20px] xl:[--box:72px] xl:[--spine:22px] pointer-coarse:[--spine:28px]">
						<StorageCabinet
							cabinet={node}
							nodes={nodes}
							items={items}
							selectedKey={selectedKey}
							onSelect={onSelect}
							onMove={canWrite ? onMoveBox : undefined}
						/>
					</div>
					{canWrite && (
						<Hint>
							<span className="pointer-coarse:hidden">
								{t(node.layout === StorageLayout.Grid ? "page.drag-hint-grid" : "page.drag-hint")}
							</span>
							<span className="hidden pointer-coarse:inline">{t("page.touch-hint")}</span>
						</Hint>
					)}
					{direct.length > 0 && (
						<div className="mt-5 [--box:64px] [--spine:20px] pointer-coarse:[--spine:28px]">
							<h3 className="mb-2.5 text-sm font-bold tracking-wide">{t("page.stored-directly", { name })}</h3>
							{bin}
						</div>
					)}
				</>
			) : (
				<div className="[--box:64px] [--spine:20px] pointer-coarse:[--spine:28px]">
					{kids.length > 0 && (
						<>
							<h3 className="mb-2.5 text-sm font-bold tracking-wide">{t("page.storage-locations")}</h3>
							<ul className="mb-5 grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-3">
								{kids.map((kid) => {
									const target = { locationId: kid.locationId, storageLocationId: kid.id };
									const inside = itemsIn(items, target, nodes);
									const Icon = layoutIcons[kid.layout];
									const kidName = nodeName(kid, nodes, t);
									return (
										<li key={kid.id}>
											<button
												type="button"
												onClick={() => onGo(target)}
												className="flex w-full flex-col gap-2.5 rounded-lg border border-primary/20 bg-primary/10 p-3 text-left transition-[border-color,box-shadow] hover:border-primary/60 hover:shadow-lg"
											>
												<span className="flex items-center justify-between gap-2">
													<span className="flex items-center gap-1.5 font-bold">
														<Icon className="size-4 text-primary-text" aria-hidden="true" />
														{kidName}
													</span>
													<span className="font-bold tabular-nums text-card-value">{inside.length}</span>
												</span>
												<span className="flex h-10 items-end gap-[3px] overflow-hidden" aria-hidden="true">
													{inside.length === 0 ? (
														<span className="text-xs text-white/50">{t("page.empty-slot")}</span>
													) : (
														inside
															.slice(0, 8)
															.map((item) => (
																<img
																	key={item.key}
																	src={item.image ?? undefined}
																	loading="lazy"
																	alt=""
																	className="h-10 w-[30px] flex-none rounded-[3px] bg-white/5 object-cover"
																/>
															))
													)}
												</span>
												<span className="text-xs text-white/60">{layoutTag(kid, t)}</span>
											</button>
										</li>
									);
								})}
							</ul>
						</>
					)}
					<h3 className="mb-2.5 text-sm font-bold tracking-wide">{t("page.stored-directly", { name })}</h3>
					{bin}
					{!node && kids.length === 0 && canWrite && <Hint>{t("page.add-hint", { name })}</Hint>}
				</div>
			)}
		</section>
	);
};
