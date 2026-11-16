import { type ReactNode, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtSimpleSelect } from "@/components/BgtForm";
import { useStorageData } from "@/hooks/useStorage";
import { NewLocationModal } from "@/routes/locations/-modals/NewLocationModal";
import { allPlaces, parsePlaceKey, placeKey, placeLabel, type StoragePlace } from "@/utils/storage";
import { StorageLocationDialog } from "./StorageLocationDialog";

const NONE = "none";
const NEW_STORAGE = "new-storage";
const NEW_LOCATION = "new-location";

interface Props {
	value: StoragePlace | null;
	onChange: (place: StoragePlace | null) => void;
	noneLabel?: string;
	placeholder?: string;
	label?: string;
	"aria-label"?: string;
	disabled?: boolean;
	canCreate?: boolean;
	className?: string;
	trigger?: ReactNode;
	triggerClassName?: string;
	defaultOpen?: boolean;
	onOpenChange?: (open: boolean) => void;
}

export const StoragePicker = (props: Props) => {
	const { value, onChange, noneLabel, placeholder, canCreate = false, ...selectProps } = props;
	const { t } = useTranslation("storage");
	const { nodes, locations } = useStorageData();
	const [creating, setCreating] = useState<"storage" | "location" | null>(null);

	const label = (place: StoragePlace) =>
		place.storageLocationId === null
			? t("place.somewhere-in", { name: placeLabel(place, nodes, locations, t) })
			: placeLabel(place, nodes, locations, t);

	const items = [
		...(noneLabel ? [{ value: NONE, label: noneLabel }] : []),
		...allPlaces(nodes, locations).map((place) => ({ value: placeKey(place), label: label(place) })),
		...(canCreate
			? [
					...(locations.length > 0 ? [{ value: NEW_STORAGE, label: t("picker.new-storage") }] : []),
					{ value: NEW_LOCATION, label: t("picker.new-location") },
				]
			: []),
	];

	const select = (selected: string | number) => {
		const key = String(selected);
		if (key === NEW_STORAGE) setCreating("storage");
		else if (key === NEW_LOCATION) setCreating("location");
		else onChange(key === NONE ? null : parsePlaceKey(key));
	};

	return (
		<>
			<BgtSimpleSelect
				{...selectProps}
				hasSearch
				placeholder={placeholder ?? t("picker.placeholder")}
				items={items}
				value={value ? placeKey(value) : noneLabel ? NONE : ""}
				onValueChange={select}
			/>
			{creating === "storage" && (
				<StorageLocationDialog
					open
					close={() => setCreating(null)}
					mode={{ kind: "create", parent: value }}
					onCreated={(node) => onChange({ locationId: node.locationId, storageLocationId: node.id })}
				/>
			)}
			{creating === "location" && (
				<NewLocationModal
					open
					close={() => setCreating(null)}
					onCreated={(location) => onChange({ locationId: location.id, storageLocationId: null })}
				/>
			)}
		</>
	);
};
