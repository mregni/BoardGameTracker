import { noop, useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import PackageIcon from "@/assets/icons/package.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { DeleteStorageLocationDialog } from "@/components/BgtStorage/DeleteStorageLocationDialog";
import { MoveStorageLocationDialog } from "@/components/BgtStorage/MoveStorageLocationDialog";
import { StorageLocationDialog } from "@/components/BgtStorage/StorageLocationDialog";
import { usePermissions } from "@/hooks/usePermissions";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { type BoxOrientation, type Location, StorageItemType, StorageLayout, type StorageLocation } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { EditLocationModal } from "@/routes/locations/-modals/EditLocationModal";
import { NewLocationModal } from "@/routes/locations/-modals/NewLocationModal";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getStorageLocations } from "@/services/queries/storage";
import {
	ancestorsOf,
	itemKey,
	itemsAt,
	needsAPlace,
	parsePlaceKey,
	placeKey,
	placeLabel,
	type StorageItem,
	type StoragePlace,
	storageItems,
} from "@/utils/storage";
import { BoxInspector } from "./-components/BoxInspector";
import { NeedsAPlaceCard } from "./-components/NeedsAPlaceCard";
import { BoxLanding } from "./-components/StorageBox";
import { StorageContent } from "./-components/StorageContent";
import { type SearchHit, StorageSearch } from "./-components/StorageSearch";
import { StorageTree } from "./-components/StorageTree";

interface StorageSearchParams {
	at?: string;
}

export const Route = createFileRoute("/storage/")({
	component: RouteComponent,
	validateSearch: (search: Record<string, unknown>): StorageSearchParams =>
		typeof search.at === "string" ? { at: search.at } : {},
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getStorageLocations()).catch(noop);
		queryClient.query(getLocations()).catch(noop);
		queryClient.query(getGames()).catch(noop);
	},
});

type Dialog =
	| { kind: "create"; parent: StoragePlace | null }
	| { kind: "edit"; node: StorageLocation }
	| { kind: "edit-location"; location: Location }
	| { kind: "move"; node: StorageLocation }
	| { kind: "delete"; node: StorageLocation }
	| { kind: "new-location" };

const reference = (item: StorageItem) => ({ type: item.type, id: item.id });

const resolve = (at: string | undefined, nodes: StorageLocation[], locations: Location[]): StoragePlace | null => {
	const place = at ? parsePlaceKey(at) : null;
	const node = nodes.find((x) => x.id === place?.storageLocationId);
	if (place && locations.some((x) => x.id === place.locationId) && (place.storageLocationId === null || node)) {
		return node?.layout === StorageLayout.Slot
			? { locationId: node.locationId, storageLocationId: node.parentId }
			: place;
	}
	const first = [...locations].sort((a, b) => a.name.localeCompare(b.name))[0];
	return first ? { locationId: first.id, storageLocationId: null } : null;
};

