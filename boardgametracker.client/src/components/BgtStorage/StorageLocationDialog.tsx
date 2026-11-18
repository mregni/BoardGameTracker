import { noop, useQuery } from "@tanstack/react-query";
import { type SubmitEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtFieldLabel, BgtSimpleInputField, BgtSimpleSelect } from "@/components/BgtForm";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { BgtText } from "@/components/BgtText/BgtText";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { StorageLayout, type StorageLocation } from "@/models";
import { getGames } from "@/services/queries/games";
import {
	allPlaces,
	isCabinet,
	itemsAt,
	nodeName,
	parsePlaceKey,
	placeKey,
	placeLabel,
	type StoragePlace,
	storageItems,
} from "@/utils/storage";

type Mode = { kind: "create"; parent: StoragePlace | null } | { kind: "edit"; node: StorageLocation };

interface Props {
	open: boolean;
	close: () => void;
	mode: Mode;
	onCreated?: (node: StorageLocation) => void;
}

const MAX_SIZE = 20;

const toSize = (value: string) => {
	const number = Number.parseInt(value, 10);
	return Number.isNaN(number) ? null : number;
};

export const StorageLocationDialog = ({ open, close, mode, onCreated }: Props) => {
	const { t } = useTranslation(["storage", "common"]);
	const { nodes, locations } = useStorageData();
	const { data: games = [] } = useQuery(getGames());
	const { createStorageLocation, updateStorageLocation, isPending } = useStorageActions();
	const editing = mode.kind === "edit" ? mode.node : null;

	const parentOptions = allPlaces(nodes, locations).filter((place) => {
		const node = nodes.find((x) => x.id === place.storageLocationId);
		return place.storageLocationId === null || node?.layout === StorageLayout.Free;
	});
	const defaultParent =
		mode.kind === "create" &&
		mode.parent &&
		parentOptions.some((x) => placeKey(x) === placeKey(mode.parent as StoragePlace))
			? mode.parent
			: (parentOptions[0] ?? null);

	const [name, setName] = useState(editing?.name ?? "");
	const [parent, setParent] = useState<StoragePlace | null>(defaultParent);
	const [layout, setLayout] = useState<StorageLayout>(editing?.layout ?? StorageLayout.Free);
	const [rows, setRows] = useState(String(editing?.rows ?? 4));
	const [columns, setColumns] = useState(String(editing?.columns ?? 4));

	const isSlot = editing?.layout === StorageLayout.Slot;
	const sized = layout === StorageLayout.Shelves || layout === StorageLayout.Grid;
	const rowCount = toSize(rows);
	const columnCount = layout === StorageLayout.Shelves ? 1 : toSize(columns);
	const validSize =
		!sized ||
		(rowCount !== null &&
			columnCount !== null &&
			rowCount >= 1 &&
			columnCount >= 1 &&
			rowCount <= MAX_SIZE &&
			columnCount <= MAX_SIZE);
	const valid = (isSlot || name.trim().length > 0) && validSize && (editing !== null || parent !== null);

	const removedItems =
		editing && isCabinet(editing) && validSize
			? nodes
					.filter((slot) => slot.parentId === editing.id)
					.filter((slot) => (slot.row ?? 0) > (rowCount ?? 0) || (slot.column ?? 0) > (columnCount ?? 0))
					.flatMap((slot) => itemsAt(storageItems(games), { locationId: slot.locationId, storageLocationId: slot.id }))
			: [];

	const save = (): Promise<unknown> => {
		if (editing) {
			return updateStorageLocation({
				id: editing.id,
				name: name.trim() || null,
				rows: isCabinet(editing) ? rowCount : null,
				columns: isCabinet(editing) ? columnCount : null,
			});
		}
		if (!parent) return Promise.resolve();
		return createStorageLocation({
			name: name.trim(),
			locationId: parent.locationId,
			parentId: parent.storageLocationId,
			layout,
			rows: sized ? rowCount : null,
			columns: sized ? columnCount : null,
		}).then((created) => onCreated?.(created));
	};

	const submit = (event: SubmitEvent<HTMLFormElement>) => {
		event.preventDefault();
		if (!valid) return;
		void save().then(close, noop);
	};

	const sizeField = (label: string, value: string, onChange: (value: string) => void) => (
		<div className="flex w-24 flex-col">
			<BgtFieldLabel>{label}</BgtFieldLabel>
			<BgtSimpleInputField
				type="number"
				inputMode="numeric"
				value={value}
				onChange={(event) => onChange(event.target.value)}
				ariaLabel={label}
				disabled={isPending}
			/>
		</div>
	);

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={submit} className="flex flex-col gap-4">
					<BgtDialogTitle>
						{editing ? t("form.edit-title", { name: nodeName(editing, nodes, t) }) : t("form.new-title")}
					</BgtDialogTitle>
					{!editing && <BgtDialogDescription>{t("form.new-description")}</BgtDialogDescription>}
					<div className="flex flex-col">
						<BgtFieldLabel>
							{isSlot && editing
								? t("form.slot-name", { name: nodeName({ ...editing, name: null }, nodes, t) })
								: t("form.name")}
						</BgtFieldLabel>
						<BgtSimpleInputField
							type="text"
							value={name}
							onChange={(event) => setName(event.target.value)}
							ariaLabel={t("form.name")}
							disabled={isPending}
						/>
					</div>
					{!editing && (
						<>
							<BgtSimpleSelect
								label={t("form.inside")}
								hasSearch
								items={parentOptions.map((place) => ({
									value: placeKey(place),
									label: placeLabel(place, nodes, locations, t),
								}))}
								value={parent ? placeKey(parent) : undefined}
								onValueChange={(value) => setParent(parsePlaceKey(String(value)))}
								disabled={isPending}
							/>
							<div className="flex flex-col gap-1">
								<BgtFieldLabel>{t("layout.label")}</BgtFieldLabel>
								<BgtSegmentedControl
									aria-label={t("layout.label")}
									value={layout}
									onChange={setLayout}
									options={[StorageLayout.Free, StorageLayout.Shelves, StorageLayout.Grid].map((value) => ({
										value,
										label: t(`layout.${value}`),
									}))}
								/>
								<BgtText size="2" color="gray">
									{t("layout.hint")}
								</BgtText>
							</div>
						</>
					)}
					{sized && (
						<div className="flex flex-col gap-1">
							<div className="flex items-end gap-3">
								{layout === StorageLayout.Shelves ? (
									sizeField(t("form.shelves"), rows, setRows)
								) : (
									<>
										{sizeField(t("form.rows"), rows, setRows)}
										<span className="pb-2 text-white/50">×</span>
										{sizeField(t("form.columns"), columns, setColumns)}
									</>
								)}
							</div>
							{validSize && !editing && (
								<BgtText size="2" color="gray">
									{layout === StorageLayout.Shelves
										? t("form.creates-shelves", { count: rowCount ?? 0 })
										: t("form.creates-cells", {
												count: (rowCount ?? 0) * (columnCount ?? 0),
												rows: rowCount,
												columns: columnCount,
											})}
								</BgtText>
							)}
							{editing && removedItems.length > 0 && (
								<BgtText size="2" className="text-warn" role="status">
									{t("form.shrink", { count: removedItems.length, name: nodeName(editing, nodes, t) })}
								</BgtText>
							)}
						</div>
					)}
					<BgtDialogFooter
						onCancel={close}
						cancelLabel={t("common:cancel")}
						confirmLabel={editing ? t("form.save") : t("form.create")}
						confirmType="submit"
						disabled={!valid || isPending}
						cancelDisabled={isPending}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
