import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtSimpleSelect } from "@/components/BgtForm";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { StorageLayout, type StorageLocation } from "@/models";
import {
	allPlaces,
	descendantIds,
	nodeName,
	parsePlaceKey,
	placeKey,
	placeLabel,
	type StoragePlace,
} from "@/utils/storage";

interface Props {
	node: StorageLocation;
	close: () => void;
}

export const MoveStorageLocationDialog = ({ node, close }: Props) => {
	const { t } = useTranslation(["storage", "common"]);
	const { nodes, locations } = useStorageData();
	const { moveStorageLocation, isPending } = useStorageActions();
	const inside = descendantIds(nodes, node.id);
	const options = allPlaces(nodes, locations).filter((place) => {
		if (place.storageLocationId === null) return true;
		const target = nodes.find((x) => x.id === place.storageLocationId);
		return target?.layout === StorageLayout.Free && !inside.includes(target.id);
	});
	const current = { locationId: node.locationId, storageLocationId: node.parentId };
	const [target, setTarget] = useState<StoragePlace>(current);
	const name = nodeName(node, nodes, t);

	const confirm = async () => {
		try {
			await moveStorageLocation({ id: node.id, locationId: target.locationId, parentId: target.storageLocationId });
			close();
		} catch {
			// the mutation already shows the error
		}
	};

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("move.title", { name })}</BgtDialogTitle>
				<BgtDialogDescription>{t("move.description")}</BgtDialogDescription>
				<BgtSimpleSelect
					label={t("move.to")}
					hasSearch
					items={options.map((place) => ({ value: placeKey(place), label: placeLabel(place, nodes, locations, t) }))}
					value={placeKey(target)}
					onValueChange={(value) => setTarget(parsePlaceKey(String(value)) ?? current)}
					disabled={isPending}
				/>
				<BgtDialogFooter
					onCancel={close}
					cancelLabel={t("common:cancel")}
					confirmLabel={t("move.save")}
					onConfirm={confirm}
					disabled={isPending || placeKey(target) === placeKey(current)}
					cancelDisabled={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