function RouteComponent() {
	const { t } = useTranslation(["storage", "common"]);
	const { at } = Route.useSearch();
	const navigate = useNavigate({ from: Route.fullPath });
	const { canWrite } = usePermissions();
	const { successToast } = useToasts();
	const { nodes, locations, isLoading } = useStorageData();
	const { data: games = [], isLoading: gamesLoading } = useQuery(getGames());
	const actions = useStorageActions();
	const [selectedKey, setSelectedKey] = useState<string | null>(null);
	const [closed, setClosed] = useState<Set<string>>(new Set());
	const [checked, setChecked] = useState<Set<number>>(new Set());
	const [flashId, setFlashId] = useState<number | null>(null);
	const [landed, setLanded] = useState<Set<string>>(new Set());
	const [dialog, setDialog] = useState<Dialog | null>(null);

	if (isLoading || gamesLoading) return <BgtLoadingSpinner />;

	if (locations.length === 0) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={PackageIcon}
				title={t("page.no-locations-title")}
				description={t("page.no-locations-description")}
				action={
					canWrite ? { label: t("page.new-location"), onClick: () => setDialog({ kind: "new-location" }) } : undefined
				}
			>
				{dialog?.kind === "new-location" && <NewLocationModal open close={() => setDialog(null)} />}
			</BgtEmptyPage>
		);
	}

	const items = storageItems(games);
	const place = resolve(at, nodes, locations);
	const location = locations.find((x) => x.id === place?.locationId);
	const node = nodes.find((x) => x.id === place?.storageLocationId);
	const selected = items.find((item) => item.key === selectedKey) ?? null;
	const homeless = needsAPlace(games);
	const open = new Set(
		[
			...locations.map((x) => placeKey({ locationId: x.id, storageLocationId: null })),
			...ancestorsOf(nodes, place?.storageLocationId ?? null).map((x) =>
				placeKey({ locationId: x.locationId, storageLocationId: x.id }),
			),
		].filter((key) => !closed.has(key)),
	);

	const go = (target: StoragePlace, select: string | null = null) => {
		setSelectedKey(select);
		void navigate({ search: { at: placeKey(target) } });
	};

	const toggle = (key: string) =>
		setClosed((previous) => {
			const next = new Set(previous);
			if (open.has(key)) next.add(key);
			else next.delete(key);
			return next;
		});

	const land = (keys: string[]) => setLanded(new Set(keys));
	const landing = {
		keys: landed,
		done: (key: string) =>
			setLanded((previous) => {
				const next = new Set(previous);
				next.delete(key);
				return next;
			}),
	};

	const arrange = (slot: StorageLocation, order: StorageItem[], moved: string) =>
		actions.arrangeStorage({ storageLocationId: slot.id, items: order.map(reference) }).then(() => land([moved]), noop);

	const moveBox = (draggedKey: string, slot: StorageLocation, beforeKey: string | null) => {
		const dragged = items.find((item) => item.key === draggedKey);
		if (!dragged) return;
		const order = itemsAt(items, { locationId: slot.locationId, storageLocationId: slot.id }).filter(
			(item) => item.key !== draggedKey,
		);
		const index = beforeKey ? order.findIndex((item) => item.key === beforeKey) : -1;
		order.splice(index < 0 ? order.length : index, 0, dragged);
		void arrange(slot, order, draggedKey);
	};

	const selectedSlot = selected ? nodes.find((x) => x.id === selected.place.storageLocationId) : undefined;
	const slotItems = selected && selectedSlot?.layout === StorageLayout.Slot ? itemsAt(items, selected.place) : [];
	const selectedIndex = slotItems.findIndex((item) => item.key === selected?.key);
	const shift = (direction: -1 | 1) => {
		if (!selectedSlot || selectedIndex < 0) return;
		const order = [...slotItems];
		const [moved] = order.splice(selectedIndex, 1);
		order.splice(selectedIndex + direction, 0, moved);
		void arrange(selectedSlot, order, moved.key);
	};

	const assign = async (refs: { type: StorageItemType; id: number }[], target: StoragePlace) => {
		try {
			await actions.assignStorage({
				items: refs,
				locationId: target.locationId,
				storageLocationId: target.storageLocationId,
			});
			successToast("storage:notifications.assigned", undefined, {
				count: refs.length,
				name: placeLabel(target, nodes, locations, t),
			});
			return true;
		} catch {
			return false;
		}
	};

	const view = (target: StoragePlace) => {
		const slot = nodes.find((x) => x.id === target.storageLocationId);
		return slot?.layout === StorageLayout.Slot
			? { locationId: slot.locationId, storageLocationId: slot.parentId }
			: target;
	};

	const pick = (hit: SearchHit) => {
		if (!hit.place) {
			setChecked((previous) => new Set(previous).add(hit.game.id));
			setFlashId(hit.game.id);
			return;
		}
		const key =
			hit.expansion && !hit.inBaseBox
				? itemKey(StorageItemType.Expansion, hit.expansion.id)
				: itemKey(StorageItemType.Game, hit.game.id);
		go(view(hit.place), key);
		land([key]);
	};

	return (
		<BgtPage>
			<BgtPageHeader
				icon={PackageIcon}
				header={t("title")}
				actions={
					canWrite
						? [
								{
									onClick: () => setDialog({ kind: "new-location" }),
									variant: "cancel",
									content: "storage:page.new-location",
									icon: MapPinIcon,
								},
								{
									onClick: () => setDialog({ kind: "create", parent: place }),
									variant: "primary",
									content: "storage:page.new-storage",
									icon: PlusIcon,
								},
							]
						: []
				}
			/>
			<BgtPageContent>
				<BoxLanding.Provider value={landing}>
					<StorageSearch games={games} nodes={nodes} locations={locations} onPick={pick} />
					<div
						className={`grid items-start gap-4 md:grid-cols-[240px_minmax(0,1fr)] xl:grid-cols-[280px_minmax(0,1fr)_300px] ${selected ? "max-md:pb-[55dvh]" : ""}`}
					>
						<aside
							aria-label={t("page.tree")}
							className="sticky top-6 max-h-[calc(100vh-48px)] overflow-auto rounded-lg border border-primary/20 bg-primary/10 p-2.5 max-md:hidden"
						>
							<StorageTree
								locations={locations}
								nodes={nodes}
								items={items}
								current={place}
								open={open}
								onToggle={toggle}
								onGo={(target) => go(target)}
							/>
						</aside>
						{place && location && (
							<StorageContent
								place={place}
								location={location}
								nodes={nodes}
								items={items}
								selectedKey={selectedKey}
								canWrite={canWrite}
								onGo={(target) => go(target)}
								onSelect={(item) => setSelectedKey(item.key)}
								onMoveBox={moveBox}
								onAdd={() => setDialog({ kind: "create", parent: place })}
								onEdit={() => setDialog(node ? { kind: "edit", node } : { kind: "edit-location", location })}
								onMove={() => node && setDialog({ kind: "move", node })}
								onDelete={() => node && setDialog({ kind: "delete", node })}
							/>
						)}
						<div className="flex flex-col gap-4 md:col-start-2 xl:sticky xl:top-6 xl:col-start-auto xl:max-h-[calc(100vh-48px)] xl:overflow-auto">
							{selected && (
								<BoxInspector
									item={selected}
									nodes={nodes}
									locations={locations}
									canWrite={canWrite}
									canShift={{
										earlier: selectedIndex > 0,
										later: selectedIndex >= 0 && selectedIndex < slotItems.length - 1,
									}}
									onClose={() => {
										document.querySelector<HTMLElement>(`[data-box="${selected.key}"]`)?.focus();
										setSelectedKey(null);
									}}
									onOrientation={(orientation: BoxOrientation) =>
										void actions
											.updateBoxOrientation({ item: reference(selected), orientation })
											.then(() => land([selected.key]), noop)
									}
									onShift={shift}
									onMove={async (target) => {
										if (await assign([reference(selected)], target)) {
											go(view(target), selected.key);
											land([selected.key]);
										}
									}}
								/>
							)}
							<NeedsAPlaceCard
								games={homeless}
								checked={checked}
								flashId={flashId}
								onFlashEnd={() => setFlashId(null)}
								canWrite={canWrite}
								onToggle={(gameId, isChecked) =>
									setChecked((previous) => {
										const next = new Set(previous);
										if (isChecked) next.add(gameId);
										else next.delete(gameId);
										return next;
									})
								}
								onMove={async (target) => {
									const refs = homeless
										.filter((game) => checked.has(game.id))
										.map((game) => ({ type: StorageItemType.Game, id: game.id }));
									if (refs.length > 0 && (await assign(refs, target))) {
										setChecked(new Set());
										go(view(target));
										land(refs.map((ref) => itemKey(ref.type, ref.id)));
									}
								}}
							/>
						</div>
					</div>
					{dialog?.kind === "create" && (
						<StorageLocationDialog
							open
							close={() => setDialog(null)}
							mode={{ kind: "create", parent: dialog.parent }}
							onCreated={(created) => go({ locationId: created.locationId, storageLocationId: created.id })}
						/>
					)}
					{dialog?.kind === "edit" && <StorageLocationDialog open close={() => setDialog(null)} mode={dialog} />}
					{dialog?.kind === "edit-location" && (
						<EditLocationModal open location={dialog.location} close={() => setDialog(null)} />
					)}
					{dialog?.kind === "move" && <MoveStorageLocationDialog node={dialog.node} close={() => setDialog(null)} />}
					{dialog?.kind === "delete" && (
						<DeleteStorageLocationDialog
							node={dialog.node}
							close={() => setDialog(null)}
							onDeleted={() => go({ locationId: dialog.node.locationId, storageLocationId: dialog.node.parentId })}
						/>
					)}
					{dialog?.kind === "new-location" && (
						<NewLocationModal
							open
							close={() => setDialog(null)}
							onCreated={(created) => go({ locationId: created.id, storageLocationId: null })}
						/>
					)}
				</BoxLanding.Provider>
			</BgtPageContent>
		</BgtPage>
	);
}
